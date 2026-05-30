using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaPass_API.Common;
using NovaPass_API.DTOs.Auth;
using NovaPass_API.Services.Interfaces;

namespace NovaPass_API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IAuthService auth) : ControllerBase
{
    /// <summary>GET /users/search?q= — Búsqueda de cliente por email (seller)</summary>
    [HttpGet("search")]
    [Authorize(Roles = "seller,admin")]
    public async Task<IActionResult> SearchUser([FromQuery] string q)
    {
        try
        {
            var result = await auth.SearchUserAsync(q);
            return Ok(ApiResponse.Ok(result));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>GET /users/employees — Listado de empleados</summary>
    [HttpGet("employees")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetEmployees()
    {
        try
        {
            var result = await auth.GetEmployeesAsync();
            return Ok(ApiResponse.Ok(result));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>POST /users/employees — Crear empleado</summary>
    [HttpPost("employees")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        try
        {
            var result = await auth.CreateEmployeeAsync(request);
            return StatusCode(201, ApiResponse.Ok(result));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>DELETE /users/employees/:id — Soft delete (desactivar)</summary>
    [HttpDelete("employees/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeactivateEmployee(string id)
    {
        try
        {
            await auth.DeactivateEmployeeAsync(id);
            return Ok(ApiResponse.Ok(new { deactivated = true }));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>PUT /users/employees/:id/activate — Reactivar empleado</summary>
    [HttpPut("employees/{id}/activate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ActivateEmployee(string id)
    {
        try
        {
            await auth.ActivateEmployeeAsync(id);
            return Ok(ApiResponse.Ok(new { activated = true }));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>PUT /users/employees/:id/permissions — Actualizar portales</summary>
    [HttpPut("employees/{id}/permissions")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePermissions(string id, [FromBody] UpdatePermissionsRequest request)
    {
        try
        {
            await auth.UpdatePermissionsAsync(id, request.Portals);
            return Ok(ApiResponse.Ok(new { updated = true }));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }
}
