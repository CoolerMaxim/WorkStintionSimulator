using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.Models;

public sealed record DifficultyProfile(
    double AnomalyRate,
    int MaxParallelAnomalies,
    double SensorNoiseLevel,
    double DataDirtyLevel,
    double MaintenanceFrequency);

public static class DifficultyProfiles
{
    public static DifficultyProfile Create(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => new DifficultyProfile(
                AnomalyRate: 0.01,
                MaxParallelAnomalies: 1,
                SensorNoiseLevel: 0.2,
                DataDirtyLevel: 0.1,
                MaintenanceFrequency: 0.1
            ),
            Difficulty.Normal => new DifficultyProfile(
                AnomalyRate: 0.05,
                MaxParallelAnomalies: 2,
                SensorNoiseLevel: 0.5,
                DataDirtyLevel: 0.3,
                MaintenanceFrequency: 0.3
            ),
            Difficulty.Hard => new DifficultyProfile(
                AnomalyRate: 0.15,
                MaxParallelAnomalies: 3,
                SensorNoiseLevel: 1.0,
                DataDirtyLevel: 0.7,
                MaintenanceFrequency: 0.6
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
        };
    }
}
