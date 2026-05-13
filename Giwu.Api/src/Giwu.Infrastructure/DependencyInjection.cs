using Giwu.Application.Common;
using Giwu.Infrastructure.Auth;
using Giwu.Infrastructure.Email;
using Giwu.Infrastructure.Persistence;
using Giwu.Infrastructure.Persistence.Interceptors;
using Giwu.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Giwu.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<AuditInterceptor>();
        services.AddScoped<DomainEventToOutboxInterceptor>();
        services.AddScoped<NotificationBroadcastInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(config.GetConnectionString("Db"), npg =>
                npg.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

            opts.AddInterceptors(
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<DomainEventToOutboxInterceptor>(),
                sp.GetRequiredService<NotificationBroadcastInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<Giwu.Application.Notifications.INotificationDispatcher,
                           Giwu.Infrastructure.Notifications.NotificationDispatcher>();

        services.AddScoped<ITenantContext, TenantContext>();

        services.Configure<JwtOptions>(config.GetSection("Auth"));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        services.Configure<SmtpOptions>(config.GetSection("Smtp"));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        services.Configure<GoogleOptions>(config.GetSection("Google"));
        services.AddSingleton<IGoogleTokenVerifier, GoogleTokenVerifier>();

        services.Configure<AppUrlOptions>(config.GetSection("AppUrl"));

        return services;
    }
}
