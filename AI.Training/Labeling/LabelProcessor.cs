using AI.Training.Models;

namespace AI.Training.Labeling;

public class LabelProcessor
{
    public IReadOnlyList<ModelInput> ToModelInputs(IEnumerable<FeatureVector> features)
    {
        return features
            .Select(f => new ModelInput
            {
                Features = new[]
                {
                    f.BatteryVoltage,
                    f.CpuTemperature,
                    f.InsideTemperature,
                    f.AmplifierOutPower,
                    f.NetworkLatencyMs,
                    f.SpeakersConfigured,
                    f.NodeUptimeMinutes,
                    f.TotalRuntimeHours,
                    f.RestartRate
                },
                Label = (uint)f.Label
            })
            .ToList();
    }
}
