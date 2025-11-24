using System.Globalization;
using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.Configuration;

public static class TelemetryDefaults
{
    public const string Scenario = "normal-day";

    public const string DurationText = "24h";

    public const int StepMinutes = 5;

    public const string WorkstationId = "WS-001";

    public const int SpeakersConfigured = 4;

    public const string NodeProfile = "randomized";

    public const string OutputFileName = "telemetry.csv";

    public const Difficulty DefaultDifficulty = Enums.Difficulty.Normal;

    public static string DifficultyName => DefaultDifficulty.ToString();

    public static TimeSpan Duration => TimeSpan.FromHours(24);

    public static TimeSpan Step => TimeSpan.FromMinutes(StepMinutes);

    public static DateTime Start => DateTime.Now;

    public static string StartIsoString => Start.ToString("O", CultureInfo.InvariantCulture);

    public static string BuildOutputPath(string? baseDirectory = null, string? fileName = null)
    {
        var directory = string.IsNullOrWhiteSpace(baseDirectory) ? Environment.CurrentDirectory : baseDirectory;
        var file = string.IsNullOrWhiteSpace(fileName) ? OutputFileName : fileName;

        return Path.GetFullPath(Path.Combine(directory, file), Environment.CurrentDirectory);
    }
}
