using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Giwu.HRMS.Hybrid.Services.Api;

internal static class ApiClientCore
{
    internal static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public static async Task<ApiResult<T>> GetAsync<T>(HttpClient http, string path, CancellationToken ct)
    {
        try
        {
            var res = await http.GetAsync(path, ct);
            if (!res.IsSuccessStatusCode)
                return ApiResult<T>.Fail(await ReadErrorAsync(res, ct));

            var value = await res.Content.ReadFromJsonAsync<T>(JsonOpts, ct);
            return value is null
                ? ApiResult<T>.Fail("Empty response body")
                : ApiResult<T>.Ok(value);
        }
        catch (HttpRequestException) { return ApiResult<T>.Fail("Cannot reach the server. Check your connection."); }
        catch (Exception ex)         { return ApiResult<T>.Fail(ex.Message); }
    }

    public static async Task<ApiResult<TResp>> PostAsync<TReq, TResp>(
        HttpClient http, string path, TReq body, CancellationToken ct)
    {
        try
        {
            var res = await http.PostAsJsonAsync(path, body, JsonOpts, ct);
            if (!res.IsSuccessStatusCode)
                return ApiResult<TResp>.Fail(await ReadErrorAsync(res, ct));

            var value = await res.Content.ReadFromJsonAsync<TResp>(JsonOpts, ct);
            return value is null
                ? ApiResult<TResp>.Fail("Empty response body")
                : ApiResult<TResp>.Ok(value);
        }
        catch (HttpRequestException) { return ApiResult<TResp>.Fail("Cannot reach the server. Check your connection."); }
        catch (Exception ex)         { return ApiResult<TResp>.Fail(ex.Message); }
    }

    public static async Task<ApiResult> PostAsync<TReq>(
        HttpClient http, string path, TReq body, CancellationToken ct)
    {
        try
        {
            var res = await http.PostAsJsonAsync(path, body, JsonOpts, ct);
            return res.IsSuccessStatusCode
                ? ApiResult.Ok()
                : ApiResult.Fail(await ReadErrorAsync(res, ct));
        }
        catch (HttpRequestException) { return ApiResult.Fail("Cannot reach the server. Check your connection."); }
        catch (Exception ex)         { return ApiResult.Fail(ex.Message); }
    }

    public static async Task<ApiResult> PostEmptyAsync(HttpClient http, string path, CancellationToken ct)
    {
        try
        {
            var res = await http.PostAsync(path, content: null, ct);
            return res.IsSuccessStatusCode
                ? ApiResult.Ok()
                : ApiResult.Fail(await ReadErrorAsync(res, ct));
        }
        catch (HttpRequestException) { return ApiResult.Fail("Cannot reach the server. Check your connection."); }
        catch (Exception ex)         { return ApiResult.Fail(ex.Message); }
    }

    public static async Task<ApiResult> PutAsync<TReq>(
        HttpClient http, string path, TReq body, CancellationToken ct)
    {
        try
        {
            var res = await http.PutAsJsonAsync(path, body, JsonOpts, ct);
            return res.IsSuccessStatusCode
                ? ApiResult.Ok()
                : ApiResult.Fail(await ReadErrorAsync(res, ct));
        }
        catch (HttpRequestException) { return ApiResult.Fail("Cannot reach the server. Check your connection."); }
        catch (Exception ex)         { return ApiResult.Fail(ex.Message); }
    }

    public static async Task<ApiResult> DeleteAsync(HttpClient http, string path, CancellationToken ct)
    {
        try
        {
            var res = await http.DeleteAsync(path, ct);
            return res.IsSuccessStatusCode
                ? ApiResult.Ok()
                : ApiResult.Fail(await ReadErrorAsync(res, ct));
        }
        catch (HttpRequestException) { return ApiResult.Fail("Cannot reach the server. Check your connection."); }
        catch (Exception ex)         { return ApiResult.Fail(ex.Message); }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage res, CancellationToken ct)
    {
        if (res.StatusCode == HttpStatusCode.BadRequest)
        {
            try
            {
                var problem = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOpts, ct);
                if (problem?.Errors is { Count: > 0 } errs)
                    return string.Join("; ", errs.SelectMany(kv => kv.Value));
            }
            catch { /* fall through */ }
            return "Validation failed";
        }

        return res.StatusCode switch
        {
            HttpStatusCode.NotFound      => "Not found",
            HttpStatusCode.Forbidden     => "You do not have permission to perform this action",
            HttpStatusCode.Unauthorized  => "Authentication required",
            HttpStatusCode.Conflict      => "Conflict with current state",
            _                            => $"HTTP {(int)res.StatusCode}",
        };
    }

    private sealed class ValidationProblemDetails
    {
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
