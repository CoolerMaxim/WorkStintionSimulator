using TelemetryGenerator.Core.Configuration;

namespace PipelineRunner.Configuration;

public sealed class PipelineConfig
{
    public GenerationConfig Generation { get; init; } = new();

    public DatasetConfig Dataset { get; init; } = new();

    public TrainingConfig Training { get; init; } = new();

    public bool ShouldRunBuild { get; init; }

    public bool ShouldRunTelemetry { get; init; }

    public bool ShouldRunCheck { get; init; }

    public bool ShouldRunTrain { get; init; }

    public string WorkingDirectory { get; init; } = string.Empty;

    public string SolutionPath { get; init; } = string.Empty;

    public string TelemetryProjectPath { get; init; } = string.Empty;
}
