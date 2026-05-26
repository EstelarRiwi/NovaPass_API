using System.Security.Cryptography;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using NovaPass_API.Data;
using NovaPass_API.DTOs.Auth;
using NovaPass_API.Helpers;
using NovaPass_API.Infrastructure.MongoDB;
using NovaPass_API.Models;
using NovaPass_API.Services.Interfaces;
using BCryptNet = BCrypt.Net.BCrypt;

namespace NovaPass_API.Services;

public class AuthService : IAuthService
{
    private readonly TicketEventsDbContext _db;
    private readonly JwtHelper _jwt;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly IWebHostEnvironment _env;
    private readonly ILogService _log;

    public AuthService(
        TicketEventsDbContext db,
        JwtHelper jwt,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment env,
        ILogService log)
    {
        _db = db;
        _jwt = jwt;
        _config = config;
        _http = httpClientFactory.CreateClient();
        _env = env;
        _log = log;
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            await _log.LogAuthAsync("registro_fallido", new { email = request.Email, razon = "Email ya registrado" });
            throw new AppException("Email already registered", 409);
        }

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            PasswordHash = BCryptNet.HashPassword(request.Password),
            Role = UserRole.customer,
            IsActive = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _log.LogAuthAsync("registro_exitoso", new { email = user.Email }, userId: user.Id);

        var token = _jwt.GenerateToken(user);
        return new LoginResponse(token, user.Role.ToString(), [], ToDto(user));
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || user.PasswordHash == null || !BCryptNet.Verify(request.Password, user.PasswordHash))
        {
            await _log.LogAuthAsync("login_fallido", new { email = request.Email, razon = "Credenciales inválidas" });
            throw new AppException("Invalid credentials", 401);
        }

        await _log.LogAuthAsync("login_exitoso", new { email = user.Email, rol = user.Role.ToString() }, userId: user.Id);

        var token = _jwt.GenerateToken(user);
        var permissions = user.Permissions?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
        return new LoginResponse(token, user.Role.ToString(), permissions, ToDto(user));
    }

    public async Task<LoginResponse> GoogleLoginAsync(string googleIdToken)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(googleIdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_config["Google:ClientId"]!],
                });
        }
        catch (InvalidJwtException)
        {
            await _log.LogAuthAsync("login_google_fallido", new { razon = "Token de Google inválido" });
            throw new AppException("Invalid Google token", 401);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject)
            ?? await _db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

        if (user != null)
        {
            if (user.GoogleId == null)
            {
                user.GoogleId = payload.Subject;
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
        else
        {
            user = new User
            {
                Email = payload.Email,
                FullName = payload.Name ?? payload.Email,
                GoogleId = payload.Subject,
                PhotoUrl = payload.Picture,
                Role = UserRole.customer,
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        await _log.LogAuthAsync("login_google_exitoso", new { email = user.Email }, userId: user.Id);

        var token = _jwt.GenerateToken(user);
        var permissions = user.Permissions?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
        return new LoginResponse(token, user.Role.ToString(), permissions, ToDto(user));
    }

    public async Task<UserDto> GetMeAsync(string userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new AppException("User not found", 404);
        return ToDto(user);
    }

    public async Task LogoutAsync(string userId, string jti)
    {
        _db.TokenBlacklists.Add(new TokenBlacklist
        {
            Jti = jti,
            UserId = userId,
            InvalidatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
        });
        await _db.SaveChangesAsync();

        await _log.LogAuthAsync("token_invalidado", new { jti }, userId: userId);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return;

        var tokenHash = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("/", "_").Replace("+", "-");

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        await _log.LogAuthAsync("contrasena_recuperada", new { email = user.Email }, userId: user.Id);

        var webhookUrl = Environment.GetEnvironmentVariable("N8N_WEBHOOK_URL")
            ?? _config["N8N:WebhookUrl"];

        if (!string.IsNullOrEmpty(webhookUrl))
        {
            await _http.PostAsJsonAsync(webhookUrl, new
            {
                email = request.Email,
                token = tokenHash,
            });
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var resetToken = await _db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == request.Token);

        if (resetToken == null || resetToken.Used != 0 || resetToken.ExpiresAt < DateTime.UtcNow)
            throw new AppException("Invalid or expired token", 400);

        resetToken.User.PasswordHash = BCryptNet.HashPassword(request.NewPassword);
        resetToken.User.UpdatedAt = DateTime.UtcNow;
        resetToken.Used = 1;

        await _db.SaveChangesAsync();

        await _log.LogAuthAsync("contrasena_cambiada", new { }, userId: resetToken.User.Id);
    }

    public async Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new AppException("User not found", 404);

        if (request.Foto != null) user.PhotoUrl = request.Foto;
        if (request.FullName != null) user.FullName = request.FullName;
        if (request.Phone != null) user.Phone = request.Phone;

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task<UserDto> UploadAvatarAsync(string userId, IFormFile file)
    {
        if (file.Length == 0)
            throw new AppException("No file provided", 400);

        var user = await _db.Users.FindAsync(userId)
            ?? throw new AppException("User not found", 404);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{userId}_{Guid.NewGuid()}{ext}";
        var folderPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "avatars");
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);
        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        user.PhotoUrl = $"/avatars/{fileName}";
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task<UserDto> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            await _log.LogAuthAsync("creacion_empleado_fallida", new { email = request.Email, razon = "Email ya registrado" });
            throw new AppException("Email already registered", 409);
        }

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            PasswordHash = BCryptNet.HashPassword(request.Password),
            Role = Enum.Parse<UserRole>(request.Role),
            Permissions = request.Permissions.Length > 0 ? string.Join(",", request.Permissions) : null,
            IsActive = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _log.LogAuthAsync("empleado_creado", new { email = user.Email, rol = user.Role.ToString() }, userId: user.Id);

        return ToDto(user);
    }

    public async Task DeactivateEmployeeAsync(string employeeId)
    {
        var user = await _db.Users.FindAsync(employeeId)
            ?? throw new AppException("Employee not found", 404);

        user.IsActive = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _log.LogAuthAsync("empleado_desactivado", new { email = user.Email }, userId: employeeId);
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Email, user.Role.ToString(), user.PhotoUrl, user.FullName, user.Phone);
}
