using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaPass_API.Data;
using NovaPass_API.Infrastructure.MongoDB;
using NovaPass_API.Services;

namespace NovaPass_API.Controllers;

[Route("api/payments")]
[ApiController]
public class PaymentController(PaymentService paymentService, TicketEventsDbContext context, ILogService log): ControllerBase
{
    [HttpPost("create-preference/{ticketId}")]
    public async Task<IActionResult> CreatePreference(string ticketId)
    {
        var ticket = await context.Tickets
            .Include(t => t.Event)
            .Include(t => t.BuyerUser)
            .FirstOrDefaultAsync(t => t.Id == ticketId);
        
        if (ticket == null) return NotFound(new { message = "Ticket no encontrado"});

        decimal precio = 5000;

        var preferenceId = await paymentService.CreateCheckoutPreferenceAsync(
            ticket.Id,
            ticket.Event?.Name ?? "Evento Estelar",
            precio,
            ticket.BuyerUser?.Email ?? "cliente@test.com"

        );
        
        return Ok(new{ preferenceId });
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromQuery] string topic, [FromQuery] string id)
    {
        if (topic == "payment")
        {
            await log.LogSystemAsync("pago_recibido_mercadopago", new 
            { 
                MercadoPagoId = id, 
                Mensaje = "Se recibió una notificación de pago" 
            });
        }
        return Ok();
    }
    
}