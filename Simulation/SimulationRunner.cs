using WorkstationJobSimulator.EventPhysic;
using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Logging;
using WorkstationJobSimulator.Models;
using WorkstationJobSimulator.Models.Workstation;

namespace WorkstationJobSimulator.Simulation;

public class SimulationRunner
{
    private readonly ISimulationLogger _logger;
    private readonly SimulationParameters _parameters;
    private readonly SimulationEventGenerator _generator;
    private readonly Workstation _workstation;
    private readonly WorkstationPhysicsEngine _physicsEngine;

    public SimulationRunner(
        ISimulationLogger logger,
        SimulationParameters parameters,
        SimulationEventGenerator generator,
        Workstation workstation,
        WorkstationPhysicsEngine physicsEngine)
    {
        _logger = logger;
        _parameters = parameters;
        _generator = generator;
        _workstation = workstation;
        _physicsEngine = physicsEngine;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        ValidatePhysicsMappings();
        DisplayInitialSystemState();

        if (_parameters.StartupDelay > TimeSpan.Zero)
        {
            _logger.LogInformation(nameof(SimulationRunner), $"Очікуємо {FormatDuration(_parameters.StartupDelay)} перед стартом симуляції...");
            await Task.Delay(_parameters.StartupDelay, cancellationToken);
        }

        for (int i = 0; i < _parameters.Iterations && !cancellationToken.IsCancellationRequested; i++)
        {
            _logger.LogInformation(nameof(SimulationRunner), new string('=', 70));
            _logger.LogInformation(nameof(SimulationRunner), $"ІТЕРАЦІЯ #{i + 1}");

            var wait = _generator.RollNextInterval();
            _logger.LogInformation(nameof(SimulationRunner), $"Очікуємо наступну подію приблизно через {wait.TotalSeconds:F0} секунд...");
            await WaitWithProgressAsync(wait, cancellationToken);

            var simulationEvent = _generator.Generate();
            ShowEventDetails(simulationEvent);

            await _physicsEngine.ApplyPhysicsAsync(_workstation, simulationEvent, cancellationToken);

            ShowCurrentSystemState();
        }

        _logger.LogInformation(nameof(SimulationRunner), new string('=', 70));
        _logger.LogInformation(nameof(SimulationRunner), "Симуляцію завершено.");
    }

    private void ValidatePhysicsMappings()
    {
        var unmappedEvents = _physicsEngine.GetUnmappedEvents(_generator.GetRegisteredEventTypes()).ToList();

        if (unmappedEvents.Count == 0)
        {
            _logger.LogInformation(nameof(SimulationRunner), "Усі доступні події мають зареєстровані фізичні обробники.");
            return;
        }

        _logger.LogWarning(nameof(SimulationRunner), "Для деяких подій відсутні модулі фізики:");
        foreach (var eventType in unmappedEvents)
        {
            _logger.LogWarning(nameof(SimulationRunner), $"  - {eventType.Name}");
        }
    }

    private void ShowCurrentSystemState()
    {
        _logger.LogInformation(nameof(SimulationRunner), "[STATE] Поточний стан системи та модулів:");
        _logger.LogInformation(nameof(SimulationRunner), $"    • Робоча станція: {_workstation.Name}");
        _logger.LogInformation(nameof(SimulationRunner), $"    • Стан виробничого процесу: {_workstation.State}");
        _logger.LogInformation(nameof(SimulationRunner), $"    • Модуль живлення: {(_workstation.IsPowerOn ? "УВІМКНЕНО" : "ВІДСУТНЄ")}");
        _logger.LogInformation(nameof(SimulationRunner), $"    • Модуль повітряної тривоги: {(_workstation.IsAirAlarmActive ? "АКТИВНИЙ" : "НЕ АКТИВНИЙ")}");
        _workstation.PrintStatus();
    }

    private async Task WaitWithProgressAsync(TimeSpan waitDuration, CancellationToken cancellationToken)
    {
        if (waitDuration <= TimeSpan.Zero)
        {
            return;
        }

        const int updateIntervalMs = 1000;
        var totalMilliseconds = (int)Math.Round(waitDuration.TotalMilliseconds);
        var elapsed = 0;

        while (elapsed + updateIntervalMs <= totalMilliseconds && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(updateIntervalMs, cancellationToken);
            elapsed += updateIntervalMs;

            var remaining = Math.Max(totalMilliseconds - elapsed, 0);
            _logger.LogInformation(nameof(SimulationRunner),
                $"... минуло {elapsed / 1000} с (залишилось ≈ {Math.Ceiling(remaining / 1000.0)} с)");
        }

        var remainder = totalMilliseconds - elapsed;
        if (remainder > 0 && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(remainder, cancellationToken);
        }

        _logger.LogInformation(nameof(SimulationRunner), "... очікування завершено!");
    }

    private void ShowEventDetails(SimulationEvent simulationEvent)
    {
        _logger.LogInformation(nameof(SimulationRunner), "[EVENT] Згенеровано нову подію в системі:");
        PrintEvent(simulationEvent, indentLevel: 0);
    }

    private void PrintEvent(SimulationEvent simulationEvent, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        _logger.LogInformation(nameof(SimulationRunner),
            $"{indent}- {simulationEvent.EventName} (тривалість: {FormatDuration(simulationEvent.Duration)})");

        foreach (var subEvent in simulationEvent.SubEvents)
        {
            PrintEvent(subEvent, indentLevel + 1);
        }
    }

    private void DisplayInitialSystemState()
    {
        _logger.LogInformation(nameof(SimulationRunner), "[INIT] Початковий стан системи та активні модулі фізики:");
        _logger.LogInformation(nameof(SimulationRunner), $"    • Робоча станція: {_workstation.Name}");
        _logger.LogInformation(nameof(SimulationRunner), $"    • Зареєстровано модулів фізики: {_physicsEngine.RegisteredPhysicsTypes.Count}");

        foreach (var module in _physicsEngine.RegisteredPhysicsTypes.OrderBy(t => t.Name))
        {
            _logger.LogInformation(nameof(SimulationRunner), $"       - {module.Name}");
        }

        ShowCurrentSystemState();
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{duration.TotalHours:F1} год";
        }

        if (duration.TotalMinutes >= 1)
        {
            return $"{duration.TotalMinutes:F0} хв";
        }

        return $"{duration.TotalSeconds:F0} с";
    }
}
