using Microsoft.EntityFrameworkCore;
using NovaPass_API.Data;
using NovaPass_API.DTOs.Auth;
using NovaPass_API.DTOs.Pqrs;
using NovaPass_API.Infrastructure.MongoDB;
using NovaPass_API.Models;
using NovaPass_API.Services.Interfaces;

namespace NovaPass_API.Services;

public class PqrsService : IPqrsService
{
    private readonly TicketEventsDbContext _db;
    private readonly ILogService _log;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public PqrsService(
        TicketEventsDbContext db,
        ILogService log,
        IConfiguration config,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _log = log;
        _config = config;
        _http = httpClientFactory.CreateClient();
    }

    public async Task<PqrDto> CreateAsync(string userId, CreatePqrRequest request)
    {
        var validTypes = new[] { "question", "complaint", "claim", "suggestion" };
        if (!validTypes.Contains(request.Type.ToLower()))
            throw new AppException("Tipo inválido. Usa: question, complaint, claim, suggestion", 400);

        var pqr = new Pqr
        {
            UserId    = userId,
            Type      = request.Type.ToLower(),
            Status    = "pending",
            Message   = request.Message,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Pqrs.Add(pqr);
        await _db.SaveChangesAsync();

        return ToDto(pqr, []);
    }

    public async Task<PqrDto> GetByIdAsync(string pqrId, string userId, string role)
    {
        var pqr = await _db.Pqrs
            .Include(p => p.PqrsResponses)
            .FirstOrDefaultAsync(p => p.Id == pqrId)
            ?? throw new AppException("PQRS no encontrada", 404);

        if (role != "admin" && pqr.UserId != userId)
            throw new AppException("No autorizado", 403);

        return ToDto(pqr, pqr.PqrsResponses.ToList());
    }

    public async Task<List<PqrDto>> GetMyAsync(string userId)
    {
        var pqrs = await _db.Pqrs
            .Include(p => p.PqrsResponses)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return pqrs.Select(p => ToDto(p, p.PqrsResponses.ToList())).ToList();
    }

    public async Task<List<PqrDto>> GetAllAsync()
    {
        var pqrs = await _db.Pqrs
            .Include(p => p.PqrsResponses)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return pqrs.Select(p => ToDto(p, p.PqrsResponses.ToList())).ToList();
    }

    public async Task<PqrDto> RespondAsync(string pqrId, string adminId, RespondPqrRequest request)
    {
        var pqr = await _db.Pqrs
            .Include(p => p.PqrsResponses)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == pqrId)
            ?? throw new AppException("PQRS no encontrada", 404);

        if (pqr.Status == "closed")
            throw new AppException("No se puede responder una PQRS cerrada", 400);

        var validStatuses = new[] { "in_progress", "resolved" };
        if (!validStatuses.Contains(request.NewStatus.ToLower()))
            throw new AppException("Estado inválido. Usa: in_progress, resolved", 400);

        var response = new PqrsResponse
        {
            PqrsId    = pqrId,
            AdminId   = adminId,
            Message   = request.Message,
            CreatedAt = DateTime.UtcNow,
        };

        pqr.Status    = request.NewStatus.ToLower();
        pqr.UpdatedAt = DateTime.UtcNow;

        _db.PqrsResponses.Add(response);
        await _db.SaveChangesAsync();

        var webhookUrl = Environment.GetEnvironmentVariable("N8N_WEBHOOK_URL")
            ?? _config["N8N:WebhookUrl"];
        if (!string.IsNullOrEmpty(webhookUrl))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _http.PostAsJsonAsync($"{webhookUrl}/webhook/pqrs-respondida", new
                    {
                        email    = pqr.User?.Email,
                        type     = pqr.Type,
                        status   = request.NewStatus.ToLower(),
                        response = request.Message,
                    });
                }
                catch { }
            });
        }

        pqr.PqrsResponses.Add(response);
        return ToDto(pqr, pqr.PqrsResponses.ToList());
    }

    public async Task<PqrDto> CloseAsync(string pqrId, string adminId)
    {
        var pqr = await _db.Pqrs
            .Include(p => p.PqrsResponses)
            .FirstOrDefaultAsync(p => p.Id == pqrId)
            ?? throw new AppException("PQRS no encontrada", 404);

        if (pqr.Status == "closed")
            throw new AppException("La PQRS ya está cerrada", 400);

        pqr.Status    = "closed";
        pqr.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _log.LogSystemAsync("pqrs_cerrada", new { pqrs_id = pqrId, admin_id = adminId });

        return ToDto(pqr, pqr.PqrsResponses.ToList());
    }

    private static PqrDto ToDto(Pqr pqr, List<PqrsResponse> responses) =>
        new(
            pqr.Id,
            pqr.Type.ToString(),
            pqr.Status.ToString(),
            pqr.Message,
            pqr.CreatedAt,
            pqr.UpdatedAt,
            responses.Select(r => new PqrResponseDto(r.Id, r.Message, r.AdminId, r.CreatedAt)).ToList()
        );
}