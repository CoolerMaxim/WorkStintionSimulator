using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PipelineRunner.Options;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Services;

namespace PipelineRunner.Steps;

internal sealed class TelemetryStep
{
    private readonly ILogger<TelemetryStep> _logger;
    private readonly PipelineOptions _options;
    private readonly ITelemetryGenerationService _telemetryGenerationService;

    public TelemetryStep(
        ITelemetryGenerationService telemetryGenerationService,
        IOptions<PipelineOptions> options,
        ILogger<TelemetryStep> logger)
    {
        _logger = logger;
        _telemetryGenerationService = telemetryGenerationService;
        _options = options.Value;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var difficulty = Enum.Parse<Difficulty>(_options.Difficulty, true);
        var start = DateTime.Parse(_options.Start, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);

        if (!DurationParser.TryParse(_options.Duration, out var parsedDuration))
        {
            throw new InvalidOperationException($"Duration failed validation: {_options.Duration}");
        }

        _logger.LogInformation(
            "Generating telemetry with scenario={Scenario}, duration={Duration}, stepMinutes={Step}, workstation={Workstation}, output={Output}",
            _options.Scenario,
            _options.Duration,
            _options.StepMinutes,
            _options.WorkstationId,
            _options.OutputPath);

        var options = new TelemetryGenerationOptions
        {
            Scenario = _options.Scenario,
            Difficulty = difficulty,
            Start = start,
            Duration = parsedDuration,
            Step = TimeSpan.FromMinutes(_options.StepMinutes),
            WorkstationId = _options.WorkstationId,
            SpeakersConfigured = _options.SpeakersConfigured,
            NodeProfile = _options.NodeProfile,
            OutputPath = _options.OutputPath,
            Seed = _options.Seed
        };

        await _telemetryGenerationService.GenerateAsync(options, cancellationToken);
        return 0;
    }
}
