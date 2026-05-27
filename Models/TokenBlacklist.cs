using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NovaPass_API.Models;

[Table("token_blacklist")]
public partial class TokenBlacklist
{
    [Key]
    [Column("jti")]
    [StringLength(36)]
    public string Jti { get; set; } = null!;

    [Column("user_id")]
    [StringLength(36)]
    public string UserId { get; set; } = null!;

    [Column("invalidated_at", TypeName = "timestamp without time zone")]
    public DateTime InvalidatedAt { get; set; }

    [Column("expires_at", TypeName = "timestamp without time zone")]
    public DateTime ExpiresAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("TokenBlacklists")]
    public virtual User User { get; set; } = null!;
}
