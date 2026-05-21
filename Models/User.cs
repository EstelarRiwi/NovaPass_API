using System;
using System.Collections.Generic;

namespace NovaPass_API.Models;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? GoogleId { get; set; }

    public string? Foto { get; set; }

    public string? Rol { get; set; }

    public List<string>? Permisos { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<Pqr> Pqrs { get; set; } = new List<Pqr>();

    public virtual ICollection<PqrsResponse> PqrsResponses { get; set; } = new List<PqrsResponse>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
