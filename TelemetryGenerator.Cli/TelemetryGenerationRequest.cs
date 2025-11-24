using TelemetryGenerator.Core.Configuration;

namespace TelemetryGenerator.Cli;

public sealed class TelemetryGenerationRequest
{
    public string Scenario { get; set; } = TelemetryDefaults.Scenario;

    public string Difficulty { get; set; } = TelemetryDefaults.DifficultyName;

    public string Start { get; set; } = TelemetryDefaults.StartIsoString;

    public string Duration { get; set; } = TelemetryDefaults.DurationText;

    public int StepMinutes { get; set; } = TelemetryDefaults.StepMinutes;

    public string WorkstationId { get; set; } = TelemetryDefaults.WorkstationId;

    public int SpeakersConfigured { get; set; } = TelemetryDefaults.SpeakersConfigured;

    public string NodeProfile { get; set; } = TelemetryDefaults.NodeProfile;

    public string OutputPath { get; set; } = TelemetryDefaults.BuildOutputPath();

    public int? Seed { get; set; }
}
