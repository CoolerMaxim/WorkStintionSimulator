using System.Linq;
using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Services;

public sealed class MaintenanceScheduler
{
    private sealed record ScheduledMaintenance(MaintenanceType Type, DateTime ExecuteAt);

    private readonly List<ScheduledMaintenance> _scheduled = new();

    public void ScheduleMaintenance(MaintenanceType type, DateTime when)
    {
        _scheduled.Add(new ScheduledMaintenance(type, when));
    }

    public MaintenanceType Update(NodeState state, NodeConfig config, DifficultyProfile profile, Random rnd)
    {
        while (state.PlannedMaintenance.Count > 0)
        {
            var (type, when) = state.PlannedMaintenance.Dequeue();
            ScheduleMaintenance(type, when);
        }

        MaintenanceType maintenanceType = MaintenanceType.None;

        var due = _scheduled
            .Where(m => state.Timestamp >= m.ExecuteAt)
            .OrderBy(m => m.ExecuteAt)
            .FirstOrDefault();

        if (due is not null)
        {
            maintenanceType = due.Type;
            _scheduled.Remove(due);
        }
        else if (rnd.NextDouble() < profile.MaintenanceFrequency * 0.005)
        {
            maintenanceType = ChooseRandomMaintenance(rnd);
        }

        if (maintenanceType != MaintenanceType.None)
        {
            ApplyMaintenance(state, config, maintenanceType);
        }
        else
        {
            state.MaintenanceType = MaintenanceType.None;
        }

        return maintenanceType;
    }

    private static MaintenanceType ChooseRandomMaintenance(Random rnd)
    {
        var options = new[]
        {
            MaintenanceType.BatteryReplacement,
            MaintenanceType.SpeakersRepair,
            MaintenanceType.CoolingService
        };
        return options[rnd.Next(options.Length)];
    }

    private static void ApplyMaintenance(NodeState state, NodeConfig config, MaintenanceType maintenanceType)
    {
        switch (maintenanceType)
        {
            case MaintenanceType.BatteryReplacement:
                state.BatteryCapacityAhEff = config.BatteryCapacityAh;
                state.BatteryChargeAh = state.BatteryCapacityAhEff * 0.95;
                state.BatteryVoltage = config.BatteryFullVoltage;
                state.BatteryStatusOk = true;
                break;
            case MaintenanceType.SpeakersRepair:
                state.SpeakersEffective = config.SpeakersConfigured;
                state.SoundStatus = false;
                break;
            case MaintenanceType.CoolingService:
                state.CoolingEfficiency = 1.0;
                state.InsideTemperature = Math.Min(state.InsideTemperature, state.OutsideTemperature + 5);
                break;
        }

        state.MaintenanceType = maintenanceType;
        state.IsAnomaly = maintenanceType != MaintenanceType.None && state.IsAnomaly && state.AnomalyType != AnomalyType.None;
    }
}
