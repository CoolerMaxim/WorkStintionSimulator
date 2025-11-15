using System;
using System.Linq;
using System.Threading;
using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Models;

namespace WorkstationJobSimulator.Models.wsModels;

public class Workstation
{
    private readonly object _lock = new();

    public string Name { get; }

    public WorkstationState State { get; private set; } = WorkstationState.Idle;

    /// <summary>Чи є зараз живлення на станції.</summary>
    public bool IsPowerOn { get; private set; } = true;

    /// <summary>Чи активна зараз повітряна тривога.</summary>
    public bool IsAirAlarmActive { get; private set; } = false;

    public Workstation(string name)
    {
        Name = name;
        LogState("Ініціалізовано робочу станцію");
        PrintStatus();
    }

    // =====================
    // Базова утиліта стану
    // =====================

    private void ChangeState(WorkstationState newState, string reason)
    {
        if (State == newState) return;

        State = newState;
        LogState($"Стан змінено на: {State} ({reason})");
    }

    private void LogState(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] [WS:{Name}] ");
            Console.ResetColor();
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Загальний лог для внутрішніх дій / подій.
    /// Доступний подіям (SimulationEvent) — тому public.
    /// </summary>
    public void Log(string message)
    {
        lock (_lock)
        {
            Console.WriteLine($"    {message}");
        }
    }

    /// <summary>
    /// Вивести поточний стан станції в консоль.
    /// </summary>
    public void PrintStatus()
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  --- Поточний стан робочої станції ---");
            Console.ResetColor();
            Console.WriteLine($"  Стан:        {State}");
            Console.WriteLine($"  Світло:      {(IsPowerOn ? "УВІМКНЕНО" : "ВИМКНЕНО")}");
            Console.WriteLine($"  Тривога:     {(IsAirAlarmActive ? "АКТИВНА" : "НЕМАЄ")}");
            Console.WriteLine("  -------------------------------------");
        }
    }

    // =====================
    // Методи, які ставлять на коробку сповіщення(як аудіофайл) т.щ.
    // =====================

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

    // =====================
    // Обробка подій
    // =====================

    public void ProcessEvent(SimulationEvent ev)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine();
        Console.WriteLine($"========== ПОЧАТОК ОБРОБКИ ПОДІЇ: \"{ev.EventName}\" ==========");
        Console.ResetColor();

        LogState($"Отримано подію: {ev.EventName}");
        ChangeState(WorkstationState.Processing, $"починаємо обробку події {ev.EventName}");

        Log($"Сталась подія \"{ev.EventName}\". Починаємо працювати над подією...");

        // Тут вся логіка піде у саму подію (AirAlarm / TurningOffTheLights)
        ev.Apply(this);

        Log("Стан після застосування події:");
        PrintStatus();

        Log("Іде обробка події... (імітація 1 сек)");
        Thread.Sleep(1000);

        ResetStateAfterEvent(ev);

        Log($"Завершили обробку події \"{ev.EventName}\".");
        ChangeState(WorkstationState.Idle, $"завершено обробку події {ev.EventName}");

        Log("Стан після відновлення:");
        PrintStatus();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"========== КІНЕЦЬ ПОДІЇ: \"{ev.EventName}\" ==========\n");
        Console.ResetColor();
    }

    private void ResetStateAfterEvent(SimulationEvent ev)
    {
        Log($"Повертаємо стан робочої станції до нормального після \"{ev.EventName}\"...");

        IsPowerOn = true;
        IsAirAlarmActive = false;

        Log(" -> Світло УВІМКНЕНО.");
        Log(" -> Повітряна тривога ВІДСУТНЯ.");
    }
}
