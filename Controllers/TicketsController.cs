using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaPass_API.Common;
using NovaPass_API.DTOs.Auth;
using NovaPass_API.DTOs.Tickets;
using NovaPass_API.Services.Interfaces;
using System.Security.Claims;

namespace NovaPass_API.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController(ITicketsService tickets) : ControllerBase
{
    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException();

    /// <summary>GET /tickets — Boletas activas del usuario</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyTickets()
    {
        try
        {
            var result = await tickets.GetMyTicketsAsync(GetUserId());
            return Ok(ApiResponse.Ok(result));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>GET /tickets/history — Historial completo de boletas</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int per_page = 10)
    {
        try
        {
            var result = await tickets.GetMyTicketHistoryAsync(GetUserId(), page, per_page);
            return Ok(ApiResponse.Ok(result));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>
    /// POST /tickets/checkout — Inicia compra online.
    /// Crea ticket en estado "pendiente" y retorna ticketId para que
    /// Juan (MercadoPago) cree la preferencia de pago.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        try
        {
            // Santiago crea el ticket pendiente y retorna el ID
            var ticketId = await tickets.CreatePendingTicketAsync(GetUserId(), request);

            // Juan (MercadoPagoService) completa el checkout_url y preference_id
            // Este endpoint retorna solo el ticketId; Juan lo enriquece con los datos de MP
            // La respuesta final al frontend incluye preference_id + checkout_url + ticket_id
            return StatusCode(201, ApiResponse.Ok(new { ticket_id = ticketId }));
        }
        catch (AppException ex) when (ex.StatusCode == 409)
        {
            return Conflict(ApiResponse.Fail("NO_CAPACITY_AVAILABLE"));
        }
        catch (AppException ex) when (ex.StatusCode == 403)
        {
            return StatusCode(403, ApiResponse.Fail("SALE_CLOSED"));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>GET /tickets/payment/verify — Verifica estado del pago post-redirección</summary>
    [HttpGet("payment/verify")]
    public async Task<IActionResult> VerifyPayment(
        [FromQuery] string? payment_id,
        [FromQuery] string? preference_id)
    {
        try
        {
            var result = await tickets.VerifyPaymentAsync(payment_id, preference_id);
            return Ok(ApiResponse.Ok(result));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>GET /tickets/:id/pdf — Descarga PDF del ticket (binario)</summary>
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetPdf(string id)
    {
        try
        {
            var pdf = await tickets.GetTicketPdfAsync(id, GetUserId());
            // Retorna binario — NO envolver en ApiResponse (ver estándar de respuestas)
            return File(pdf, "application/pdf", $"ticket-{id}.pdf");
        }
        catch (AppException ex) when (ex.StatusCode == 404)
        {
            return NotFound(ApiResponse.Fail("TICKET_NOT_FOUND"));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>
    /// POST /tickets/presential — Venta presencial (solo rol "seller").
    /// Crea ticket directamente en estado "activo".
    /// </summary>
    [HttpPost("presential")]
    [Authorize(Roles = "seller")]
    public async Task<IActionResult> CreatePresentialTicket([FromBody] PresentialSaleRequest request)
    {
        try
        {
            var result = await tickets.CreatePresentialTicketAsync(GetUserId(), request);
            return StatusCode(201, ApiResponse.Ok(result));
        }
        catch (AppException ex) when (ex.StatusCode == 409)
        {
            return Conflict(ApiResponse.Fail("NO_CAPACITY_AVAILABLE"));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }

    /// <summary>
    /// POST /tickets/validate — Validación de QR en puerta (solo rol "scanner").
    /// </summary>
    [HttpPost("validate")]
    [Authorize(Roles = "scanner")]
    public async Task<IActionResult> ValidateQr([FromBody] ValidateQrRequest request)
    {
        try
        {
            var result = await tickets.ValidateQrAsync(request);
            return Ok(ApiResponse.Ok(result));
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message)); }
    }
}