using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NovaPass_API.Models;

[Table("pqrs_responses", Schema = "Novapass")]
public partial class PqrsResponse
{
    [Key]
    [Column("id")]
    [StringLength(36)]
    public string Id { get; set; } = null!;

    [Column("pqrs_id")]
    [StringLength(36)]
    public string PqrsId { get; set; } = null!;

    [Column("admin_id")]
    [StringLength(36)]
    public string AdminId { get; set; } = null!;

    [Column("message")]
    public string Message { get; set; } = null!;

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("AdminId")]
    [InverseProperty("PqrsResponses")]
    public virtual User Admin { get; set; } = null!;

    [ForeignKey("PqrsId")]
    [InverseProperty("PqrsResponses")]
    public virtual Pqr Pqrs { get; set; } = null!;
}
