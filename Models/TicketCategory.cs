using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NovaPass_API.Models;

[Table("ticket_categories", Schema = "Novapass")]
public partial class TicketCategory
{
    [Key]
    [Column("id")]
    [StringLength(36)]
    public string Id { get; set; } = null!;

    [Column("event_id")]
    [StringLength(36)]
    public string EventId { get; set; } = null!;

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("price")]
    [Precision(12, 2)]
    public decimal Price { get; set; }

    [Column("total_capacity")]
    public int TotalCapacity { get; set; }

    [Column("available_capacity")]
    public int AvailableCapacity { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("EventId")]
    [InverseProperty("TicketCategories")]
    public virtual Event Event { get; set; } = null!;

    [InverseProperty("Category")]
    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    [InverseProperty("Category")]
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
