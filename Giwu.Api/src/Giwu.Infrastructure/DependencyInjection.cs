using Giwu.Application.Common;
using Giwu.Infrastructure.Auth;
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

        services.AddDbContext<ApplicationDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(config.GetConnectionString("Db"), npg =>
                npg.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

            opts.AddInterceptors(
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<DomainEventToOutboxInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ITenantContext, TenantContext>();

        services.Configure<JwtOptions>(config.GetSection("Auth"));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        return services;
    }
}
