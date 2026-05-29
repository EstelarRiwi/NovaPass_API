using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaPass_API.Common;
using NovaPass_API.DTOs.Auth;
using NovaPass_API.Services.Interfaces;

namespace NovaPass_API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = "AdminOnly")]
public class ReportsController(IReportsService reports) : ControllerBase
{
    /// <summary>GET /reports/sales — Ventas por período o semana</summary>
    [HttpGet("sales")]
    public async Task<IActionResult> GetSales(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string? group_by = "week")
    {
        try
        {
            var result = await reports.GetSalesPerPeriodAsync(from, to, group_by);
            return Ok(ApiResponse.Ok(result));
        }
        catch (Exception ex) { return StatusCode(500, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>GET /reports/users — Usuarios registrados por período</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsersRegistered(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        try
        {
            var result = await reports.GetUsersRegisteredAsync(from, to);
            return Ok(ApiResponse.Ok(result));
        }
        catch (Exception ex) { return StatusCode(500, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>GET /reports/occupancy — Índice de ocupación por evento</summary>
    [HttpGet("occupancy")]
    public async Task<IActionResult> GetOccupancy([FromQuery] string? event_id = null)
    {
        try
        {
            var result = await reports.GetOccupancyByEventAsync(event_id);
            return Ok(ApiResponse.Ok(result));
        }
        catch (Exception ex) { return StatusCode(500, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>GET /reports/sales-by-category — Ventas desglosadas por categoría</summary>
    [HttpGet("sales-by-category")]
    public async Task<IActionResult> GetSalesByCategory(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var result = await reports.GetSalesByCategoryAsync(from, to);
            return Ok(ApiResponse.Ok(result));
        }
        catch (Exception ex) { return StatusCode(500, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>GET /reports/entries/:eventId — Conteo de ingresos validados</summary>
    [HttpGet("entries/{eventId}")]
    public async Task<IActionResult> GetEntries(string eventId)
    {
        try
        {
            var result = await reports.GetValidatedEntriesAsync(eventId);
            return Ok(ApiResponse.Ok(result));
        }
        catch (Exception ex) { return StatusCode(500, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>GET /reports/audit — Registro de auditoría paginado</summary>
    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int per_page = 20)
    {
        try
        {
            var result = await reports.GetAuditLogsAsync(page, per_page);
            return Ok(ApiResponse.Ok(result));
        }
        catch (Exception ex) { return StatusCode(500, ApiResponse.Fail(ex.Message)); }
    }
}