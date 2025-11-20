using System.Reflection;
using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Logging;
using WorkstationJobSimulator.Models.Workstation;

namespace WorkstationJobSimulator.EventPhysic;

public class WorkstationPhysicsEngine
{
    private readonly Dictionary<Type, IEventPhysics> _handlers = new();
    private readonly ISimulationLogger _logger;

    public WorkstationPhysicsEngine(ISimulationLogger logger)
    {
        _logger = logger;
    }

    public void Register(IEventPhysics physics)
    {
        _handlers[physics.EventType] = physics;
    }

    public void RegisterFromAssembly(Assembly assembly)
    {
        var physicsTypes = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(IEventPhysics).IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToArray();

        foreach (var type in physicsTypes)
        {
            if (Activator.CreateInstance(type) is IEventPhysics physics)
            {
                Register(physics);
            }
        }

        _logger.LogInformation(nameof(WorkstationPhysicsEngine), $"Зареєстровано {physicsTypes.Length} модулів фізики.");
    }

    public IReadOnlyCollection<Type> RegisteredPhysicsTypes => _handlers.Keys.ToArray();

    public bool HasPhysicsFor(Type eventType) => _handlers.ContainsKey(eventType);

    public IEnumerable<Type> GetUnmappedEvents(IEnumerable<Type> eventTypes)
    {
        foreach (var eventType in eventTypes)
        {
            if (!HasPhysicsFor(eventType))
            {
                yield return eventType;
            }
        }
    }

    public async Task ApplyPhysicsAsync(Workstation workstation, SimulationEvent simulationEvent, CancellationToken cancellationToken)
    {
        workstation.BeginEventProcessing(simulationEvent.EventName);
        workstation.Log($"[Engine] Починаємо обробку події \"{simulationEvent.EventName}\"");

        await ProcessEventAsync(workstation, simulationEvent, cancellationToken);

        workstation.Log($"[Engine] Завершено обробку події \"{simulationEvent.EventName}\"");
        workstation.CompleteEventProcessing(simulationEvent.EventName);
    }

    private async Task ProcessEventAsync(Workstation workstation, SimulationEvent simulationEvent, CancellationToken cancellationToken)
    {
        if (_handlers.TryGetValue(simulationEvent.GetType(), out var handler))
        {
            await handler.ApplyAsync(workstation, simulationEvent, cancellationToken);
        }
        else
        {
            workstation.Log($"[Engine] Немає фізики для події {simulationEvent.GetType().Name}, пропускаємо.");
        }

        if (simulationEvent.SubEvents.Count == 0)
        {
            return;
        }

        workstation.Log($"[Engine] Опрацьовуємо {simulationEvent.SubEvents.Count} підівент(и) для \"{simulationEvent.EventName}\"");
        foreach (var subEvent in simulationEvent.SubEvents)
        {
            await ProcessEventAsync(workstation, subEvent, cancellationToken);
        }
    }
}
