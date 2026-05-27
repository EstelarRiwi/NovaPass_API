using NovaPass_API.DTOs.Pqrs;

namespace NovaPass_API.Services.Interfaces;

public interface IPqrsService
{
    Task<PqrDto> CreateAsync(string userId, CreatePqrRequest request);
    Task<PqrDto> GetByIdAsync(string pqrId, string userId, string role);
    Task<List<PqrDto>> GetAllAsync();
    Task<PqrDto> RespondAsync(string pqrId, string adminId, RespondPqrRequest request);
    Task<PqrDto> CloseAsync(string pqrId, string adminId);
}