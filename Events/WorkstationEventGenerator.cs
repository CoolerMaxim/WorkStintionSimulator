using System.Reflection;
using WorkstationJobSimulator.Logging;
using WorkstationJobSimulator.Models;
using WorkstationJobSimulator.Utilities;

namespace WorkstationJobSimulator.Events;

public class SimulationEventGenerator
{
    private static readonly IReadOnlyList<(double Weight, Type Type)> CachedEventTypes =
        Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(SimulationEvent).IsAssignableFrom(t))
            .Select(t => (Attr: t.GetCustomAttribute<EventChanceAttribute>(), Type: t))
            .Where(x => x.Attr is not null)
            .Select(x => (Weight: x.Attr!.Chance, Type: x.Type))
            .ToArray();

    private readonly ISimulationLogger _logger;
    private readonly Random _random;
    private readonly List<(double Weight, Type Type)> _eventTypes = new();
    private readonly SimulationParameters _parameters;

    public SimulationEventGenerator(ISimulationLogger logger, SimulationParameters? parameters = null)
    {
        _logger = logger;
        _parameters = parameters ?? SimulationParameters.Default;

        SimulationRandom.Reset(_parameters.RandomSeed);
        _random = SimulationRandom.Instance;

        var configuredWeights = EventConfigurationLoader.LoadWeights(_parameters.EventWeightsConfigPath, _logger);

        _eventTypes.Clear();
        _eventTypes.AddRange(
            CachedEventTypes.Select(evt =>
            {
                var weight = configuredWeights.TryGetValue(evt.Type.Name, out var configured)
                    ? configured
                    : evt.Weight;
                return (weight, evt.Type);
            })
        );

        if (_eventTypes.Count == 0)
        {
            throw new InvalidOperationException(
                "Не знайдено жодного класу події з EventChanceAttribute.");
        }

        if (_eventTypes.All(x => x.Weight <= 0))
        {
            throw new InvalidOperationException("Всі ваги подій некоректні (<= 0).");
        }
    }

    public IReadOnlyCollection<Type> GetRegisteredEventTypes() =>
        _eventTypes.Select(x => x.Type).ToArray();

    public SimulationEvent Generate()
    {
        _logger.LogInformation(nameof(SimulationEventGenerator), "Генеруємо нову подію...");

        double totalWeight = _eventTypes.Sum(x => x.Weight);
        double roll = _random.NextDouble() * totalWeight;

        double cumulative = 0;
        foreach (var (weight, type) in _eventTypes)
        {
            cumulative += weight;
            if (roll <= cumulative)
            {
                var ev = (SimulationEvent)Activator.CreateInstance(type)!;
                _logger.LogInformation(nameof(SimulationEventGenerator), $"Згенерована подія: {ev.EventName}");
                return ev;
            }
        }

        var lastType = _eventTypes.Last().Type;
        var fallback = (SimulationEvent)Activator.CreateInstance(lastType)!;
        _logger.LogWarning(nameof(SimulationEventGenerator), $"Використовуємо fallback подію: {fallback.EventName}");
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
