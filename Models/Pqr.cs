using System;
using System.Collections.Generic;

namespace NovaPass_API.Models;

public partial class Pqr
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Tipo { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public string? Estado { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<PqrsResponse> PqrsResponses { get; set; } = new List<PqrsResponse>();

    public virtual User? User { get; set; }
}
