namespace NovaPass_API.Common;

/// <summary>
/// Envelope estándar de la API Estelar.
/// Todas las respuestas JSON siguen esta estructura:
/// { "success": true|false, "data": {...}|null, "error": null|{...} }
/// Excepción: /tickets/:id/pdf retorna binario sin envelope.
/// </summary>
public class ApiResponse
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public ApiError? Error { get; init; }

    public static ApiResponse Ok(object? data) => new()
    {
        Success = true,
        Data = data,
        Error = null
    };

    public static ApiResponse Fail(string code, string? message = null, object? fields = null) => new()
    {
        Success = false,
        Data = null,
        Error = new ApiError(code, message ?? code, fields)
    };
}

public record ApiError(string Code, string Message, object? Fields = null);