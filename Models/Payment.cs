using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NovaPass_API.Models;

[Table("payments")]
public partial class Payment
{
    [Key]
    [Column("id")]
    [StringLength(36)]
    public string Id { get; set; } = null!;

    [Column("ticket_id")]
    [StringLength(36)]
    public string TicketId { get; set; } = null!;

    [Column("mercadopago_preference_id")]
    [StringLength(255)]
    public string? MercadopagoPreferenceId { get; set; }

    [Column("mercadopago_payment_id")]
    [StringLength(255)]
    public string? MercadopagoPaymentId { get; set; }

    [Column("amount")]
    [Precision(12, 2)]
    public decimal Amount { get; set; }

    [Column("currency")]
    [StringLength(10)]
    public string Currency { get; set; } = null!;

    [Column("status")]
    public PaymentStatus Status { get; set; }

    [Column("webhook_received_at", TypeName = "timestamp without time zone")]
    public DateTime? WebhookReceivedAt { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("TicketId")]
    [InverseProperty("Payments")]
    public virtual Ticket Ticket { get; set; } = null!;
}
