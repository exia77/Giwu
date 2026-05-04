using Giwu.Application.Common;

namespace Giwu.Api.Middleware;

/// <summary>
/// Converts an Application <see cref="Result{T}"/> / <see cref="Result"/>
/// into the appropriate HTTP response. Endpoints stay free of HTTP plumbing.
/// </summary>
public static class ResultMapper
{
    public static IResult ToHttp<T>(this Result<T> r) => r.Kind switch
    {
        ResultKind.Success    => Results.Ok(r.Value),
        ResultKind.NotFound   => Results.NotFound(new { message = r.Message ?? "Not found" }),
        ResultKind.Forbidden  => Results.Forbid(),
        ResultKind.Conflict   => Results.Conflict(new { message = r.Message }),
        ResultKind.Invalid    => Results.ValidationProblem(r.Errors!),
        _                     => Results.StatusCode(500),
    };

    public static IResult ToHttp(this Result r) => r.Kind switch
    {
        ResultKind.Success    => Results.NoContent(),
        ResultKind.NotFound   => Results.NotFound(new { message = r.Message ?? "Not found" }),
        ResultKind.Forbidden  => Results.Forbid(),
        ResultKind.Conflict   => Results.Conflict(new { message = r.Message }),
        ResultKind.Invalid    => Results.ValidationProblem(r.Errors!),
        _                     => Results.StatusCode(500),
    };
}
