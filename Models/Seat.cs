using System;
using System.Collections.Generic;

namespace NovaPass_API.Models;

public partial class Seat
{
    public int Id { get; set; }

    public int? CategoryId { get; set; }

    public string CodigoFila { get; set; } = null!;

    public int Numero { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual TicketCategory? Category { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
