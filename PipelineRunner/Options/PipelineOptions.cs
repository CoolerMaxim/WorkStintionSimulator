using System.Collections.Generic;
using System.Linq;

namespace PipelineRunner.Options;

public sealed class PipelineOptions
{
    public required string Scenario { get; set; }

    public required string Duration { get; set; }

    public int StepMinutes { get; set; }

    public required string WorkstationId { get; set; }

    public required string Difficulty { get; set; }

    public required string Start { get; set; }

    public int SpeakersConfigured { get; set; }

    public required string NodeProfile { get; set; }

    public required string OutputPath { get; set; }

    public int? Seed { get; set; }

    public required string SolutionPath { get; set; }

    public required string TelemetryProjectPath { get; set; }

    public required string WorkingDirectory { get; set; }

    public bool RunAll { get; set; }

    public bool Build { get; set; }

    public bool Telemetry { get; set; }

    public bool Check { get; set; }

    public bool Train { get; set; }

    public required List<string> Steps { get; set; }

    public bool ShouldRunBuild => ShouldRunStep(Build, "build");

    public bool ShouldRunTelemetry => ShouldRunStep(Telemetry, "telemetry");

    public bool ShouldRunCheck => ShouldRunStep(Check, "check");

    public bool ShouldRunTrain => ShouldRunStep(Train, "train");

    private bool ShouldRunStep(bool explicitFlag, string stepName)
    {
        return RunAll
            || explicitFlag
            || (Steps ?? Enumerable.Empty<string>())
                .Any(step => string.Equals(step, stepName, StringComparison.OrdinalIgnoreCase));
    }
}
