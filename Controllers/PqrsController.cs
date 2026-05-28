using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaPass_API.DTOs.Pqrs;
using NovaPass_API.Services.Interfaces;
using System.Security.Claims;

namespace NovaPass_API.Controllers;

[ApiController]
[Route("api/pqrs")]
[Authorize]
public class PqrsController : ControllerBase
{
    private readonly IPqrsService _pqrs;

    public PqrsController(IPqrsService pqrs) => _pqrs = pqrs;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string Role   => User.FindFirstValue("role")!;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePqrRequest request)
    {
        var result = await _pqrs.CreateAsync(UserId, request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _pqrs.GetByIdAsync(id, UserId, Role);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _pqrs.GetAllAsync();
        return Ok(result);
    }

    [HttpPost("{id}/respond")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Respond(string id, [FromBody] RespondPqrRequest request)
    {
        var result = await _pqrs.RespondAsync(id, UserId, request);
        return Ok(result);
    }

    [HttpPost("{id}/close")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Close(string id)
    {
        var result = await _pqrs.CloseAsync(id, UserId);
        return Ok(result);
    }
}