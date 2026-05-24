using NovaPass_API.DTOs.Auth;

namespace NovaPass_API.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> GoogleLoginAsync(string googleIdToken);
    Task<UserDto> GetMeAsync(int userId);
    Task LogoutAsync(int userId, string jti);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task<UserDto> UploadAvatarAsync(int userId, IFormFile file);
    Task<UserDto> CreateEmployeeAsync(CreateEmployeeRequest request);
    Task DeactivateEmployeeAsync(int employeeId);
}
