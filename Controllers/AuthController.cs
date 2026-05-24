using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NovaPass_API.DTOs.Auth;
using NovaPass_API.Services.Interfaces;

namespace NovaPass_API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await auth.RegisterAsync(request);
            return StatusCode(201, result);
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await auth.LoginAsync(request);
            return Ok(result);
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
    }

    [HttpPost("google")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginBody request)
    {
        try
        {
            var result = await auth.GoogleLoginAsync(request.IdToken);
            return Ok(result);
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception) { return StatusCode(401, new { message = "Invalid Google token" }); }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            var result = await auth.GetMeAsync(GetUserId());
            return Ok(result);
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? "";
        await auth.LogoutAsync(GetUserId(), jti);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await auth.ForgotPasswordAsync(request);
        return Ok(new { message = "If the email exists, a reset link has been sent" });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var result = await auth.UpdateProfileAsync(GetUserId(), request);
            return Ok(result);
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
    }

    [HttpPost("avatar")]
    [Authorize]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
    {
        try
        {
            var result = await auth.UploadAvatarAsync(GetUserId(), file);
            return Ok(result);
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
    }

    [HttpPost("employees")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        try
        {
            var result = await auth.CreateEmployeeAsync(request);
            return StatusCode(201, result);
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
    }

    [HttpDelete("employees/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeactivateEmployee(int id)
    {
        try
        {
            await auth.DeactivateEmployeeAsync(id);
            return NoContent();
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
    }

    private int GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(raw, out var id) ? id : 0;
    }
}
