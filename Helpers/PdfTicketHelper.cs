using NovaPass_API.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NovaPass_API.Helpers;

public class PdfTicketHelper(QrHelper qrHelper)
{
    public byte[] GenerateTicketPdf(Ticket ticket)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        byte[]? qrImage = ticket.QrToken != null
            ? qrHelper.GenerateQrImage(ticket.QrToken)
            : null;

        var eventDate = ticket.Event?.EventDate.ToString("dd/MM/yyyy HH:mm") ?? "";
        var seat = ticket.Seat != null
            ? $"{ticket.Seat.RowCode}-{ticket.Seat.SeatNumber}"
            : "General";
        var price = ticket.Category?.Price ?? 0;
        var priceStr = $"$ {price:N0}".Replace(",", ".");
        var buyerName = ticket.BuyerUser?.FullName ?? ticket.BuyerUser?.Email ?? "—";
        var buyerEmail = ticket.BuyerUser?.Email ?? "";
        var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // ── Header ──────────────────────────────────────
                    col.Item().AlignCenter().Text("NovaPass")
                        .Bold().FontSize(22);
                    col.Item().AlignCenter().Text("Punto de Venta")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // ── Ticket ID ────────────────────────────────────
                    col.Item().AlignCenter().Text("BOLETA")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().AlignCenter().Text(ticket.Id)
                        .Bold().FontSize(10);
                    col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // ── Cliente ──────────────────────────────────────
                    col.Item().Text("Cliente:").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text(buyerName).Bold().FontSize(11);
                    if (!string.IsNullOrEmpty(buyerEmail))
                        col.Item().Text(buyerEmail).FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // ── Evento ───────────────────────────────────────
                    col.Item().Text("Evento:").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text(ticket.Event?.Name ?? "").Bold().FontSize(11);
                    col.Item().Text(eventDate).FontSize(10);
                    col.Item().Text($"Lugar: {ticket.Event?.Venue ?? ""}").FontSize(10).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // ── Detalle ──────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Categoría").FontSize(10);
                        row.AutoItem().Text(ticket.Category?.Name ?? "").FontSize(10).Bold();
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Asiento").FontSize(10);
                        row.AutoItem().Text(seat).FontSize(10).Bold();
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("P. Unitario").FontSize(10);
                        row.AutoItem().Text(priceStr).FontSize(10).Bold();
                    });
                    col.Item().PaddingVertical(4).LineHorizontal(2).LineColor(Colors.Grey.Darken1);

                    // ── Total ────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL").Bold().FontSize(13);
                        row.AutoItem().Text(priceStr).Bold().FontSize(13);
                    });
                    col.Item().PaddingVertical(4).LineHorizontal(2).LineColor(Colors.Grey.Darken1);

                    // ── QR ───────────────────────────────────────────
                    if (qrImage != null)
                    {
                        col.Item().PaddingTop(10).AlignCenter().Width(150).Image(qrImage);
                        col.Item().PaddingTop(4).AlignCenter()
                            .Text("Presenta este código en la entrada")
                            .FontSize(9).FontColor(Colors.Grey.Medium);
                    }
                    else
                    {
                        col.Item().PaddingTop(10).AlignCenter()
                            .Text("QR pendiente de activación")
                            .FontSize(9).FontColor(Colors.Grey.Medium);
                    }

                    // ── Footer ───────────────────────────────────────
                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(4).AlignCenter()
                        .Text(now).FontSize(8).FontColor(Colors.Grey.Lighten1);
                });
            });
        });

        return pdf.GeneratePdf();
    }
}
