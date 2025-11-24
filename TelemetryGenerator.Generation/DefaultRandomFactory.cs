using TelemetryGenerator.Core.Services;

namespace TelemetryGenerator.Generation;

public sealed class DefaultRandomFactory : IRandomFactory
{
    public Random Create(int? seed)
    {
        return seed.HasValue ? new Random(seed.Value) : new Random();
    }
}
