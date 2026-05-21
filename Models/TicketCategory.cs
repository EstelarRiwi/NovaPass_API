using System;
using System.Collections.Generic;

namespace NovaPass_API.Models;

public partial class TicketCategory
{
    public int Id { get; set; }

    public int? EventId { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal Precio { get; set; }

    public int AforoTotal { get; set; }

    public int AforoDisponible { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Event? Event { get; set; }

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
