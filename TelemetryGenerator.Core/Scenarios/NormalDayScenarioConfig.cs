using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.Scenarios;

public sealed record NormalDayScenarioConfig(
    IReadOnlyList<NormalDayPhaseConfig> Phases,
    IReadOnlyList<NormalDayInsertionConfig> Insertions,
    bool RepeatUntilDuration);

public sealed record NormalDayPhaseConfig(
    TimeSpan Duration,
    double CoolingEfficiency,
    bool SoundStatus,
    bool ResetState = false);

public sealed record NormalDayInsertionConfig(int AfterPhaseIndex, NormalDayPhaseConfig Phase);

public static class NormalDayScenarioProfiles
{
    public static NormalDayScenarioConfig Build(Difficulty difficulty)
    {
        return new NormalDayScenarioConfig(
            SelectPhases(difficulty),
            Array.Empty<NormalDayInsertionConfig>(),
            RepeatUntilDuration: true);
    }

    private static IReadOnlyList<NormalDayPhaseConfig> SelectPhases(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Hard => new List<NormalDayPhaseConfig>
            {
                new(TimeSpan.FromHours(6), 0.88, false, ResetState: true),
                new(TimeSpan.FromHours(10), 0.8, true),
                new(TimeSpan.FromHours(8), 0.92, false)
            },
            Difficulty.Easy => new List<NormalDayPhaseConfig>
            {
                new(TimeSpan.FromHours(6), 0.92, false, ResetState: true),
                new(TimeSpan.FromHours(10), 0.88, true),
                new(TimeSpan.FromHours(8), 0.97, false)
            },
            _ => new List<NormalDayPhaseConfig>
            {
                new(TimeSpan.FromHours(6), 0.9, false, ResetState: true),
                new(TimeSpan.FromHours(10), 0.85, true),
                new(TimeSpan.FromHours(8), 0.95, false)
            }
        };
    }
}
