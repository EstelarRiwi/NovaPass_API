using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NovaPass_API.Models;

[Table("events")]
public partial class Event
{
    [Key]
    [Column("id")]
    [StringLength(36)]
    public string Id { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("venue")]
    [StringLength(255)]
    public string? Venue { get; set; }

    [Column("event_date", TypeName = "timestamp without time zone")]
    public DateTime EventDate { get; set; }

    [Column("image_url")]
    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [Column("sale_opens_at", TypeName = "timestamp without time zone")]
    public DateTime? SaleOpensAt { get; set; }

    [Column("sale_closes_at", TypeName = "timestamp without time zone")]
    public DateTime? SaleClosesAt { get; set; }

    [Column("status", TypeName = "event_status")]
    public EventStatus Status { get; set; }

    [Column("created_by")]
    [StringLength(36)]
    public string CreatedBy { get; set; } = null!;

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CreatedBy")]
    [InverseProperty("Events")]
    public virtual User CreatedByNavigation { get; set; } = null!;

    [InverseProperty("Event")]
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    [InverseProperty("Event")]
    public virtual ICollection<TicketCategory> TicketCategories { get; set; } = new List<TicketCategory>();

    [InverseProperty("Event")]
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
