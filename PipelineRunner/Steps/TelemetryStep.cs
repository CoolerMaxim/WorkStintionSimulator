using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PipelineRunner.Options;
using TelemetryGenerator.Cli;

namespace PipelineRunner.Steps;

internal sealed class TelemetryStep
{
    private readonly ILogger<TelemetryStep> _logger;
    private readonly PipelineOptions _options;
    private readonly TelemetryGenerationRunner _telemetryGenerationRunner;

    public TelemetryStep(
        TelemetryGenerationRunner telemetryGenerationRunner,
        IOptions<PipelineOptions> options,
        ILogger<TelemetryStep> logger)
    {
        _logger = logger;
        _telemetryGenerationRunner = telemetryGenerationRunner;
        _options = options.Value;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating telemetry with scenario={Scenario}, duration={Duration}, stepMinutes={Step}, workstation={Workstation}, output={Output}",
            _options.Scenario,
            _options.Duration,
            _options.StepMinutes,
            _options.WorkstationId,
            _options.OutputPath);

        var request = new TelemetryGenerationRequest
        {
            Scenario = _options.Scenario,
            Difficulty = _options.Difficulty,
            Start = _options.Start,
            Duration = _options.Duration,
            StepMinutes = _options.StepMinutes,
            WorkstationId = _options.WorkstationId,
            SpeakersConfigured = _options.SpeakersConfigured,
            NodeProfile = _options.NodeProfile,
            OutputPath = _options.OutputPath,
            Seed = _options.Seed
        };

        return await _telemetryGenerationRunner.RunAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
