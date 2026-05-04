namespace Giwu.HRMS.Hybrid.Services.Api;

public sealed record ApiResult<T>(bool Success, T? Value, string? ErrorMessage)
{
    public static ApiResult<T> Ok(T value) => new(true, value, null);
    public static ApiResult<T> Fail(string error) => new(false, default, error);
}

public sealed record ApiResult(bool Success, string? ErrorMessage)
{
    public static ApiResult Ok() => new(true, null);
    public static ApiResult Fail(string error) => new(false, error);
}
