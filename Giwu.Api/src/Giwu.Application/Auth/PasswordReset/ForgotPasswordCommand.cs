using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Giwu.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Giwu.Application.Auth.PasswordReset;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

internal sealed class ForgotPasswordHandler(
    IApplicationDbContext db,
    IEmailSender email,
    ITenantContext tenant,
    TimeProvider clock,
    IOptions<AppUrlOptions> appUrl,
    ILogger<ForgotPasswordHandler> log)
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        // Always return success to prevent account enumeration.
        // If the email matches a real, active user we send the link;
        // otherwise we silently do nothing.
        tenant.Bypass();

        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == cmd.Email && u.DeletedAt == null, ct);

        if (user is null || !user.IsActive)
        {
            log.LogInformation("ForgotPassword for unknown/inactive email {Email} ignored.", cmd.Email);
            return Result.Success();
        }

        var token = GenerateUrlSafeToken();
        user.PasswordResetTokenHash = HashToken(token);
        user.PasswordResetExpiresAt = clock.GetUtcNow().AddHours(1);

        tenant.SetTenant(user.TenantId);
        await db.SaveChangesAsync(ct);

        var resetUrl = BuildResetUrl(appUrl.Value.BaseUrl, user.Email, token);
        var body = $"""
            <p>Hello {System.Web.HttpUtility.HtmlEncode(user.DisplayName)},</p>
            <p>We received a request to reset your Giwu HRIS password.</p>
            {(string.IsNullOrEmpty(resetUrl)
                ? $"<p>Use this code in the app within the next hour:</p><p><b>{token}</b></p>"
                : $"<p><a href=\"{resetUrl}\">Reset your password</a> (link expires in 1 hour)</p><p>Or paste this token into the app: <b>{token}</b></p>")}
            <p>If you didn't request a reset, you can ignore this email.</p>
            """;

        await email.SendAsync(user.Email, user.DisplayName, "Reset your Giwu HRIS password", body, ct);
        return Result.Success();
    }

    private static string BuildResetUrl(string baseUrl, string email, string token)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return string.Empty;
        var qEmail = Uri.EscapeDataString(email);
        var qToken = Uri.EscapeDataString(token);
        return $"{baseUrl.TrimEnd('/')}/reset-password?email={qEmail}&token={qToken}";
    }

    private static string GenerateUrlSafeToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
