using TelemetryGenerator.Core.Configuration;

namespace TelemetryGenerator.Cli;

public sealed class TelemetryGenerationRequest
{
    public string Scenario { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    public string Start { get; set; } = string.Empty;

    public string Duration { get; set; } = string.Empty;

    public int StepMinutes { get; set; }

    public string WorkstationId { get; set; } = string.Empty;

    public int SpeakersConfigured { get; set; }

    public string NodeProfile { get; set; } = string.Empty;

    public string OutputPath { get; set; } = string.Empty;

    public int? Seed { get; set; }
}
