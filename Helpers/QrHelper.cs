using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using QRCoder;

namespace NovaPass_API.Helpers;

/// <summary>
/// Payload firmado criptográficamente que va dentro del QR.
/// Contenido: ticket_id, event_id, seat, expires_at.
/// </summary>
public record QrPayload(string TicketId, string EventId, string Seat, DateTime ExpiresAt);

public class QrHelper(IConfiguration config)
{
    private string GetSecret() =>
        Environment.GetEnvironmentVariable("QR_SECRET")
        ?? config["Qr:Secret"]
        ?? throw new InvalidOperationException("QR_SECRET no configurado");

    /// <summary>
    /// Genera un token JWT firmado con HMACSHA256 como contenido del QR.
    /// Retorna el string del token (no la imagen — la imagen se genera on-demand).
    /// </summary>
    public string GenerateSignedQr(QrPayload payload)
    {
        var secret = GetSecret();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("tid",  payload.TicketId),
            new Claim("eid",  payload.EventId),
            new Claim("seat", payload.Seat),
            new Claim(JwtRegisteredClaimNames.Exp,
                new DateTimeOffset(payload.ExpiresAt).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: payload.ExpiresAt,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Verifica la firma del token y retorna el payload.
    /// Lanza excepción si la firma es inválida o el token expiró.
    /// </summary>
    public QrPayload VerifyAndDecodeQr(string token)
    {
        var secret = GetSecret();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        }, out var validated);

        var jwt = (JwtSecurityToken)validated;
        var ticketId = jwt.Claims.First(c => c.Type == "tid").Value;
        var eventId  = jwt.Claims.First(c => c.Type == "eid").Value;
        var seat     = jwt.Claims.First(c => c.Type == "seat").Value;
        var exp      = jwt.ValidTo;

        return new QrPayload(ticketId, eventId, seat, exp);
    }

    /// <summary>
    /// Genera imagen PNG del QR a partir del token firmado.
    /// Retorna bytes PNG listos para embeber en el PDF.
    /// </summary>
    public byte[] GenerateQrImage(string qrToken)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(qrToken, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(10);
    }
}