using Microsoft.EntityFrameworkCore;
using NovaPass_API.Data;
using NovaPass_API.DTOs.Auth;
using NovaPass_API.DTOs.Events;
using NovaPass_API.Infrastructure.MongoDB;
using NovaPass_API.Models;
using NovaPass_API.Services.Interfaces;

namespace NovaPass_API.Services;

public class EventsService(
    TicketEventsDbContext db,
    IConfiguration config,
    IHttpClientFactory httpFactory,
    ILogService log) : IEventsService
{
    // ── Helpers privados ────────────────────────────────────────────────────

    private static EventSummaryDto ToSummary(Event e) => new(
        e.Id,
        e.Name,
        e.Description ?? string.Empty,
        e.EventDate,
        e.Venue ?? string.Empty,
        e.ImageUrl,
        e.Status.ToString(),
        e.TicketCategories.Select(c => new CategorySummaryDto(
            c.Id, c.Name, c.Price, c.AvailableCapacity)).ToList());

    private static EventDetailDto ToDetail(Event e) => new(
        e.Id,
        e.Name,
        e.Description ?? string.Empty,
        e.EventDate,
        e.Venue ?? string.Empty,
        e.ImageUrl,
        e.Status.ToString(),
        e.SaleOpensAt ?? DateTime.MinValue,
        e.SaleClosesAt ?? DateTime.MinValue,
        e.TicketCategories.Select(c => new CategoryDetailDto(
            c.Id, c.Name, c.Price, c.TotalCapacity, c.AvailableCapacity)).ToList());

    private async Task PublishN8nAsync(string eventName, object payload)
    {
        try
        {
            var webhookUrl = Environment.GetEnvironmentVariable("N8N_WEBHOOK_URL")
                             ?? config["N8N:WebhookUrl"];
            if (string.IsNullOrEmpty(webhookUrl)) return;

            var client = httpFactory.CreateClient();
            await client.PostAsJsonAsync($"{webhookUrl}/{eventName}", payload);
        }
        catch (Exception ex)
        {
            await log.LogSystemAsync("n8n_publish_error", new { eventName, error = ex.Message });
        }
    }

    // ── Cartelera pública ───────────────────────────────────────────────────

    public async Task<EventListResponse> GetEventsAsync(int page, int perPage, string? status = null)
    {
        var query = db.Events
            .Include(e => e.TicketCategories)
            .AsQueryable();

        if (status != null && Enum.TryParse<EventStatus>(status, out var statusEnum))
            query = query.Where(e => e.Status == statusEnum);
        else
            query = query
                .Where(e => e.Status != EventStatus.cancelled)
                .Where(e => e.EventDate >= DateTime.UtcNow);

        var total = await query.CountAsync();
        var events = await query
            .OrderBy(e => e.EventDate)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return new EventListResponse(events.Select(ToSummary).ToList(), total, page, perPage);
    }

    public async Task<EventDetailDto> GetEventByIdAsync(string eventId)
    {
        var ev = await db.Events
            .Include(e => e.TicketCategories)
            .FirstOrDefaultAsync(e => e.Id == eventId)
            ?? throw new AppException("Event not found", 404);

        return ToDetail(ev);
    }

    // ── Gestión de eventos (admin) ──────────────────────────────────────────

    public async Task<EventDetailDto> CreateEventAsync(CreateEventRequest request)
    {
        var ev = new Event
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Title,
            Description = request.Description,
            EventDate = request.Date,
            Venue = request.Venue,
            ImageUrl = request.ImageUrl,
            Status = EventStatus.active,
            SaleOpensAt = request.SaleOpensAt,
            SaleClosesAt = request.SaleClosesAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.Events.Add(ev);

        foreach (var cat in request.Categories)
        {
            db.TicketCategories.Add(new TicketCategory
            {
                Id = Guid.NewGuid().ToString(),
                EventId = ev.Id,
                Name = cat.Name,
                Price = cat.Price,
                TotalCapacity = cat.TotalCapacity,
                AvailableCapacity = cat.TotalCapacity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();

        // Recarga con categorías para retornar el DTO completo
        var created = await db.Events
            .Include(e => e.TicketCategories)
            .FirstAsync(e => e.Id == ev.Id);

        return ToDetail(created);
    }

    public async Task<EventDetailDto> UpdateEventAsync(string eventId, UpdateEventRequest request)
    {
        var ev = await db.Events
            .Include(e => e.TicketCategories)
            .FirstOrDefaultAsync(e => e.Id == eventId)
            ?? throw new AppException("Event not found", 404);

        var previousStatus = ev.Status;

        if (request.Title != null)       ev.Name = request.Title;
        if (request.Description != null) ev.Description = request.Description;
        if (request.Date.HasValue)       ev.EventDate = request.Date.Value;
        if (request.Venue != null)       ev.Venue = request.Venue;
        if (request.ImageUrl != null)    ev.ImageUrl = request.ImageUrl;
        if (request.SaleOpensAt.HasValue)  ev.SaleOpensAt = request.SaleOpensAt.Value;
        if (request.SaleClosesAt.HasValue) ev.SaleClosesAt = request.SaleClosesAt.Value;

        if (request.Status != null && Enum.TryParse<EventStatus>(request.Status, out var newStatus))
            ev.Status = newStatus;

        ev.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Notificar a n8n si el evento fue cancelado
        if (ev.Status == EventStatus.cancelled && previousStatus != EventStatus.cancelled)
        {
            var buyers = await db.Tickets
                .Where(t => t.EventId == eventId && t.Status == TicketStatus.active)
                .Include(t => t.BuyerUser)
                .Select(t => new { t.BuyerUser!.Email, t.BuyerUser.FullName })
                .Distinct()
                .ToListAsync();

            await PublishN8nAsync("evento_cancelado", new
            {
                event_id = eventId,
                event_title = ev.Name,
                buyers
            });
        }
        else if (request.Title != null || request.Date.HasValue || request.Venue != null)
        {
            // Notificar a usuarios que tienen este evento en favoritos
            var favUsers = await db.Favorites
                .Where(f => f.EventId == eventId)
                .Include(f => f.User)
                .Select(f => new { f.User.Email, f.User.FullName })
                .ToListAsync();

            if (favUsers.Count > 0)
            {
                await PublishN8nAsync("evento_actualizado", new
                {
                    event_id = eventId,
                    event_title = ev.Name,
                    users = favUsers
                });
            }
        }

        return ToDetail(ev);
    }

    public async Task DeleteEventAsync(string eventId)
    {
        var ev = await db.Events.FindAsync(eventId)
            ?? throw new AppException("Event not found", 404);
        ev.Status = EventStatus.cancelled;
        ev.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    // ── Categorías ──────────────────────────────────────────────────────────

    public async Task<CategoryDetailDto> AddCategoryAsync(string eventId, CreateCategoryRequest request)
    {
        var ev = await db.Events.FindAsync(eventId)
            ?? throw new AppException("Event not found", 404);

        var cat = new TicketCategory
        {
            Id = Guid.NewGuid().ToString(),
            EventId = eventId,
            Name = request.Name,
            Price = request.Price,
            TotalCapacity = request.TotalCapacity,
            AvailableCapacity = request.TotalCapacity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.TicketCategories.Add(cat);
        await db.SaveChangesAsync();

        return new CategoryDetailDto(cat.Id, cat.Name, cat.Price, cat.TotalCapacity, cat.AvailableCapacity);
    }

    public async Task<CategoryDetailDto> UpdateCategoryAsync(string categoryId, UpdateCategoryRequest request)
    {
        var cat = await db.TicketCategories.FindAsync(categoryId)
            ?? throw new AppException("Category not found", 404);

        if (request.Name != null)             cat.Name = request.Name;
        if (request.Price.HasValue)           cat.Price = request.Price.Value;
        if (request.TotalCapacity.HasValue)   cat.TotalCapacity = request.TotalCapacity.Value;
        cat.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return new CategoryDetailDto(cat.Id, cat.Name, cat.Price, cat.TotalCapacity, cat.AvailableCapacity);
    }

    public async Task DeleteCategoryAsync(string categoryId)
    {
        var cat = await db.TicketCategories.FindAsync(categoryId)
            ?? throw new AppException("Category not found", 404);
        db.TicketCategories.Remove(cat);
        await db.SaveChangesAsync();
    }

    // ── Favoritos ───────────────────────────────────────────────────────────

    public async Task<FavoritesListResponse> GetFavoritesAsync(string userId)
    {
        var favorites = await db.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Event)
                .ThenInclude(e => e.TicketCategories)
            .ToListAsync();

        var dtos = favorites.Select(f => new FavoriteItemDto(
            f.EventId,   // se usa EventId como "id del favorito" (PK compuesta)
            ToSummary(f.Event))).ToList();

        return new FavoritesListResponse(dtos);
    }

    public async Task<AddFavoriteResponse> AddFavoriteAsync(string userId, string eventId)
    {
        var exists = await db.Favorites.AnyAsync(f => f.UserId == userId && f.EventId == eventId);
        if (exists) throw new AppException("Already in favorites", 409);

        var ev = await db.Events.FindAsync(eventId)
            ?? throw new AppException("Event not found", 404);

        var fav = new Favorite
        {
            UserId = userId,
            EventId = eventId,
            CreatedAt = DateTime.UtcNow
        };

        db.Favorites.Add(fav);
        await db.SaveChangesAsync();

        return new AddFavoriteResponse(eventId, eventId);
    }

    public async Task RemoveFavoriteAsync(string userId, string favoriteId)
    {
        // favoriteId corresponde al EventId (la PK es compuesta UserId+EventId)
        var fav = await db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.EventId == favoriteId)
            ?? throw new AppException("Favorite not found", 404);

        db.Favorites.Remove(fav);
        await db.SaveChangesAsync();
    }
}