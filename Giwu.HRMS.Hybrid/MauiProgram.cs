using Giwu.HRMS.Hybrid.Components.Pages.Settings;
using Giwu.HRMS.Hybrid.Services;
using Giwu.HRMS.Hybrid.Services.Attendance;
using Giwu.HRMS.Hybrid.Services.Auth;
using Giwu.HRMS.Hybrid.Services.Benefits;
using Giwu.HRMS.Hybrid.Services.Departments;
using Giwu.HRMS.Hybrid.Services.Employees;
using Giwu.HRMS.Hybrid.Services.Leaves;
using Giwu.HRMS.Hybrid.Services.Payroll;
using Giwu.HRMS.Hybrid.Services.Recruitment;
using Giwu.HRMS.Hybrid.Services.Reports;
using Giwu.HRMS.Hybrid.Services.Settings;
using Giwu.HRMS.Hybrid.Services.Tenancy;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace Giwu.HRMS.Hybrid
{
    public static class MauiProgram
    {
        // Override at runtime: env var GIWU_API_BASE_URL > #if DEBUG default
#if DEBUG
        private const string DefaultApiBaseUrl = "http://localhost:5080";
#else
        private const string DefaultApiBaseUrl = "https://api.giwu-hrms.com";
#endif

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddMudServices();

            // ── Domain services ────────────────────────────────────────────
            builder.Services.AddSingleton<RoleAuthService>();
            builder.Services.AddSingleton<IRoleAuthService>(sp => sp.GetRequiredService<RoleAuthService>());

            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeService>());

            builder.Services.AddSingleton<SettingsService>();

            // ── Auth + HTTP plumbing ───────────────────────────────────────
            builder.Services.AddSingleton<ITokenStorage, SecureTokenStorage>();
            builder.Services.AddTransient<AuthBearerHandler>();

            var apiBaseUrl = Environment.GetEnvironmentVariable("GIWU_API_BASE_URL") ?? DefaultApiBaseUrl;

            void ConfigureApiClient(HttpClient c)
            {
                c.BaseAddress = new Uri(apiBaseUrl);
                c.Timeout = TimeSpan.FromSeconds(30);
            }

            builder.Services
                .AddHttpClient<IAuthApi, AuthApiClient>(c =>
                {
                    c.BaseAddress = new Uri(apiBaseUrl);
                    c.Timeout = TimeSpan.FromSeconds(15);
                })
                .AddHttpMessageHandler<AuthBearerHandler>();

            builder.Services.AddHttpClient<IEmployeesApi,   EmployeesApiClient>(ConfigureApiClient).AddHttpMessageHandler<AuthBearerHandler>();
            builder.Services.AddHttpClient<IDepartmentsApi, DepartmentsApiClient>(ConfigureApiClient).AddHttpMessageHandler<AuthBearerHandler>();
            builder.Services.AddHttpClient<IAttendanceApi,  AttendanceApiClient>(ConfigureApiClient).AddHttpMessageHandler<AuthBearerHandler>();
            builder.Services.AddHttpClient<ILeavesApi,      LeavesApiClient>(ConfigureApiClient).AddHttpMessageHandler<AuthBearerHandler>();
            builder.Services.AddHttpClient<ITenancyApi,     TenancyApiClient>(ConfigureApiClient).AddHttpMessageHandler<AuthBearerHandler>();
            builder.Services.AddHttpClient<IRecruitmentApi, RecruitmentApiClient>(ConfigureApiClient).AddHttpMessageHandler<AuthBearerHandler>();
            builder.Services.AddHttpClient<IPayrollApi,     PayrollApiClient>(ConfigureApiClient).AddHttpMessageHandler<AuthBearerHandler>();
            builder.Services.AddHttpClient<IBenefitsApi,    BenefitsApiClient>(ConfigureApiClient).AddHttpMessageHandler<AuthBearerHandler>();
            builder.Services.AddHttpClient<IReportsApi,     ReportsApiClient>(ConfigureApiClient).AddHttpMessageHandler<AuthBearerHandler>();
            builder.Services.AddHttpClient<ISettingsApi,    SettingsApiClient>(ConfigureApiClient).AddHttpMessageHandler<AuthBearerHandler>();

            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<IAuthService>(sp => sp.GetRequiredService<AuthService>());

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
