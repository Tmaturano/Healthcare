using Healthcare.Application.Interfaces;
using Healthcare.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Healthcare.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application services here
        services.AddScoped<IUsageEventService, UsageEventService>();
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddHostedService<BackgroundJobProcessor>();
        return services;
    }
}
