using System.Collections.Generic;
using System.Linq;

namespace PipelineRunner.Options;

public sealed class PipelineOptions
{
    private static readonly string ApplicationRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    public string Scenario { get; set; } = string.Empty;

    public string Duration { get; set; } = string.Empty;

    public int StepMinutes { get; set; }

    public string WorkstationId { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    public string Start { get; set; } = string.Empty;

    public int SpeakersConfigured { get; set; }

    public string NodeProfile { get; set; } = string.Empty;

    public string OutputPath { get; set; } = string.Empty;

    public int? Seed { get; set; }

    public string? DatasetName { get; set; }

    public string QualityMarkdownPath { get; set; } = Path.Combine(ApplicationRoot, "out", "DataQualityReport.md");

    public string QualityJsonPath { get; set; } = Path.Combine(ApplicationRoot, "out", "DataQualityReport.json");

    public string TrainingOutput { get; set; } = Path.Combine(ApplicationRoot, "out", "training-output");

    public string SolutionPath { get; set; } = Path.Combine(ApplicationRoot, "TelemetryGenerator.Build.slnf");

    public string TelemetryProjectPath { get; set; } = Path.Combine(ApplicationRoot, "TelemetryGenerator.Cli", "TelemetryGenerator.Cli.csproj");

    public string WorkingDirectory { get; set; } = ApplicationRoot;

    public bool RunAll { get; set; } = false;

    public bool Build { get; set; }

    public bool Telemetry { get; set; }

    public bool Check { get; set; }

    public bool Train { get; set; }

    public List<string> Steps { get; set; } = new();

    public bool ShouldRunBuild => ShouldRunStep(Build, "build");

    public bool ShouldRunTelemetry => ShouldRunStep(Telemetry, "telemetry");

    public bool ShouldRunCheck => ShouldRunStep(Check, "check");

    public bool ShouldRunTrain => ShouldRunStep(Train, "train");

    private bool ShouldRunStep(bool explicitFlag, string stepName)
    {
        return RunAll
            || explicitFlag
            || Steps.Any(step => string.Equals(step, stepName, StringComparison.OrdinalIgnoreCase));
    }
}
