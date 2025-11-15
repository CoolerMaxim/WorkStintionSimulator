using System;
using System.Collections.Generic;

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
    private static readonly Random Random = new();
    private const double AirAlarmOverlapChance = 0.15;

    public TurningOffTheLights()
    {
        EventName = "Відключення світла";
        Duration = RollDuration();

        if (Random.NextDouble() < AirAlarmOverlapChance)
        {
            SubEvents.Add(new AirAlarm());
        }
    }

    private static TimeSpan RollDuration()
    {
        int hours = Random.Next(3, 8);
        return TimeSpan.FromHours(hours);
    }
}
