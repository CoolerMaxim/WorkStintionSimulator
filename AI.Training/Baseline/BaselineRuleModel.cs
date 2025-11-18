using AI.Training.Models;

namespace AI.Training.Baseline;

public class BaselineRuleModel
{
    public HealthState Predict(FeatureVector features)
    {
        if (features.NetworkLatencyMs > 2000)
        {
            return HealthState.Failed;
        }

        if (features.InsideTemperature > 70 || features.BatteryVoltage < 22.5f)
        {
            return HealthState.Critical;
        }

        if (features.AmplifierOutPower < 0.5 * features.SpeakersConfigured || features.RestartRate > 0.2)
        {
            return HealthState.Degraded;
        }

        if (features.NetworkLatencyMs > 1500)
        {
            return HealthState.Degraded;
        }

        return HealthState.Normal;
    }
}
