using NovaPass_API.DTOs.Auth;

namespace NovaPass_API.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> GoogleLoginAsync(string googleIdToken);
    Task<UserDto> GetMeAsync(string userId);
    Task LogoutAsync(string userId, string jti);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<UserDto> UploadAvatarAsync(string userId, IFormFile file);
    Task<UserDto> CreateEmployeeAsync(CreateEmployeeRequest request);
    Task DeactivateEmployeeAsync(string employeeId);
}