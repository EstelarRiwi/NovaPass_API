using Microsoft.AspNetCore.Mvc;

namespace NovaPass_API.Controllers;

[ApiController]
[Route("api/chatbot")]
public class ChatbotController : ControllerBase
{
    private static readonly (string[] Keywords, string Response)[] Responses =
    [
        (["hola"], "¡Hola! Soy Nova, tu asistente de NovaPass. ¿En qué puedo ayudarte? Puedes preguntarme sobre eventos, compras, entradas o PQRS."),
        (["evento"], "Puedes explorar todos los eventos disponibles en nuestra página principal. Cada evento muestra su fecha, ubicación y categorías de boletas disponibles."),
        (["compra"], "Para comprar boletas, selecciona un evento, elige la categoría y cantidad, y haz clic en 'Comprar Ahora'. Te redirigiremos a la pasarela de pago."),
        (["boleto", "entrada", "ticket"], "Tus entradas activas las encuentras en 'Mis Entradas'. Desde allí puedes ver el código QR y descargar el PDF de cada boleta."),
        (["qr"], "El código QR de tu boleta se muestra al hacer clic en 'Ver QR' en Mis Entradas. Este QR es necesario para ingresar al evento."),
        (["pago", "pagar"], "Aceptamos pagos a través de MercadoPago con tarjetas de crédito, débito y otros métodos disponibles en la plataforma."),
        (["favorito"], "Puedes guardar eventos como favoritos haciendo clic en el corazón en la tarjeta del evento. Luego los encuentras en 'Mis Favoritos'."),
        (["perfil"], "En 'Mi Perfil' puedes actualizar tu nombre, email y foto de perfil."),
        (["pqrs"], "En la sección PQRS puedes enviar preguntas, quejas, reclamos o sugerencias. Recibirás respuesta del equipo de soporte."),
        (["contacto", "soporte"], "Puedes contactar al soporte a través de la sección PQRS o escribiendo a soporte@novapass.com."),
    ];

    [HttpPost]
    public IActionResult Chat([FromBody] ChatbotRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return Ok(new ChatbotResponse("Por favor escribe un mensaje."));

        var lower = request.Message.ToLower();

        foreach (var (keywords, response) in Responses)
        {
            if (keywords.Any(k => lower.Contains(k, StringComparison.OrdinalIgnoreCase)))
                return Ok(new ChatbotResponse(response));
        }

        return Ok(new ChatbotResponse(
            "No tengo información específica sobre eso. ¿Puedes preguntarme sobre eventos, compras, entradas, PQRS u otros temas?"));
    }
}

public record ChatbotRequest(string Message);
public record ChatbotResponse(string Response);
