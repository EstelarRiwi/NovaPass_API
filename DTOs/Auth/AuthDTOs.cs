namespace NovaPass_API.DTOs.Auth;

public record RegisterRequest(string Email, string Password, string FullName);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string Role, string[] Permissions, UserDto User);

public record UserDto(string Id, string Email, string Role, string? AvatarUrl, string? FullName, string? Phone);

public record GoogleLoginBody(string IdToken);

public record CreateEmployeeRequest(string Email, string Password, string Role, string[] Permissions, string FullName);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword);

public record UpdateProfileRequest(string? Foto, string? FullName, string? Phone);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public class AppException(string message, int statusCode = 500) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}