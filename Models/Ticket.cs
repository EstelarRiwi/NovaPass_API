using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NovaPass_API.Models;

[Table("tickets")]
public partial class Ticket
{
    [Key]
    [Column("id")]
    [StringLength(36)]
    public string Id { get; set; } = null!;

    [Column("event_id")]
    [StringLength(36)]
    public string EventId { get; set; } = null!;

    [Column("category_id")]
    [StringLength(36)]
    public string CategoryId { get; set; } = null!;

    [Column("seat_id")]
    [StringLength(36)]
    public string? SeatId { get; set; }

    [Column("buyer_user_id")]
    [StringLength(36)]
    public string? BuyerUserId { get; set; }

    [Column("qr_token")]
    public string? QrToken { get; set; }

    [Column("payment_reference")]
    [StringLength(255)]
    public string? PaymentReference { get; set; }

    [Column("sold_by_seller_id")]
    [StringLength(36)]
    public string? SoldBySellerId { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("purchased_at", TypeName = "timestamp without time zone")]
    public DateTime? PurchasedAt { get; set; }

    [Column("activated_at", TypeName = "timestamp without time zone")]
    public DateTime? ActivatedAt { get; set; }

    [Column("used_at", TypeName = "timestamp without time zone")]
    public DateTime? UsedAt { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("BuyerUserId")]
    [InverseProperty("TicketBuyerUsers")]
    public virtual User? BuyerUser { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("Tickets")]
    public virtual TicketCategory Category { get; set; } = null!;

    [ForeignKey("EventId")]
    [InverseProperty("Tickets")]
    public virtual Event Event { get; set; } = null!;

    [InverseProperty("Ticket")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [ForeignKey("SeatId")]
    [InverseProperty("Tickets")]
    public virtual Seat? Seat { get; set; }

    [ForeignKey("SoldBySellerId")]
    [InverseProperty("TicketSoldBySellers")]
    public virtual User? SoldBySeller { get; set; }
}
