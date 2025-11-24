using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Services;

namespace TelemetryGenerator.Generation;

public sealed class DefaultAnomalyInjectorFactory : IAnomalyInjectorFactory
{
    public AnomalyInjector Create()
    {
        var anomalyConfiguration = AnomalyConfigurationFactory.CreateDefault();
        return new AnomalyInjector(anomalyConfiguration);
    }
}
