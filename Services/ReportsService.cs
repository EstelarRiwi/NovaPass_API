using Microsoft.EntityFrameworkCore;
using NovaPass_API.Data;
using NovaPass_API.DTOs.Reports;
using NovaPass_API.Models;
using NovaPass_API.Services.Interfaces;

namespace NovaPass_API.Services;

public class ReportsService(TicketEventsDbContext db) : IReportsService
{
    public async Task<SalesPerPeriodResponse> GetSalesPerPeriodAsync(DateTime from, DateTime to, string? groupBy = "week")
    {
        var tickets = await db.Tickets
            .Where(t => t.Status == TicketStatus.active || t.Status == TicketStatus.used)
            .Where(t => t.CreatedAt >= from && t.CreatedAt <= to)
            .Include(t => t.Category)
            .ToListAsync();

        // Agrupar por semana o por día
        var grouped = groupBy == "week"
            ? tickets.GroupBy(t => GetWeekStart(t.CreatedAt))
            : tickets.GroupBy(t => t.CreatedAt.Date);

        var data = grouped.Select(g => new SalesPerPeriodDto(
            g.Key,
            groupBy == "week" ? g.Key.AddDays(6) : g.Key,
            g.Count(),
            g.Sum(t => t.Category?.Price ?? 0)
        )).OrderBy(x => x.PeriodStart).ToList();

        return new SalesPerPeriodResponse(data);
    }

    public async Task<UsersRegisteredResponse> GetUsersRegisteredAsync(DateTime from, DateTime to)
    {
        var rawUsers = await db.Users
            .Where(u => u.CreatedAt >= from && u.CreatedAt <= to && u.Role == UserRole.customer)
            .ToListAsync();

        var users = rawUsers
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new UsersRegisteredDto(g.Key, g.Count()))
            .OrderBy(x => x.Date)
            .ToList();

        return new UsersRegisteredResponse(users);
    }

    public async Task<OccupancyResponse> GetOccupancyByEventAsync(string? eventId = null)
    {
        var query = db.Events
            .Include(e => e.TicketCategories)
                .ThenInclude(c => c.Tickets)
            .AsQueryable();

        if (eventId != null)
            query = query.Where(e => e.Id == eventId);

        var events = await query.ToListAsync();

        var data = events.Select(ev =>
        {
            var categories = ev.TicketCategories.Select(c =>
            {
                var sold = c.Tickets.Count(t => t.Status == TicketStatus.active || t.Status == TicketStatus.used);
                return new OccupancyCategoryDto(
                    c.Id, c.Name, sold, c.TotalCapacity,
                    c.TotalCapacity > 0 ? Math.Round((double)sold / c.TotalCapacity * 100, 2) : 0);
            }).ToList();

            var totalSold = categories.Sum(c => c.Sold);
            var totalCap = categories.Sum(c => c.TotalCapacity);

            return new OccupancyByEventDto(
                ev.Id, ev.Name, categories, totalSold, totalCap,
                totalCap > 0 ? Math.Round((double)totalSold / totalCap * 100, 2) : 0);
        }).ToList();

        return new OccupancyResponse(data);
    }

    public async Task<SalesByCategoryResponse> GetSalesByCategoryAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = db.Tickets
            .Where(t => t.Status == TicketStatus.active || t.Status == TicketStatus.used)
            .Include(t => t.Category)
                .ThenInclude(c => c!.Event)
            .AsQueryable();

        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue)   query = query.Where(t => t.CreatedAt <= to.Value);

        var tickets = await query.ToListAsync();

        var grouped = tickets.GroupBy(t => new { t.CategoryId, t.EventId });
        var data = grouped.Select(g =>
        {
            var first = g.First();
            return new SalesByCategoryDto(
                first.EventId,
                first.Category?.Event?.Name ?? "",
                g.Key.CategoryId,
                first.Category?.Name ?? "",
                g.Count(),
                g.Sum(t => t.Category?.Price ?? 0));
        }).ToList();

        return new SalesByCategoryResponse(data);
    }

    public async Task<ValidatedEntriesDto> GetValidatedEntriesAsync(string eventId)
    {
        var ev = await db.Events.FindAsync(eventId)
            ?? throw new Exception("Event not found");

        var usedCount = await db.Tickets
            .CountAsync(t => t.EventId == eventId && t.Status == TicketStatus.used);

        var totalCount = await db.Tickets
            .CountAsync(t => t.EventId == eventId && t.Status != TicketStatus.pending);

        return new ValidatedEntriesDto(eventId, ev.Name, usedCount, totalCount);
    }

    public async Task<AuditLogResponse> GetAuditLogsAsync(int page, int perPage)
    {
        // Los logs de auditoría vienen del Middleware de Isa (MongoDB).
        // Por ahora se retorna vacío — se conecta cuando Isa entregue el middleware.
        // TODO: Conectar con ILogService.GetAuditLogsAsync cuando esté disponible.
        return new AuditLogResponse([], 0, page, perPage);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}