
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using NovaPass_API.Data;
using NovaPass_API.DTOs.Auth;
using NovaPass_API.Helpers;
using NovaPass_API.Models;
using NovaPass_API.Services.Interfaces;
using BCryptNet = BCrypt.Net.BCrypt;

namespace NovaPass_API.Services;

public class AuthService : IAuthService
{

    public static readonly ConcurrentDictionary<string, byte> RevokedTokens = new();


    private static readonly ConcurrentDictionary<string, (string Email, DateTime Expiry)> _resetTokens = new();

    private readonly TicketEventsDbContext _db;
    private readonly JwtHelper _jwt;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly IWebHostEnvironment _env;

    public AuthService(
        TicketEventsDbContext db,
        JwtHelper jwt,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment env)
    {
        _db = db;
        _jwt = jwt;
        _config = config;
        _http = httpClientFactory.CreateClient();
        _env = env;
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new AppException("Email already registered", 409);

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            PasswordHash = BCryptNet.HashPassword(request.Password),
            Role = UserRol.customer,
            IsActive = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        return new LoginResponse(token, user.Role.ToString(), [], ToDto(user));
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || user.PasswordHash == null || !BCryptNet.Verify(request.Password, user.PasswordHash))
            throw new AppException("Invalid credentials", 401);

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
                Role = UserRol.customer,
                IsActive = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

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

    public Task LogoutAsync(string userId, string jti)
    {
        RevokedTokens.TryAdd(jti, 0);
        return Task.CompletedTask;
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return;

        var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("/", "_").Replace("+", "-");

        _resetTokens[resetToken] = (request.Email, DateTime.UtcNow.AddHours(1));

        var webhookUrl = Environment.GetEnvironmentVariable("N8N_WEBHOOK_URL")
            ?? _config["N8N:WebhookUrl"];

        if (!string.IsNullOrEmpty(webhookUrl))
        {
            await _http.PostAsJsonAsync(webhookUrl, new
            {
                email = request.Email,
                token = resetToken,
            });
        }
    }

    public async Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new AppException("User not found", 404);

        if (request.Foto != null)
            user.PhotoUrl = request.Foto;

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
            throw new AppException("Email already registered", 409);

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            PasswordHash = BCryptNet.HashPassword(request.Password),
            Role = Enum.Parse<UserRol>(request.Role),
            Permissions = request.Permissions.Length > 0 ? string.Join(",", request.Permissions) : null,
            IsActive = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task DeactivateEmployeeAsync(string employeeId)
    {
        var user = await _db.Users.FindAsync(employeeId)
            ?? throw new AppException("Employee not found", 404);

        user.IsActive = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Email, user.Role.ToString(), user.PhotoUrl, user.FullName);
}
