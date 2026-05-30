using Microsoft.EntityFrameworkCore;
using NovaPass_API.Data;
using NovaPass_API.DTOs.Auth;
using NovaPass_API.DTOs.Tickets;
using NovaPass_API.Helpers;
using NovaPass_API.Infrastructure.MongoDB;
using NovaPass_API.Models;
using NovaPass_API.Services.Interfaces;

namespace NovaPass_API.Services;

public class TicketsService(
    TicketEventsDbContext db,
    IConfiguration config,
    IHttpClientFactory httpFactory,
    ILogService log,
    QrHelper qrHelper,
    PdfTicketHelper pdfHelper) : ITicketsService
{
    // ── Historial ───────────────────────────────────────────────────────────

    public async Task<TicketListResponse> GetMyTicketsAsync(string userId)
    {
        var tickets = await db.Tickets
            .Where(t => t.BuyerUserId == userId && (t.Status == TicketStatus.active || t.Status == TicketStatus.pending))
            .Include(t => t.Event)
            .Include(t => t.Category)
            .Include(t => t.Seat)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return new TicketListResponse(tickets.Select(MapToSummary).ToList());
    }

    public async Task<TicketHistoryResponse> GetMyTicketHistoryAsync(string userId, int page, int perPage)
    {
        var query = db.Tickets
            .Where(t => t.BuyerUserId == userId)
            .Include(t => t.Event)
            .Include(t => t.Category)
            .Include(t => t.Seat)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync();
        var tickets = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync();

        var dtos = tickets.Select(t => new TicketHistoryDto(
            t.Id,
            new TicketEventDto(t.Event!.Id, t.Event.Name, t.Event.EventDate, t.Event.Venue, t.Event.ImageUrl),
            t.Category!.Name,
            t.Seat != null ? $"{t.Seat.RowCode}-{t.Seat.SeatNumber}" : null,
            t.Status.ToString(),
            t.CreatedAt,
            t.PaymentReference,
            t.Category?.Price ?? 0
        )).ToList();

        return new TicketHistoryResponse(dtos, total, page, perPage);
    }

    // ── Checkout (crea ticket pendiente) ───────────────────────────────────

    public async Task<string> CreatePendingTicketAsync(string userId, CheckoutRequest request)
    {
        // Validar que el evento esté en período de venta
        var ev = await db.Events.FindAsync(request.EventId)
            ?? throw new AppException("Event not found", 404);

        if (ev.Status == EventStatus.cancelled)
            throw new AppException("Sale closed", 403);

        if (DateTime.UtcNow < ev.SaleOpensAt || DateTime.UtcNow > ev.SaleClosesAt)
            throw new AppException("Sale closed", 403);

        // Verificar y decrementar aforo de forma atómica con transacción
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // FOR UPDATE para evitar race conditions
            var category = await db.TicketCategories
                .FromSqlRaw(
                    "SELECT * FROM ticket_categories WHERE id = {0} FOR UPDATE",
                    request.CategoryId)
                .FirstOrDefaultAsync()
                ?? throw new AppException("Category not found", 404);

            if (category.AvailableCapacity < request.Quantity)
                throw new AppException("No capacity available", 409);

            category.AvailableCapacity -= request.Quantity;
            category.UpdatedAt = DateTime.UtcNow;

            var ticketId = Guid.NewGuid().ToString();

            // Generate QR immediately — no MP webhook integration yet
            var qrToken = qrHelper.GenerateSignedQr(new QrPayload(
                ticketId,
                request.EventId,
                request.SeatId ?? "N/A",
                ExpiresAt: DateTime.UtcNow.AddYears(1)
            ));

            var ticket = new Ticket
            {
                Id = ticketId,
                EventId = request.EventId,
                CategoryId = request.CategoryId,
                SeatId = request.SeatId,
                BuyerUserId = userId,
                Status = TicketStatus.active,
                QrToken = qrToken,
                PurchasedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return ticket.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ── ActivateTicket — MÉTODO INTERNO (llamado por Juan desde webhook MP) ─

    /// <summary>
    /// NO es un endpoint HTTP. Juan llama este método directamente
    /// desde MercadoPagoService después de confirmar el pago.
    /// Si este método lanza excepción, Juan NO publica evento a n8n y
    /// registra el error en logs_system.
    /// </summary>
    public async Task ActivateTicketAsync(string ticketId, string paymentReference)
    {
        var ticket = await db.Tickets
            .Include(t => t.Event)
            .Include(t => t.Category)
            .Include(t => t.BuyerUser)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new Exception($"Ticket {ticketId} not found");

        if (ticket.Status != TicketStatus.pending)
            throw new Exception($"Ticket {ticketId} is not in pending state");

        // Generar QR firmado criptográficamente
        var qrToken = qrHelper.GenerateSignedQr(new QrPayload(
            ticket.Id,
            ticket.EventId,
            ticket.Seat != null ? $"{ticket.Seat.RowCode}-{ticket.Seat.SeatNumber}" : "N/A",
            ExpiresAt: DateTime.UtcNow.AddYears(1)
        ));

        ticket.Status = TicketStatus.active;
        ticket.QrToken = qrToken;
        ticket.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await log.LogTicketAsync("ticket_activated", new
        {
            ticket_id = ticketId,
            payment_reference = paymentReference,
            event_id = ticket.EventId
        }, ticket.BuyerUserId);
    }

    // ── Verificación de pago (para pantalla post-pago) ─────────────────────

    public async Task<PaymentVerifyResponse> VerifyPaymentAsync(string? paymentId, string? preferenceId)
    {
        // Busca el Payment asociado al id de MercadoPago
        Payment? payment = null;

        if (paymentId != null)
            payment = await db.Payments
                .Include(p => p.Ticket)
                    .ThenInclude(t => t!.Event)
                .FirstOrDefaultAsync(p => p.MercadopagoPaymentId == paymentId);
        else if (preferenceId != null)
            payment = await db.Payments
                .Include(p => p.Ticket)
                    .ThenInclude(t => t!.Event)
                .FirstOrDefaultAsync(p => p.MercadopagoPreferenceId == preferenceId);

        if (payment == null)
            return new PaymentVerifyResponse("pending", null);

        if (payment.Status == PaymentStatus.approved && payment.Ticket?.Status == TicketStatus.active)
        {
            var t = payment.Ticket;
            return new PaymentVerifyResponse("approved", new TicketVerifiedDto(
                t.Id,
                t.Event!.Name,
                t.Seat != null ? $"{t.Seat.RowCode}-{t.Seat.SeatNumber}" : null,
                t.QrToken != null ? $"/api/tickets/{t.Id}/qr" : null,
                $"/api/tickets/{t.Id}/pdf"
            ));
        }

        if (payment.Status == PaymentStatus.rejected)
            return new PaymentVerifyResponse("rejected", null);

        return new PaymentVerifyResponse("pending", null);
    }

    // ── PDF del ticket ──────────────────────────────────────────────────────

    public async Task<byte[]> GetTicketPdfAsync(string ticketId, string userId)
    {
        var ticket = await db.Tickets
            .Include(t => t.Event)
            .Include(t => t.Category)
            .Include(t => t.BuyerUser)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.BuyerUserId == userId)
            ?? throw new AppException("Ticket not found", 404);

        return pdfHelper.GenerateTicketPdf(ticket);
    }

    // ── QR image del ticket ─────────────────────────────────────────────────

    public async Task<byte[]> GetTicketQrAsync(string ticketId, string userId)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.BuyerUserId == userId)
            ?? throw new AppException("Ticket not found", 404);

        if (string.IsNullOrEmpty(ticket.QrToken))
            throw new AppException("QR not available", 404);

        return qrHelper.GenerateQrImage(ticket.QrToken);
    }

    // ── Venta presencial (taquilla) ─────────────────────────────────────────

    public async Task<TicketSummaryDto> CreatePresentialTicketAsync(string sellerId, PresentialSaleRequest request)
    {
        var ev = await db.Events.FindAsync(request.EventId)
            ?? throw new AppException("Event not found", 404);

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var category = await db.TicketCategories
                .FromSqlRaw(
                    "SELECT * FROM ticket_categories WHERE id = {0} FOR UPDATE",
                    request.CategoryId)
                .FirstOrDefaultAsync()
                ?? throw new AppException("Category not found", 404);

            if (category.AvailableCapacity < 1)
                throw new AppException("No capacity available", 409);

            category.AvailableCapacity--;
            category.UpdatedAt = DateTime.UtcNow;

            var qrToken = qrHelper.GenerateSignedQr(new QrPayload(
                Guid.NewGuid().ToString(), // se reemplaza abajo
                request.EventId,
                request.SeatId ?? "N/A",
                ExpiresAt: DateTime.UtcNow.AddYears(1)
            ));

            var ticket = new Ticket
            {
                Id = Guid.NewGuid().ToString(),
                EventId = request.EventId,
                CategoryId = request.CategoryId,
                SeatId = request.SeatId,
                SoldBySellerId = sellerId,
                Status = TicketStatus.active,
                QrToken = qrToken,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            // Publicar evento a n8n
            await PublishN8nAsync("ticket_vendido_presencial", new
            {
                ticket_id = ticket.Id,
                event_title = ev.Name,
                buyer_email = request.BuyerEmail,
                buyer_name = request.BuyerName,
                category = category.Name,
                seat = request.SeatId
            });

            return MapToSummary(ticket);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ── Validación de QR en puerta ──────────────────────────────────────────

    public async Task<QrValidationResult> ValidateQrAsync(ValidateQrRequest request)
    {
        // Verificar firma del QR
        QrPayload? payload;
        try
        {
            payload = qrHelper.VerifyAndDecodeQr(request.QrToken);
        }
        catch
        {
            return new QrValidationResult("fake", null, null);
        }

        if (payload == null || payload.ExpiresAt < DateTime.UtcNow)
            return new QrValidationResult("fake", null, null);

        var ticket = await db.Tickets
            .Include(t => t.BuyerUser)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == payload.TicketId);

        if (ticket == null)
            return new QrValidationResult("fake", null, null);

        if (ticket.Status == TicketStatus.used)
            return new QrValidationResult("used", null, null);

        if (ticket.Status != TicketStatus.active)
            return new QrValidationResult("fake", null, null);

        // Marcar como usado
        ticket.Status = TicketStatus.used;
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var customerName = ticket.BuyerUser?.FullName ?? "Unknown";

        return new QrValidationResult("valid", customerName, ticket.Category?.Name);
    }

    // ── Mapper privado ──────────────────────────────────────────────────────

    private static TicketSummaryDto MapToSummary(Ticket t) => new(
        t.Id,
        new TicketEventDto(
            t.Event?.Id ?? t.EventId,
            t.Event?.Name ?? "",
            t.Event?.EventDate ?? default,
            t.Event?.Venue ?? "",
            t.Event?.ImageUrl),
        t.Category?.Name ?? "",
        t.Seat != null ? $"{t.Seat.RowCode}-{t.Seat.SeatNumber}" : null,
        t.Status.ToString(),
        t.QrToken != null ? $"/api/tickets/{t.Id}/qr" : null,
        $"/api/tickets/{t.Id}/pdf",
        t.Category?.Price ?? 0
    );

    private async Task PublishN8nAsync(string eventName, object payload)
    {
        try
        {
            var webhookUrl = Environment.GetEnvironmentVariable("N8N_WEBHOOK_URL")
                             ?? config["N8N:WebhookUrl"];
            if (string.IsNullOrEmpty(webhookUrl)) return;
            var client = httpFactory.CreateClient();
            await client.PostAsJsonAsync($"{webhookUrl}/{eventName}", payload);
        }
        catch (Exception ex)
        {
            await log.LogSystemAsync("n8n_publish_error", new { eventName, error = ex.Message });
        }
    }
}