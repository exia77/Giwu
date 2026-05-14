namespace Giwu.HRMS.Hybrid.Services;

public interface IAuthService
{
    bool IsLoggedIn { get; }
    UserSession? CurrentUser { get; }
    string UserEmail { get; }
    string UserName { get; }

    /// <summary>
    /// The Employee record GUID linked to this user via the JWT "eid" claim, or
    /// null if the user is not mapped to an employee (e.g. the seeded admin).
    /// Pages that file leave / submit OT / view personal payslips key off this.
    /// </summary>
    Guid? EmployeeId { get; }

    event Action? OnChange;

    /// <summary>Calls the API. Returns null on success, a user-readable error message on failure.</summary>
    Task<string?> LoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>Exchanges a Google ID token for a session. Same contract as <see cref="LoginAsync"/>.</summary>
    Task<string?> SignInWithGoogleAsync(string idToken, CancellationToken ct = default);

    /// <summary>Restores a session from stored tokens at app startup. No-op if no tokens or expired.</summary>
    Task TryRestoreAsync(CancellationToken ct = default);

    Task LogoutAsync(CancellationToken ct = default);
}
