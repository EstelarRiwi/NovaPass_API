namespace NovaPass_API.DTOs.Pqrs;

public record CreatePqrRequest(string Type, string Message);

public record RespondPqrRequest(string Message, string NewStatus);

public record PqrDto(
    string Id,
    string Type,
    string Status,
    string Message,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<PqrResponseDto> Responses
);

public record PqrResponseDto(
    string Id,
    string Message,
    string AdminId,
    DateTime CreatedAt
);