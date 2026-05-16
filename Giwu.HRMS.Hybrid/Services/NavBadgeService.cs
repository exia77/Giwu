namespace Giwu.HRMS.Hybrid.Services;

/// <summary>
/// Lightweight pub/sub so that pages performing mutations that affect
/// sidebar attention badges (pending-leave count, today's late/absent
/// attendance, etc.) can ask the layout to recompute them.
///
/// Usage:
///   • DashboardLayout subscribes in OnInitialized, unsubscribes in Dispose.
///   • Mutating pages inject this and call <see cref="RequestRefresh"/>
///     after a successful Approve / Reject / Cancel / File.
///
/// Registered as a singleton so every page + the layout share the same
/// event bus.
/// </summary>
public sealed class NavBadgeService
{
    /// <summary>Fired when a page requests the layout recompute its badges.
    /// Subscribers should kick off a refresh and call StateHasChanged. The
    /// invocation happens on whatever thread the publisher was on, so
    /// subscribers must marshal back to the UI thread if needed.</summary>
    public event Func<Task>? RefreshRequested;

    /// <summary>Ask the layout to refresh its sidebar badges. Safe to call
    /// without subscribers — the event is null-checked.</summary>
    public Task RequestRefreshAsync()
    {
        var handler = RefreshRequested;
        if (handler is null) return Task.CompletedTask;

        // Fan out to all subscribers; ignore exceptions from any single
        // subscriber so one broken page doesn't prevent the others from
        // refreshing.
        var tasks = handler.GetInvocationList()
            .OfType<Func<Task>>()
            .Select(async h =>
            {
                try { await h(); }
                catch { /* subscriber bug shouldn't break the publisher */ }
            });
        return Task.WhenAll(tasks);
    }
}
