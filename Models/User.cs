using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NovaPass_API.Models;

[Table("users", Schema = "Novapass")]
[Index("Email", Name = "users_email_key", IsUnique = true)]
[Index("GoogleId", Name = "users_google_id_key", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("id")]
    [StringLength(36)]
    public string Id { get; set; } = null!;

    [Column("email")]
    [StringLength(255)]
    public string Email { get; set; } = null!;

    [Column("password_hash")]
    [StringLength(255)]
    public string? PasswordHash { get; set; }

    [Column("google_id")]
    [StringLength(255)]
    public string? GoogleId { get; set; }

    [Column("full_name")]
    [StringLength(255)]
    public string FullName { get; set; } = null!;

    [Column("phone")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Column("photo_url")]
    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    [Column("permissions")]
    [StringLength(100)]
    public string? Permissions { get; set; }

    [Column("role")]
    public UserRole Role { get; set; }

    [Column("is_active")]
    public short IsActive { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    [InverseProperty("User")]
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    [InverseProperty("User")]
    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    [InverseProperty("User")]
    public virtual ICollection<Pqr> Pqrs { get; set; } = new List<Pqr>();

    [InverseProperty("Admin")]
    public virtual ICollection<PqrsResponse> PqrsResponses { get; set; } = new List<PqrsResponse>();

    [InverseProperty("BuyerUser")]
    public virtual ICollection<Ticket> TicketBuyerUsers { get; set; } = new List<Ticket>();

    [InverseProperty("SoldBySeller")]
    public virtual ICollection<Ticket> TicketSoldBySellers { get; set; } = new List<Ticket>();

    [InverseProperty("User")]
    public virtual ICollection<TokenBlacklist> TokenBlacklists { get; set; } = new List<TokenBlacklist>();
}
