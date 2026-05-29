using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NovaPass_API.Helpers;
using NovaPass_API.Data;
using NovaPass_API.Services;
using NovaPass_API.Services.Interfaces;
using Scalar.AspNetCore;
using NovaPass_API.Infrastructure.MongoDB;
using Npgsql;
using NovaPass_API.Models;
using MercadoPago.Config;

var builder = WebApplication.CreateBuilder(args);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);



builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();



var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                       ?? builder.Configuration.GetConnectionString("PostgreSQL")
                       ?? throw new InvalidOperationException("La cadena de conexión no está configurada");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableUnmappedTypes();
dataSourceBuilder.MapEnum<UserRole>("user_role");
dataSourceBuilder.MapEnum<EventStatus>("event_status");
dataSourceBuilder.MapEnum<TicketStatus>("ticket_status");
dataSourceBuilder.MapEnum<PaymentStatus>("payment_status");
dataSourceBuilder.MapEnum<PqrsType>("pqrs_type");
dataSourceBuilder.MapEnum<PqrsStatus>("pqrs_status");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<TicketEventsDbContext>(options => 
    options.UseNpgsql(dataSource));


builder.Services.AddHttpClient();


builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventsService, EventsService>();
builder.Services.AddScoped<ITicketsService, TicketsService>();
builder.Services.AddScoped<IPqrsService, PqrsService>();
builder.Services.AddScoped<PaymentService>();


var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["Jwt:Secret"]!;
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"]!;

var mpAccessToken = builder.Configuration["MercadoPago:AccessToken"];
if (!string.IsNullOrWhiteSpace(mpAccessToken))
{
    MercadoPagoConfig.AccessToken = mpAccessToken;
}

builder.Services.Configure<MongoSettings>(options =>
{
    options.ConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
        ?? builder.Configuration["MongoDB:ConnectionString"]
        ?? string.Empty;
    options.Database = Environment.GetEnvironmentVariable("MONGODB_DATABASE")
        ?? builder.Configuration["MongoDB:Database"]
        ?? "novapass_logs";
});

builder.Services.AddSingleton<ILogService, LogService>();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false; 
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = "role",  
        NameClaimType = "sub",
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            try
            {
                var jti = context.Principal?.FindFirst("jti")?.Value;
                if (jti != null)
                {
                    var db = context.HttpContext.RequestServices
                        .GetRequiredService<TicketEventsDbContext>();
                    var revoked = await db.TokenBlacklists.AnyAsync(t => t.Jti == jti);
                    if (revoked) context.Fail("Token has been revoked");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[JwtEvents] Error checking blacklist: {ex.Message}");
            }
        }
    };
});


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});


var corsOrigins = new List<string>
{
    "http://localhost:3001",
    "http://localhost:3002",
    "http://localhost:3003",
    "http://localhost:3004",
    "http://localhost:8000",
    "http://localhost:5173",
    "http://localhost:5174",
    "http://5.189.174.154:9000",
    "http://5.189.174.154:9001",
    "http://5.189.174.154:9002",
    "http://5.189.174.154:9003",
};

var extraOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")
    ?? builder.Configuration["Cors:Origins"];

if (!string.IsNullOrWhiteSpace(extraOrigins))
    corsOrigins.AddRange(extraOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

builder.Services.AddCors(options =>
{
    options.AddPolicy("EstelarPolicy", policy =>
        policy
            .WithOrigins([.. corsOrigins])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

 
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    options.AddPolicy("register", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseStaticFiles();       
app.UseCors("EstelarPolicy");

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        Console.Error.WriteLine($"[Global] Error no controlado: {ex}");
        await context.Response.WriteAsJsonAsync(new { message = "Error interno del servidor" });
    });
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TicketEventsDbContext>();
    try { db.Database.EnsureCreated(); }
    catch (Exception ex) { Console.Error.WriteLine($"[Startup] EnsureCreated: {ex.Message}"); }
}

app.Run();
