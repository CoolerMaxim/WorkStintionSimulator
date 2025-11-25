using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.Services;

namespace TelemetryGenerator.Generation;

public sealed class DefaultTemperatureModelFactory : ITemperatureModelFactory
{
    public ITemperatureModel Create()
    {
        return new TemperatureModel();
    }
}
