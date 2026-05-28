using NovaPass_API.DTOs.Reports;

namespace NovaPass_API.Services.Interfaces;

public interface IReportsService
{
    Task<SalesPerPeriodResponse> GetSalesPerPeriodAsync(DateTime from, DateTime to, string? groupBy = "week");
    Task<UsersRegisteredResponse> GetUsersRegisteredAsync(DateTime from, DateTime to);
    Task<OccupancyResponse> GetOccupancyByEventAsync(string? eventId = null);
    Task<SalesByCategoryResponse> GetSalesByCategoryAsync(DateTime? from = null, DateTime? to = null);
    Task<ValidatedEntriesDto> GetValidatedEntriesAsync(string eventId);
    Task<AuditLogResponse> GetAuditLogsAsync(int page, int perPage);
}