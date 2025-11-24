using System.Collections.Generic;
using System.Linq;
using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;

namespace PipelineRunner.Options;

public sealed class PipelineOptions
{
    private static readonly string ApplicationRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    public string Scenario { get; set; } = TelemetryDefaults.Scenario;

    public string Duration { get; set; } = TelemetryDefaults.DurationText;

    public int StepMinutes { get; set; } = TelemetryDefaults.StepMinutes;

    public string WorkstationId { get; set; } = TelemetryDefaults.WorkstationId;

    public string Difficulty { get; set; } = TelemetryDefaults.DifficultyName;

    public string Start { get; set; } = TelemetryDefaults.StartIsoString;

    public int SpeakersConfigured { get; set; } = TelemetryDefaults.SpeakersConfigured;

    public string NodeProfile { get; set; } = TelemetryDefaults.NodeProfile;

    public string OutputPath { get; set; } = TelemetryDefaults.BuildOutputPath(Path.Combine(ApplicationRoot, "out"));

    public int? Seed { get; set; }

    public string? DatasetName { get; set; }

    public string QualityMarkdownPath { get; set; } = Path.Combine(ApplicationRoot, "out", "DataQualityReport.md");

    public string QualityJsonPath { get; set; } = Path.Combine(ApplicationRoot, "out", "DataQualityReport.json");

    public string TrainingOutput { get; set; } = Path.Combine(ApplicationRoot, "out", "training-output");

    public string SolutionPath { get; set; } = Path.Combine(ApplicationRoot, "TelemetryGenerator.sln");

    public string BuildOutputDirectory { get; set; } = Path.Combine(ApplicationRoot, "out", "build");

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
