namespace NovaPass_API.DTOs.Reports;

// ── Responses ────────────────────────────────────────────────────────────────

public record SalesPerPeriodDto(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int TotalTickets,
    decimal TotalRevenue
);

public record SalesPerPeriodResponse(List<SalesPerPeriodDto> Data);

public record UsersRegisteredDto(DateTime Date, int Count);

public record UsersRegisteredResponse(List<UsersRegisteredDto> Data);

public record OccupancyByEventDto(
    string EventId,
    string EventTitle,
    List<OccupancyCategoryDto> Categories,
    int TotalSold,
    int TotalCapacity,
    double OccupancyPercent
);

public record OccupancyCategoryDto(
    string CategoryId,
    string CategoryName,
    int Sold,
    int TotalCapacity,
    double OccupancyPercent
);

public record OccupancyResponse(List<OccupancyByEventDto> Data);

public record SalesByCategoryDto(
    string EventId,
    string EventTitle,
    string CategoryId,
    string CategoryName,
    int TicketsSold,
    decimal Revenue
);

public record SalesByCategoryResponse(List<SalesByCategoryDto> Data);

public record ValidatedEntriesDto(
    string EventId,
    string EventTitle,
    int ValidatedCount,
    int TotalTickets
);

public record AuditLogDto(
    string Id,
    string UserId,
    string Action,
    string Entity,
    string? EntityId,
    DateTime Timestamp,
    string? IpAddress
);

public record AuditLogResponse(
    List<AuditLogDto> Data,
    int Total,
    int Page,
    int PerPage
);