using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WorkstationJobSimulator.Models;

namespace WorkstationJobSimulator.Events;

public class SimulationEventGenerator
{
    private readonly Random _random = new();

    private readonly List<(double Weight, Type Type)> _eventTypes = new();
    private readonly SimulationParameters _parameters;

    public SimulationEventGenerator(SimulationParameters? parameters = null)
    {
        _parameters = parameters ?? SimulationParameters.Default;

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

    public IReadOnlyCollection<Type> GetRegisteredEventTypes() =>
        _eventTypes.Select(x => x.Type).ToArray();

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
        var minSeconds = Math.Min(_parameters.MinEventIntervalSeconds, _parameters.MaxEventIntervalSeconds);
        var maxSeconds = Math.Max(_parameters.MinEventIntervalSeconds, _parameters.MaxEventIntervalSeconds);

        int ms = _random.Next(minSeconds * 1000, (maxSeconds * 1000) + 1);
        return TimeSpan.FromMilliseconds(ms);
    }
}
