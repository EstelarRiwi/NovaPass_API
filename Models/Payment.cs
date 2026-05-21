using System;
using System.Collections.Generic;

namespace NovaPass_API.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int? TicketId { get; set; }

    public string MercadoPagoRef { get; set; } = null!;

    public string EstadoTransaccion { get; set; } = null!;

    public decimal Monto { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Ticket? Ticket { get; set; }
}
