namespace NovaPass_API.DTOs.Tickets;

// ── Requests ────────────────────────────────────────────────────────────────

/// <summary>
/// Inicia el proceso de compra online (crea ticket en estado "pendiente").
/// Juan recibe preference_id y checkout_url de MercadoPago y los retorna al frontend.
/// </summary>
public record CheckoutRequest(
    string EventId,
    string CategoryId,
    string? SeatId,
    int Quantity = 1
);

/// <summary>
/// Venta presencial desde taquilla. El ticket se crea directamente en estado "activo".
/// </summary>
public record PresentialSaleRequest(
    string EventId,
    string CategoryId,
    string? SeatId,
    string? BuyerEmail,
    string? BuyerName,
    string? BuyerUserId = null,
    int Quantity = 1
);

/// <summary>
/// Validación de QR en puerta (enviado por el scanner).
/// </summary>
public record ValidateQrRequest(string QrToken);

// ── Responses ────────────────────────────────────────────────────────────────

public record TicketEventDto(
    string Id,
    string Title,
    DateTime Date,
    string Venue,
    string? ImageUrl
);

public record TicketSummaryDto(
    string Id,
    TicketEventDto Event,
    string Category,
    string? Seat,
    string Status,
    string? QrUrl,
    string? PdfUrl,
    decimal Price
);

public record TicketHistoryDto(
    string Id,
    TicketEventDto Event,
    string Category,
    string? Seat,
    string Status,
    DateTime PurchasedAt,
    string? PaymentReference,
    decimal Price
);

public record TicketListResponse(List<TicketSummaryDto> Tickets);

public record TicketHistoryResponse(
    List<TicketHistoryDto> Tickets,
    int Total,
    int Page,
    int PerPage
);

/// <summary>
/// Retornado al frontend justo después de iniciar el checkout.
/// checkout_url y ticket_id son suficientes; preference_id viene de Juan (MercadoPago).
/// </summary>
public record CheckoutResponse(
    string PreferenceId,
    string CheckoutUrl,
    string TicketId
);

/// <summary>
/// Retornado por GET /tickets/payment/verify
/// </summary>
public record PaymentVerifyResponse(
    string Status,       // "approved" | "pending" | "rejected"
    TicketVerifiedDto? Ticket
);

public record TicketVerifiedDto(
    string Id,
    string EventTitle,
    string? Seat,
    string? QrUrl,
    string? PdfUrl
);

/// <summary>
/// Resultado de la validación del QR en puerta.
/// </summary>
public record QrValidationResult(
    string State,        // "valid" | "used" | "fake"
    string? CustomerName,
    string? CategoryName
);