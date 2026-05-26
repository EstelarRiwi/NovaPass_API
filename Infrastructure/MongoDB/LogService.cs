using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace NovaPass_API.Infrastructure.MongoDB;

public class LogService : ILogService
{
    private readonly IMongoCollection<BsonDocument> _logsAuth;
    private readonly IMongoCollection<BsonDocument> _logsTickets;
    private readonly IMongoCollection<BsonDocument> _logsValidation;
    private readonly IMongoCollection<BsonDocument> _logsSystem;

    public LogService(IOptions<MongoSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.Database);

        _logsAuth       = db.GetCollection<BsonDocument>("logs_auth");
        _logsTickets    = db.GetCollection<BsonDocument>("logs_tickets");
        _logsValidation = db.GetCollection<BsonDocument>("logs_validation");
        _logsSystem     = db.GetCollection<BsonDocument>("logs_system");
    }

    public Task LogAuthAsync(string type, object payload, string? userId = null) =>
        InsertLog(_logsAuth, type, payload, userId: userId);

    public Task LogTicketAsync(string type, object payload, string? userId = null) =>
        InsertLog(_logsTickets, type, payload, userId: userId);

    public Task LogValidationAsync(string type, object payload, string? employeeId = null) =>
        InsertLog(_logsValidation, type, payload, employeeId: employeeId);

    public Task LogSystemAsync(string type, object payload) =>
        InsertLog(_logsSystem, type, payload);

    private static Task InsertLog(
        IMongoCollection<BsonDocument> collection,
        string type,
        object payload,
        string? userId = null,
        string? employeeId = null)
    {
        var doc = new BsonDocument
        {
            { "timestamp",   BsonValue.Create(DateTime.UtcNow) },
            { "type",        type },
            { "payload",     payload.ToBsonDocument() }
        };

        if (userId     != null) doc.Add("user_id",     userId);
        if (employeeId != null) doc.Add("employee_id", employeeId);

        return collection.InsertOneAsync(doc);
    }
}