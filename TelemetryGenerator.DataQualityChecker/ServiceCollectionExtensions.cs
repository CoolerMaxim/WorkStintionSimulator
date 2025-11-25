using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelemetryGenerator.DataQualityChecker.Analyzers;
using TelemetryGenerator.DataQualityChecker.Configuration;
using TelemetryGenerator.DataQualityChecker.Reporting;
using TelemetryGenerator.DataQualityChecker.Services;

namespace TelemetryGenerator.DataQualityChecker;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataQualityChecker(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<DataQualityCheckerOptions>()
            .Bind(configuration.GetSection("ValidationSettings"))
            .ValidateOnStart();

        services.AddSingleton<CsvLoader>();
        services.AddSingleton<StructureAnalyzer>();
        services.AddSingleton<TimeGridAnalyzer>();
        services.AddSingleton<PhysicsAnalyzer>();
        services.AddSingleton<AnomalyAnalyzer>();
        services.AddSingleton<ScenarioAnalyzer>();
        services.AddSingleton<MLFitnessAnalyzer>();
        services.AddSingleton<ReportBuilder>();
        services.AddScoped<DataQualityChecker>();

        return services;
    }
}
