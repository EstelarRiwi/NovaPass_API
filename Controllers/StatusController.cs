using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaPass_API.Data;

namespace NovaPass_API.Controllers;

[ApiController]
[Route("api/debug")]
public class StatusController(TicketEventsDbContext db) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var result = new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow,
            ["db_can_connect"] = false,
            ["tables"] = new List<string>(),
            ["errors"] = new List<string>(),
        };

        try
        {
            result["db_can_connect"] = await db.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            ((List<string>)result["errors"]).Add($"CanConnect: {ex.Message}");
        }

        try
        {
            var tables = await db.Database.SqlQueryRaw<string>(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name")
                .ToListAsync();
            result["tables"] = tables;
        }
        catch (Exception ex)
        {
            ((List<string>)result["errors"]).Add($"ListTables: {ex.Message}");
        }

        try
        {
            var blacklistExists = await db.TokenBlacklists.AnyAsync();
            result["token_blacklist_has_rows"] = blacklistExists;
        }
        catch
        {
            result["token_blacklist_has_rows"] = false;
            ((List<string>)result["errors"]).Add("token_blacklist table not accessible");
        }

        return Ok(result);
    }

    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { status = "ok" });
}
