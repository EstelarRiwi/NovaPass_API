using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NovaPass_API.Models;

[Table("seats", Schema = "Novapass")]
public partial class Seat
{
    [Key]
    [Column("id")]
    [StringLength(36)]
    public string Id { get; set; } = null!;

    [Column("category_id")]
    [StringLength(36)]
    public string CategoryId { get; set; } = null!;

    [Column("row_code")]
    [StringLength(10)]
    public string? RowCode { get; set; }

    [Column("seat_number")]
    public int SeatNumber { get; set; }

    [Column("is_available")]
    public short IsAvailable { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("Seats")]
    public virtual TicketCategory Category { get; set; } = null!;

    [InverseProperty("Seat")]
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
