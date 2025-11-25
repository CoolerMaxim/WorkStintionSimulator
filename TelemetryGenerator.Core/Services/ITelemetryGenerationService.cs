using TelemetryGenerator.Core.Configuration;

namespace TelemetryGenerator.Core.Services;

public interface ITelemetryGenerationService
{
    Task GenerateAsync(GenerationConfig config, CancellationToken cancellationToken = default);
}
