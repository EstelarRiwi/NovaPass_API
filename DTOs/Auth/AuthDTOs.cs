namespace NovaPass_API.DTOs.Auth;

public record RegisterRequest(string Email, string Password, string FullName);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string Role, string[] Permissions, UserDto User);

public record UserDto(string Id, string Email, string Role, string? AvatarUrl, string? FullName, string? Phone);

public record GoogleLoginBody(string IdToken);

public record CreateEmployeeRequest(
    string Email,
    string Password,
    string[] Permissions,
    string? FullName = null,
    string? Name = null,
    string? Role = null)
{
    public string ResolvedName => FullName ?? Name ?? Email;
    public string ResolvedRole => Role
        ?? (Permissions.Contains("acceso") && !Permissions.Contains("taquilla") ? "scanner" : "seller");
}

public record EmployeeDto(string Id, string Name, string Email, string[] Permissions, bool Active, DateTime CreatedAt);

public record UpdatePermissionsRequest(string[] Portals);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword);

public record UpdateProfileRequest(string? Foto, string? FullName, string? Phone);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record UserSearchResult(string Id, string Name, string Email);

public class AppException(string message, int statusCode = 500) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}