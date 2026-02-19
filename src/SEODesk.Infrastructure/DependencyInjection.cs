using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SEODesk.Infrastructure.Options;
using SEODesk.Infrastructure.Services;
using SEODesk.Infrastructure.Services.Interfaces;

namespace SEODesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
    IConfiguration configuration)
    {
        services.Configure<GoogleSearchConsoleOptions>(
            configuration.GetSection(GoogleSearchConsoleOptions.SectionName));

        services.AddMemoryCache();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IGoogleSearchConsoleService, GoogleSearchConsoleService>();

        return services;
    }
}