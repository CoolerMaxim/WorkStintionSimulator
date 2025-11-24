using Microsoft.Extensions.DependencyInjection;

namespace TelemetryGenerator.Cli;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelemetryGenerationRunner(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        TelemetryGenerationRunner.Register(services);
        return services;
    }
}
