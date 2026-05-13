 using System.Text;
using Giwu.Api.Common;
using Giwu.Api.Hangfire;
using Giwu.Api.Jobs;
using Giwu.Api.Middleware;
using Giwu.Api.Realtime;
using Giwu.Application.Notifications;
using Giwu.Application;
using Giwu.Application.Common;
using Giwu.Infrastructure;
using Giwu.Infrastructure.Auth;
using Giwu.Infrastructure.Persistence;
using Giwu.Infrastructure.Persistence.Seed;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .Enrich.FromLogContext());

// ── Services ───────────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

builder.Services.AddEndpoints();

// JWT auth
var auth = builder.Configuration.GetSection("Auth").Get<JwtOptions>()!;
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = auth.Issuer,
            ValidateAudience         = true,
            ValidAudience            = auth.Audience,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.Key)),
            ClockSkew                = TimeSpan.FromSeconds(30),
        };

        // SignalR sends the JWT via querystring (?access_token=...) when
        // upgrading to WebSocket, because browsers won't let you set
        // custom headers on the WS handshake. Pull it from the query
        // string for the /hubs/* paths only.
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                var path  = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(o => o.RegisterPermissionPolicies());

// CORS for the MAUI client (any origin during dev, locked-down list in prod)
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// Hangfire (jobs share the Postgres instance, separate database)
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(opt =>
        opt.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Hangfire"))));
builder.Services.AddHangfireServer();
builder.Services.AddScoped<OutboxDispatcherJob>();
builder.Services.AddScoped<AttendanceRollupJob>();

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Db")!, name: "postgres");

// OpenAPI + Scalar UI
builder.Services.AddOpenApi();

// SignalR + the in-app notification realtime push
builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, JwtUserIdProvider>();
builder.Services.AddScoped<INotificationBroadcaster, SignalRNotificationBroadcaster>();

// ── Pipeline ───────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();   // /scalar/v1
}

app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

// Hangfire dashboard at /hangfire (requires Settings.Manage)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorization() }
});

// Recurring jobs
RecurringJob.AddOrUpdate<OutboxDispatcherJob>(
    "outbox-dispatcher", j => j.RunAsync(CancellationToken.None), "*/1 * * * *");
RecurringJob.AddOrUpdate<AttendanceRollupJob>(
    "attendance-rollup", j => j.RunAsync(CancellationToken.None), "0 1 * * *"); // 1am daily

app.MapEndpoints();

// Real-time notification hub. Clients connect with an access_token in
// the query string (see JwtBearer.OnMessageReceived above).
app.MapHub<NotificationsHub>("/hubs/notifications").RequireAuthorization();

// ── DB migrate + seed on startup (DEV only) ────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db      = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var hasher  = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var tenant  = scope.ServiceProvider.GetRequiredService<ITenantContext>();
    await db.Database.MigrateAsync();
    await Seeder.SeedAsync(db, hasher, tenant);

    // Optional sample dataset for local dev. Idempotent — only runs on an
    // empty DB. Comment out to keep the app starting empty.
    await SampleDataSeeder.SeedAsync(db, hasher);
}

app.Run();

public partial class Program;   // for WebApplicationFactory in tests
