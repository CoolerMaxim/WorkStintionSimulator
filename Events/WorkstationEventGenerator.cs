using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WorkstationJobSimulator.Events;

public class SimulationEventGenerator
{
    private readonly Random _random = new();

    private readonly List<(double Weight, Type Type)> _eventTypes = new();

    public SimulationEventGenerator()
    {
        var assembly = Assembly.GetExecutingAssembly();

        _eventTypes.Clear();
        _eventTypes.AddRange(
            assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && typeof(SimulationEvent).IsAssignableFrom(t))
                .Select(t => (Attr: t.GetCustomAttribute<EventChanceAttribute>(), Type: t))
                .Where(x => x.Attr is not null)
                .Select(x => (Weight: x.Attr!.Chance, Type: x.Type))
        );

        if (_eventTypes.Count == 0)
        {
            throw new InvalidOperationException(
                "Не знайдено жодного класу події з EventChanceAttribute.");
        }
    }

    public SimulationEvent Generate()
    {
        Console.WriteLine("[LOG] Генеруємо нову подію...");

        double totalWeight = _eventTypes.Sum(x => x.Weight);
        double roll = _random.NextDouble() * totalWeight;

        double cumulative = 0;
        foreach (var (weight, type) in _eventTypes)
        {
            cumulative += weight;
            if (roll <= cumulative)
            {
                var ev = (SimulationEvent)Activator.CreateInstance(type)!;
                Console.WriteLine($"[LOG] Згенерована подія: {ev.EventName}");
                return ev;
            }
        }

        var lastType = _eventTypes.Last().Type;
        var fallback = (SimulationEvent)Activator.CreateInstance(lastType)!;
        Console.WriteLine($"[WARN] Використовуємо fallback подію: {fallback.EventName}");
        return fallback;
    }

    /// <summary>
    /// Повертає інтервал до наступної події (в середньому ~30 секунд).
    /// </summary>
    public TimeSpan RollNextInterval()
    {
        int ms = _random.Next(20_000, 40_001);
        return TimeSpan.FromMilliseconds(ms);
    }
}
