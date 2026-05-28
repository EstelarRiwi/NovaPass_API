using NovaPass_API.DTOs.Tickets;

namespace NovaPass_API.Services.Interfaces;

public interface ITicketsService
{
    // Historial del usuario
    Task<TicketListResponse> GetMyTicketsAsync(string userId);
    Task<TicketHistoryResponse> GetMyTicketHistoryAsync(string userId, int page, int perPage);

    // Checkout online — crea ticket en estado "pendiente"
    // Retorna ticketId para que Juan (MercadoPago) lo asocie a la preferencia
    Task<string> CreatePendingTicketAsync(string userId, CheckoutRequest request);

    /// <summary>
    /// MÉTODO INTERNO — Juan lo llama desde el webhook de MercadoPago.
    /// NO es un endpoint HTTP.
    /// Cambia el estado del ticket a "activo" y genera el QR firmado.
    /// </summary>
    Task ActivateTicketAsync(string ticketId, string paymentReference);

    // Verificación de estado de pago (para pantalla post-pago del frontend)
    Task<PaymentVerifyResponse> VerifyPaymentAsync(string? paymentId, string? preferenceId);

    // PDF del ticket (binario)
    Task<byte[]> GetTicketPdfAsync(string ticketId, string userId);

    // Venta presencial (taquilla — rol seller)
    Task<TicketSummaryDto> CreatePresentialTicketAsync(string sellerId, PresentialSaleRequest request);

    // Validación de QR en puerta (rol scanner)
    Task<QrValidationResult> ValidateQrAsync(ValidateQrRequest request);
}