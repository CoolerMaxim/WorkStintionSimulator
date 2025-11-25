using Microsoft.Extensions.Logging;
using PipelineRunner.Configuration;
using TelemetryGenerator.Cli;

namespace PipelineRunner.Steps;

internal sealed class TelemetryStep
{
    private readonly ILogger<TelemetryStep> _logger;
    private readonly PipelineConfig _config;
    private readonly TelemetryGenerationRunner _telemetryGenerationRunner;

    public TelemetryStep(
        TelemetryGenerationRunner telemetryGenerationRunner,
        PipelineConfig config,
        ILogger<TelemetryStep> logger)
    {
        _logger = logger;
        _telemetryGenerationRunner = telemetryGenerationRunner;
        _config = config;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating telemetry with scenario={Scenario}, duration={Duration}, stepMinutes={Step}, workstation={Workstation}, output={Output}",
            _config.Generation.Scenario,
            _config.Generation.Duration,
            _config.Generation.Step.TotalMinutes,
            _config.Generation.WorkstationId,
            _config.Generation.OutputPath);

        return await _telemetryGenerationRunner.RunAsync(_config.Generation, cancellationToken).ConfigureAwait(false);
    }
}
