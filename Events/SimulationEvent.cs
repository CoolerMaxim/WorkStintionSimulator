using WorkstationJobSimulator.Utilities;

namespace WorkstationJobSimulator.Events;

public abstract class SimulationEvent
{
    public string EventName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public List<SimulationEvent> SubEvents { get; } = new();
}

[EventChance(0.5)]
public class AirAlarm : SimulationEvent
{
    public AirAlarm()
    {
        EventName = "Повітряна тривога";
        Duration = TimeSpan.FromMinutes(2);
    }
}

[EventChance(0.35)]
public class TurningOffTheLights : SimulationEvent
{
    private const double AirAlarmOverlapChance = 0.15;

    public TurningOffTheLights()
    {
        EventName = "Відключення світла";
        Duration = RollDuration();

        if (SimulationRandom.Instance.NextDouble() < AirAlarmOverlapChance)
        {
            SubEvents.Add(new AirAlarm());
        }
    }

    private static TimeSpan RollDuration()
    {
        int hours = SimulationRandom.Instance.Next(3, 8);
        return TimeSpan.FromHours(hours);
    }
}
