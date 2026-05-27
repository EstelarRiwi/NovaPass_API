using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NovaPass_API.Models;

[Table("pqrs")]
public partial class Pqr
{
    [Key]
    [Column("id")]
    [StringLength(36)]
    public string Id { get; set; } = null!;

    [Column("user_id")]
    [StringLength(36)]
    public string UserId { get; set; } = null!;

    [Column("type", TypeName = "pqrs_type")]
    public PqrsType Type { get; set; }

    [Column("status", TypeName = "pqrs_status")]
    public PqrsStatus Status { get; set; }

    [Column("message")]
    public string Message { get; set; } = null!;

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Pqrs")]
    public virtual ICollection<PqrsResponse> PqrsResponses { get; set; } = new List<PqrsResponse>();

    [ForeignKey("UserId")]
    [InverseProperty("Pqrs")]
    public virtual User User { get; set; } = null!;
}
