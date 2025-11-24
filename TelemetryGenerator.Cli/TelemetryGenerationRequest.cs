using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Cli;

public sealed class TelemetryGenerationRequest
{
    public string Scenario { get; set; } = "normal-day";

    public string Difficulty { get; set; } = TelemetryGenerator.Core.Enums.Difficulty.Normal.ToString();

    public string Start { get; set; } = DateTime.Now.ToString("O", System.Globalization.CultureInfo.InvariantCulture);

    public string Duration { get; set; } = "24h";

    public int StepMinutes { get; set; } = 5;

    public string WorkstationId { get; set; } = "WS-001";

    public int SpeakersConfigured { get; set; } = 4;

    public string NodeProfile { get; set; } = "randomized";

    public string OutputPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "telemetry.csv");

    public int? Seed { get; set; }
}
