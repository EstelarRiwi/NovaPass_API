using System;
using System.Collections.Generic;

namespace NovaPass_API.Models;

public partial class PqrsResponse
{
    public int Id { get; set; }

    public int? PqrsId { get; set; }

    public int? AdminId { get; set; }

    public string Respuesta { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual User? Admin { get; set; }

    public virtual Pqr? Pqrs { get; set; }
}
