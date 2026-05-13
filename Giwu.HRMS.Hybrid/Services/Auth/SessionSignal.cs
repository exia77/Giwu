namespace Giwu.HRMS.Hybrid.Services.Auth;

public sealed class SessionSignal
{
    public event Action? SessionExpired;

    public void RaiseSessionExpired() => SessionExpired?.Invoke();
}
