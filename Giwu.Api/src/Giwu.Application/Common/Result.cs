namespace Giwu.Application.Common;

public enum ResultKind { Success, NotFound, Forbidden, Conflict, Invalid }

public sealed class Result
{
    public ResultKind Kind { get; }
    public string? Message { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    private Result(ResultKind kind, string? message = null,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        Kind = kind; Message = message; Errors = errors;
    }

    public static Result Success() => new(ResultKind.Success);
    public static Result NotFound(string? msg = null) => new(ResultKind.NotFound, msg);
    public static Result Forbidden(string? msg = null) => new(ResultKind.Forbidden, msg);
    public static Result Conflict(string? msg = null) => new(ResultKind.Conflict, msg);
    public static Result Invalid(string field, string error) =>
        new(ResultKind.Invalid, "Validation failed",
            new Dictionary<string, string[]> { [field] = new[] { error } });
}

public sealed class Result<T>
{
    public ResultKind Kind { get; }
    public T? Value { get; }
    public string? Message { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    private Result(ResultKind kind, T? value = default, string? message = null,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        Kind = kind; Value = value; Message = message; Errors = errors;
    }

    public static Result<T> Success(T value) => new(ResultKind.Success, value);
    public static Result<T> NotFound(string? msg = null) => new(ResultKind.NotFound, default, msg);
    public static Result<T> Forbidden(string? msg = null) => new(ResultKind.Forbidden, default, msg);
    public static Result<T> Conflict(string? msg = null) => new(ResultKind.Conflict, default, msg);
    public static Result<T> Invalid(string field, string error) =>
        new(ResultKind.Invalid, default, "Validation failed",
            new Dictionary<string, string[]> { [field] = new[] { error } });
}
