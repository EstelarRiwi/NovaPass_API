using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NovaPass_API.Models;

namespace NovaPass_API.Data;

public partial class TicketEventsDbContext : DbContext
{
    public TicketEventsDbContext(DbContextOptions<TicketEventsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Pqr> Pqrs { get; set; }

    public virtual DbSet<PqrsResponse> PqrsResponses { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<TicketCategory> TicketCategories { get; set; }

    public virtual DbSet<TokenBlacklist> TokenBlacklists { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Novapass");
        
        modelBuilder
            .HasPostgresExtension("uuid-ossp")
            .HasPostgresEnum<UserRole>("user_role")
            .HasPostgresEnum<EventStatus>("event_status")
            .HasPostgresEnum<TicketStatus>("ticket_status")
            .HasPostgresEnum<PaymentStatus>("payment_status")
            .HasPostgresEnum<PqrsType>("pqrs_type")
            .HasPostgresEnum<PqrsStatus>("pqrs_status");

        
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("events_pkey");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .IsFixedLength();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.CreatedBy).IsFixedLength();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Events)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_events_created_by");
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.EventId }).HasName("favorites_pkey");

            entity.Property(e => e.UserId).IsFixedLength();
            entity.Property(e => e.EventId).IsFixedLength();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Event).WithMany(p => p.Favorites).HasConstraintName("fk_favorites_event");

            entity.HasOne(d => d.User).WithMany(p => p.Favorites).HasConstraintName("fk_favorites_user");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("password_reset_tokens_pkey");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .IsFixedLength();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Used).HasDefaultValue((short)0);
            entity.Property(e => e.UserId).IsFixedLength();

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens).HasConstraintName("fk_password_reset_tokens_user");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payments_pkey");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .IsFixedLength();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Currency).HasDefaultValueSql("'COP'::character varying");
            entity.Property(e => e.TicketId).IsFixedLength();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Ticket).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payments_ticket");
        });

        modelBuilder.Entity<Pqr>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pqrs_pkey");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .IsFixedLength();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UserId).IsFixedLength();

            entity.HasOne(d => d.User).WithMany(p => p.Pqrs)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_pqrs_user");
        });

        modelBuilder.Entity<PqrsResponse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pqrs_responses_pkey");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .IsFixedLength();
            entity.Property(e => e.AdminId).IsFixedLength();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.PqrsId).IsFixedLength();

            entity.HasOne(d => d.Admin).WithMany(p => p.PqrsResponses)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_pqrs_responses_admin");

            entity.HasOne(d => d.Pqrs).WithMany(p => p.PqrsResponses).HasConstraintName("fk_pqrs_responses_pqrs");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("seats_pkey");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .IsFixedLength();
            entity.Property(e => e.CategoryId).IsFixedLength();
            entity.Property(e => e.IsAvailable).HasDefaultValue((short)1);

            entity.HasOne(d => d.Category).WithMany(p => p.Seats).HasConstraintName("fk_seats_category");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tickets_pkey");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .IsFixedLength();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.BuyerUserId).IsFixedLength();
            entity.Property(e => e.CategoryId).IsFixedLength();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.EventId).IsFixedLength();
            entity.Property(e => e.SeatId).IsFixedLength();
            entity.Property(e => e.SoldBySellerId).IsFixedLength();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.BuyerUser).WithMany(p => p.TicketBuyerUsers)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_tickets_buyer");

            entity.HasOne(d => d.Category).WithMany(p => p.Tickets)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tickets_category");

            entity.HasOne(d => d.Event).WithMany(p => p.Tickets)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tickets_event");

            entity.HasOne(d => d.Seat).WithMany(p => p.Tickets)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_tickets_seat");

            entity.HasOne(d => d.SoldBySeller).WithMany(p => p.TicketSoldBySellers)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_tickets_seller");
        });

        modelBuilder.Entity<TicketCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ticket_categories_pkey");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .IsFixedLength();
            entity.Property(e => e.AvailableCapacity).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.EventId).IsFixedLength();
            entity.Property(e => e.TotalCapacity).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Event).WithMany(p => p.TicketCategories).HasConstraintName("fk_ticket_categories_event");
        });

        modelBuilder.Entity<TokenBlacklist>(entity =>
        {
            entity.HasKey(e => e.Jti).HasName("token_blacklist_pkey");

            entity.Property(e => e.Jti).IsFixedLength();
            entity.Property(e => e.InvalidatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UserId).IsFixedLength();

            entity.HasOne(d => d.User).WithMany(p => p.TokenBlacklists).HasConstraintName("fk_token_blacklist_user");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(uuid_generate_v4())::text")
                .IsFixedLength();
            entity.Property(e => e.Role)
                .HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue((short)1);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

