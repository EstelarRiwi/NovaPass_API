using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaPass_API.Common;
using NovaPass_API.DTOs.Auth;
using NovaPass_API.DTOs.Events;
using NovaPass_API.Services.Interfaces;
using System.Security.Claims;

namespace NovaPass_API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IEventsService events) : ControllerBase
{
    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException();

    // ── Cartelera pública ───────────────────────────────────────────────────

    /// <summary>GET /events — Listado público de cartelera</summary>
    [HttpGet]
    public async Task<IActionResult> GetEvents(
        [FromQuery] int page = 1,
        [FromQuery] int per_page = 10,
        [FromQuery] string? status = null)
    {
        try
        {
            var result = await events.GetEventsAsync(page, per_page, status);
            return Ok(ApiResponse.Ok(result));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>GET /events/:id — Detalle público de evento</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(string id)
    {
        try
        {
            var result = await events.GetEventByIdAsync(id);
            return Ok(ApiResponse.Ok(result));
        }
        catch (AppException ex) when (ex.StatusCode == 404)
        {
            return NotFound(ApiResponse.Fail("EVENT_NOT_FOUND"));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    // ── Gestión de eventos (solo admin) ────────────────────────────────────

    /// <summary>POST /events — Crear evento</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            var result = await events.CreateEventAsync(request);
            return StatusCode(201, ApiResponse.Ok(result));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>PUT /events/:id — Actualizar evento</summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateEvent(string id, [FromBody] UpdateEventRequest request)
    {
        try
        {
            var result = await events.UpdateEventAsync(id, request);
            return Ok(ApiResponse.Ok(result));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>DELETE /events/:id — Eliminar evento</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteEvent(string id)
    {
        try
        {
            await events.DeleteEventAsync(id);
            return Ok(ApiResponse.Ok(new { deleted = true }));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    // ── Categorías ──────────────────────────────────────────────────────────

    /// <summary>POST /events/:id/categories — Agregar categoría a evento</summary>
    [HttpPost("{id}/categories")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AddCategory(string id, [FromBody] CreateCategoryRequest request)
    {
        try
        {
            var result = await events.AddCategoryAsync(id, request);
            return StatusCode(201, ApiResponse.Ok(result));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>PUT /events/categories/:categoryId — Actualizar categoría</summary>
    [HttpPut("categories/{categoryId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateCategory(string categoryId, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            var result = await events.UpdateCategoryAsync(categoryId, request);
            return Ok(ApiResponse.Ok(result));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>DELETE /events/categories/:categoryId — Eliminar categoría</summary>
    [HttpDelete("categories/{categoryId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteCategory(string categoryId)
    {
        try
        {
            await events.DeleteCategoryAsync(categoryId);
            return Ok(ApiResponse.Ok(new { deleted = true }));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    // ── Favoritos ───────────────────────────────────────────────────────────

    /// <summary>GET /favorites — Favoritos del usuario autenticado</summary>
    [HttpGet("/api/favorites")]
    [Authorize]
    public async Task<IActionResult> GetFavorites()
    {
        try
        {
            var result = await events.GetFavoritesAsync(GetUserId());
            return Ok(ApiResponse.Ok(result));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>POST /favorites — Agregar a favoritos</summary>
    [HttpPost("/api/favorites")]
    [Authorize]
    public async Task<IActionResult> AddFavorite([FromBody] AddFavoriteRequest request)
    {
        try
        {
            var result = await events.AddFavoriteAsync(GetUserId(), request.EventId);
            return StatusCode(201, ApiResponse.Ok(result));
        }
        catch (AppException ex) when (ex.StatusCode == 409)
        {
            return Conflict(ApiResponse.Fail("ALREADY_IN_FAVORITES"));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>DELETE /favorites/:id — Quitar de favoritos</summary>
    [HttpDelete("/api/favorites/{id}")]
    [Authorize]
    public async Task<IActionResult> RemoveFavorite(string id)
    {
        try
        {
            await events.RemoveFavoriteAsync(GetUserId(), id);
            return Ok(ApiResponse.Ok(new { deleted = true }));
        }
        catch (AppException ex) when (ex.StatusCode == 404)
        {
            return NotFound(ApiResponse.Fail("FAVORITE_NOT_FOUND"));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }
}