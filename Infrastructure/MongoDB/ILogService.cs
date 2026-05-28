namespace NovaPass_API.Infrastructure.MongoDB;

public interface ILogService
{
    Task LogAuthAsync(string type, object payload, string? userId = null);
    Task LogTicketAsync(string type, object payload, string? userId = null);
    Task LogValidationAsync(string type, object payload, string? employeeId = null);
    Task LogSystemAsync(string type, object payload, string? userId = null);
}