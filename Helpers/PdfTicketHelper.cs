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

        // Generar imagen QR
        byte[]? qrImage = null;
        if (ticket.QrToken != null)
        {
            qrImage = qrHelper.GenerateQrImage(ticket.QrToken);
        }

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Content().Column(col =>
                {
                    // Header
                    col.Item().Text("🎭 ESTELAR")
                        .Bold().FontSize(24).AlignCenter();

                    col.Item().PaddingVertical(4).LineHorizontal(1);

                    // Datos del evento
                    col.Item().Text(ticket.Event?.Name ?? "")
                        .Bold().FontSize(16).AlignCenter();

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text($"Fecha: {ticket.Event?.EventDate:dd/MM/yyyy HH:mm}");
                            inner.Item().Text($"Lugar: {ticket.Event?.Venue}");
                            inner.Item().Text($"Categoría: {ticket.Category?.Name}");
                            inner.Item().Text($"Asiento: {(ticket.Seat != null ? $"{ticket.Seat.RowCode}-{ticket.Seat.SeatNumber}" : "General")}");
                            inner.Item().Text($"Estado: {ticket.Status}");
                        });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1);

                    // QR
                    if (qrImage != null)
                    {
                        col.Item().AlignCenter().Width(150).Image(qrImage);
                        col.Item().PaddingTop(4)
                            .Text("Presenta este código en la entrada")
                            .FontSize(9).AlignCenter().FontColor(Colors.Grey.Medium);
                    }

                    // Footer
                    col.Item().PaddingTop(12)
                        .Text($"ID: {ticket.Id}")
                        .FontSize(8).AlignCenter().FontColor(Colors.Grey.Lighten2);
                });
            });
        });

        return pdf.GeneratePdf();
    }
}