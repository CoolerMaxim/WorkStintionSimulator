using WorkstationJobSimulator.Logging;
using WorkstationJobSimulator.Models;

namespace WorkstationJobSimulator.Models.Workstation;

public class Workstation
{
    private readonly object _lock = new();
    private readonly ISimulationLogger _logger;

    public string Name { get; }

    public WorkstationState State { get; private set; } = WorkstationState.Idle;

    /// <summary>Чи є зараз живлення на станції.</summary>
    public bool IsPowerOn { get; private set; } = true;

    /// <summary>Чи активна зараз повітряна тривога.</summary>
    public bool IsAirAlarmActive { get; private set; } = false;

    public Workstation(string name, ISimulationLogger logger)
    {
        Name = name;
        _logger = logger;
        LogState("Ініціалізовано робочу станцію");
        PrintStatus();
    }

    private void ChangeState(WorkstationState newState, string reason)
    {
        if (State == newState)
        {
            return;
        }

        State = newState;
        LogState($"Стан змінено на: {State} ({reason})");
    }

    private void LogState(string message)
    {
        lock (_lock)
        {
            _logger.LogInformation($"WS:{Name}", message);
        }
    }

    /// <summary>
    /// Загальний лог для внутрішніх дій / подій.
    /// </summary>
    public void Log(string message)
    {
        lock (_lock)
        {
            _logger.LogInformation($"WS:{Name}", message);
        }
    }

    public void PrintStatus()
    {
        lock (_lock)
        {
            _logger.LogInformation($"WS:{Name}", $"Стан: {State}; Світло: {(IsPowerOn ? "УВІМКНЕНО" : "ВИМКНЕНО")}; Тривога: {(IsAirAlarmActive ? "АКТИВНА" : "НЕМАЄ")}");
        }
    }

    public void SetPower(bool isOn, string reason)
    {
        IsPowerOn = isOn;
        LogState($"Світло {(IsPowerOn ? "УВІМКНЕНО" : "ВИМКНЕНО")} ({reason})");
        PrintStatus();
    }

    public void SetAirAlarm(bool isActive, string reason)
    {
        IsAirAlarmActive = isActive;
        LogState($"Повітряна тривога {(IsAirAlarmActive ? "АКТИВНА" : "НЕМАЄ")} ({reason})");
        PrintStatus();
    }

    public void BeginEventProcessing(string eventName)
    {
        LogState($"Отримано подію: {eventName}");
        ChangeState(WorkstationState.Processing, $"починаємо обробку події {eventName}");
    }

    public void CompleteEventProcessing(string eventName)
    {
        ChangeState(WorkstationState.Idle, $"завершено обробку події {eventName}");
        Log($"Стан після обробки події \"{eventName}\":");
        PrintStatus();
    }
}
