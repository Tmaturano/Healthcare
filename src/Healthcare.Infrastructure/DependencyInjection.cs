using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Healthcare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<HealthcareDbContext>(options =>
        {
            options.UseInMemoryDatabase("InMemoryDb");
        });

        return services;
    }
}
