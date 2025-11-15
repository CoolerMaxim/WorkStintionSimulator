using WorkstationJobSimulator.Models.wsModels;

namespace WorkstationJobSimulator.Events;

public abstract class SimulationEvent
{
    public string EventName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public List<SimulationEvent> SubEvents { get; } = new();

    public virtual void Apply(Workstation workstation)
    {
        // За замовчуванням: просто проганяємо підівенти
        foreach (var sub in SubEvents)
        {
            workstation.Log($"  Підівент \"{sub.EventName}\" починає застосовуватись...");
            sub.Apply(workstation);
        }
    }
}

[EventChance(0.5)]
public class AirAlarm : SimulationEvent
{
    public AirAlarm()
    {
        EventName = "Повітряна тривога";
        Duration = TimeSpan.FromMinutes(2);
    }

    public override void Apply(Workstation workstation)
    {
        workstation.SetAirAlarm(true, "Подія: повітряна тривога");
        base.Apply(workstation); // якщо раптом зʼявляться підівенти
    }
}

[EventChance(0.35)]
public class TurningOffTheLights : SimulationEvent
{
    private static readonly Random _random = new();
    private const double AirAlarmOverlapChance = 0.15; // 15% шанс на тривогу під час відключення

    public TurningOffTheLights()
    {
        EventName = "Відключення світла";
        Duration = RollDuration();

        if (_random.NextDouble() < AirAlarmOverlapChance)
        {
            // під час відключення світла додатково трапляється повітряна тривога
            SubEvents.Add(new AirAlarm());
        }
    }

    private static TimeSpan RollDuration()
    {
        int hours = _random.Next(3, 8);
        return TimeSpan.FromHours(hours);
    }

    public override void Apply(Workstation workstation)
    {
        workstation.SetPower(false, "Подія: відключення світла");
        base.Apply(workstation); // тут застосуються підівенти (якщо є AirAlarm)
    }
}

