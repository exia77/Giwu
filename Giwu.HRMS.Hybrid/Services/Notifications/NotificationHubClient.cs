using Giwu.Contracts.Notifications;
using Giwu.HRMS.Hybrid.Services.Auth;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Giwu.HRMS.Hybrid.Services.Notifications;

/// <summary>
/// Long-lived SignalR connection to /hubs/notifications. Exposes a
/// NotificationReceived event so the layout can refresh the bell badge
/// the moment the server pushes a new notification.
/// </summary>
public sealed class NotificationHubClient : IAsyncDisposable
{
    private readonly ITokenStorage _tokens;
    private readonly ILogger<NotificationHubClient> _log;
    private readonly string _hubUrl;
    private HubConnection? _conn;

    public event Action<NotificationDto>? NotificationReceived;

    public NotificationHubClient(ITokenStorage tokens, string apiBaseUrl, ILogger<NotificationHubClient> log)
    {
        _tokens = tokens;
        _log = log;
        _hubUrl = apiBaseUrl.TrimEnd('/') + "/hubs/notifications";
    }

    /// <summary>Idempotent: safe to call from OnInitializedAsync without a
    /// guard. Returns immediately if a connection is already up or starting.</summary>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_conn is not null && _conn.State is HubConnectionState.Connected or HubConnectionState.Connecting)
            return;

        _conn = new HubConnectionBuilder()
            .WithUrl(_hubUrl, opts =>
            {
                // Bearer token via querystring — the JwtBearer event handler
                // in the Api's Program.cs reads ?access_token=... for /hubs/*.
                opts.AccessTokenProvider = async () =>
                {
                    var t = await _tokens.GetAsync();
                    return t?.AccessToken ?? "";
                };
            })
            .WithAutomaticReconnect()
            .Build();

        _conn.On<NotificationDto>("notification", dto =>
        {
            NotificationReceived?.Invoke(dto);
        });

        try
        {
            await _conn.StartAsync(ct);
            _log.LogInformation("Notification hub connected");
        }
        catch (Exception ex)
        {
            // Connection failures are non-fatal — the bell falls back to the
            // 60s poll path automatically. Log and move on.
            _log.LogWarning(ex, "Notification hub connection failed; falling back to polling.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_conn is not null) await _conn.DisposeAsync();
    }
}
