using System;
using System.Collections.Generic;

namespace NovaPass_API.Models;

public partial class Ticket
{
    public int Id { get; set; }

    public string QrToken { get; set; } = null!;

    public int? UserId { get; set; }

    public string CompradorCorreo { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int? SeatId { get; set; }

    public string? Estado { get; set; }

    public string? Vendedor { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual TicketCategory? Category { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Seat? Seat { get; set; }

    public virtual User? User { get; set; }
}
