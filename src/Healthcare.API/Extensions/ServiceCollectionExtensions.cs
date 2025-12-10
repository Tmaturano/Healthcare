using FluentValidation;
using Healthcare.API.Validators;
using Healthcare.Application.DTOs;

namespace Healthcare.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<UsageEventRequest>, UsageEventRequestValidator>();
        services.AddScoped<IValidator<BatchEventsRequest>, BatchEventsRequestValidator>();
        return services;
    }
}
