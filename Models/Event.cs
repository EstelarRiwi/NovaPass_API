using System;
using System.Collections.Generic;

namespace NovaPass_API.Models;

public partial class Event
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime Fecha { get; set; }

    public string Lugar { get; set; } = null!;

    public string? Imagen { get; set; }

    public string? Estado { get; set; }

    public DateTime FechaAperturaVenta { get; set; }

    public DateTime FechaCierreVenta { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<TicketCategory> TicketCategories { get; set; } = new List<TicketCategory>();
}
