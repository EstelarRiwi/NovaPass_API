namespace NovaPass_API.DTOs.Events;

// ── Requests ────────────────────────────────────────────────────────────────

public record CreateEventRequest(
    string Title,
    string Description,
    DateTime Date,
    string Venue,
    string? ImageUrl,
    DateTime? SaleOpensAt,
    DateTime? SaleClosesAt,
    List<CreateCategoryRequest> Categories
);

public record UpdateEventRequest(
    string? Title,
    string? Description,
    DateTime? Date,
    string? Venue,
    string? ImageUrl,
    string? Status,           // "active" | "cancelled" | "sold_out"
    DateTime? SaleOpensAt,
    DateTime? SaleClosesAt
);

public record CreateCategoryRequest(
    string Name,
    decimal Price,
    int TotalCapacity
);

public record UpdateCategoryRequest(
    string? Name,
    decimal? Price,
    int? TotalCapacity
);

public record AddFavoriteRequest(string EventId);

// ── Responses ────────────────────────────────────────────────────────────────

public record CategorySummaryDto(
    string Id,
    string Name,
    decimal Price,
    int Available
);

public record CategoryDetailDto(
    string Id,
    string Name,
    decimal Price,
    int TotalCapacity,
    int Available
);

public record EventSummaryDto(
    string Id,
    string Title,
    string Description,
    DateTime Date,
    string Venue,
    string? ImageUrl,
    string Status,
    List<CategorySummaryDto> Categories
);

public record EventDetailDto(
    string Id,
    string Title,
    string Description,
    DateTime Date,
    string Venue,
    string? ImageUrl,
    string Status,
    DateTime SaleOpensAt,
    DateTime SaleClosesAt,
    List<CategoryDetailDto> Categories
);

public record EventListResponse(
    List<EventSummaryDto> Events,
    int Total,
    int Page,
    int PerPage
);

public record FavoriteItemDto(
    string Id,
    EventSummaryDto Event
);

public record FavoritesListResponse(List<FavoriteItemDto> Favorites);

public record AddFavoriteResponse(string Id, string EventId);