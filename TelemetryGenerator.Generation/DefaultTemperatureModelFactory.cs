using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.Services;

namespace TelemetryGenerator.Generation;

public sealed class DefaultTemperatureModelFactory : ITemperatureModelFactory
{
    public TemperatureModel Create()
    {
        return new TemperatureModel();
    }
}
