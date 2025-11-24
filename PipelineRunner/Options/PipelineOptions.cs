using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TelemetryGenerator.Core.Enums;

namespace PipelineRunner.Options;

public sealed class PipelineOptions
{
    public string Scenario { get; set; } = "normal-day";

    public string Duration { get; set; } = "24h";

    public int StepMinutes { get; set; } = 5;

    public string WorkstationId { get; set; } = "WS-001";

    public string Difficulty { get; set; } = global::TelemetryGenerator.Core.Enums.Difficulty.Normal.ToString();

    public string Start { get; set; } = DateTime.Now.ToString("O", CultureInfo.InvariantCulture);

    public int SpeakersConfigured { get; set; } = 4;

    public string NodeProfile { get; set; } = "randomized";

    public string OutputPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "out", "telemetry.csv");

    public int? Seed { get; set; }

    public string? DatasetName { get; set; }

    public string QualityMarkdownPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "out", "DataQualityReport.md");

    public string QualityJsonPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "out", "DataQualityReport.json");

    public string TrainingOutput { get; set; } = Path.Combine(Environment.CurrentDirectory, "out", "training-output");

    public string SolutionPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "TelemetryGenerator.sln");

    public string TelemetryProjectPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "TelemetryGenerator.Cli", "TelemetryGenerator.Cli.csproj");

    public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();

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
