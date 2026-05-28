using NovaPass_API.DTOs.Events;

namespace NovaPass_API.Services.Interfaces;

public interface IEventsService
{
    // Cartelera pública (sin JWT)
    Task<EventListResponse> GetEventsAsync(int page, int perPage, string? status = null);
    Task<EventDetailDto> GetEventByIdAsync(string eventId);

    // Gestión de eventos (admin)
    Task<EventDetailDto> CreateEventAsync(CreateEventRequest request);
    Task<EventDetailDto> UpdateEventAsync(string eventId, UpdateEventRequest request);
    Task DeleteEventAsync(string eventId);

    // Categorías
    Task<CategoryDetailDto> AddCategoryAsync(string eventId, CreateCategoryRequest request);
    Task<CategoryDetailDto> UpdateCategoryAsync(string categoryId, UpdateCategoryRequest request);
    Task DeleteCategoryAsync(string categoryId);

    // Favoritos (requiere JWT — userId del token)
    Task<FavoritesListResponse> GetFavoritesAsync(string userId);
    Task<AddFavoriteResponse> AddFavoriteAsync(string userId, string eventId);
    Task RemoveFavoriteAsync(string userId, string favoriteId);
}