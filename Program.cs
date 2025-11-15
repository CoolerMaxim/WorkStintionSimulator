using System.Linq;
using System.Text;
using System.Threading;
using WorkstationJobSimulator.EventPhysic;
using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Models;
using WorkstationJobSimulator.Models.wsModels;

Console.OutputEncoding = Encoding.UTF8;

var parameters = SimulationParameters.Default;

ShowBanner();
PrintBasicFunctionality();
PrintParameters(parameters);

Thread.Sleep(parameters.StartupDelay);

var generator = new SimulationEventGenerator(parameters);
var workstation = new Workstation(parameters.WorkstationName);
var physicsEngine = new WorkstationPhysicsEngine();
PhysicsRegistry.RegisterAllEventPhysics(physicsEngine);

ValidatePhysicsMappings(generator, physicsEngine);
DisplayInitialSystemState(workstation, physicsEngine);

Console.WriteLine("Натисніть будь-яку клавішу, щоб почати симуляцію...");
if (Console.IsInputRedirected)
{
    Console.WriteLine("[INIT] Вхід консолі перенаправлено, стартуємо автоматично...");
}
else
{
    Console.ReadKey(true);
}
Console.Clear();

for (int i = 0; i < parameters.Iterations; i++)
{
    Console.WriteLine(new string('=', 70));
    Console.WriteLine($"ІТЕРАЦІЯ #{i + 1}");

    var wait = generator.RollNextInterval();
    Console.WriteLine($"[LOG] Очікуємо наступну подію приблизно через {wait.TotalSeconds:F0} секунд...");
    WaitWithProgress(wait);

    var simulationEvent = generator.Generate();

    Console.WriteLine();
    ShowEventDetails(simulationEvent);

    physicsEngine.ApplyPhysics(workstation, simulationEvent);

    ShowCurrentSystemState(workstation);
}

Console.WriteLine(new string('=', 70));
Console.WriteLine("Симуляцію завершено. Натисніть будь-яку клавішу для виходу...");
if (!Console.IsInputRedirected)
{
    Console.ReadKey(true);
}

void ShowBanner()
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("============================================");
    Console.WriteLine("   WORKSTATION JOB SIMULATOR – СТАРТ");
    Console.WriteLine("============================================");
    Console.ResetColor();
}

void PrintBasicFunctionality()
{
    Console.WriteLine("Базовий функціонал симулятора:");
    Console.WriteLine(" • Генерація випадкових виробничих подій з ваговими коефіцієнтами.");
    Console.WriteLine(" • Застосування фізики подій до робочої станції.");
    Console.WriteLine(" • Відстеження станів: живлення, повітряна тривога та активність.");
    Console.WriteLine(" • Логування кожного кроку та відображення поточного стану системи.");
    Console.WriteLine();
}

void PrintParameters(SimulationParameters simulationParameters)
{
    Console.WriteLine("Поточні параметри запуску:");
    Console.WriteLine($" • Робоча станція: {simulationParameters.WorkstationName}");
    Console.WriteLine($" • Кількість ітерацій: {simulationParameters.Iterations}");
    Console.WriteLine($" • Інтервал між подіями: {simulationParameters.MinEventIntervalSeconds}-{simulationParameters.MaxEventIntervalSeconds} сек.");
    Console.WriteLine($" • Затримка перед стартом: {simulationParameters.StartupDelay.TotalSeconds:F0} сек.");
    Console.WriteLine();
}

void ValidatePhysicsMappings(SimulationEventGenerator eventGenerator, WorkstationPhysicsEngine engine)
{
    var unmappedEvents = engine.GetUnmappedEvents(eventGenerator.GetRegisteredEventTypes()).ToList();

    if (unmappedEvents.Count == 0)
    {
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine("[INIT] Усі доступні події мають зареєстровані фізичні обробники.");
        Console.ResetColor();
        return;
    }

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("[WARN] Для деяких подій відсутні модулі фізики:");
    Console.ResetColor();

    foreach (var eventType in unmappedEvents)
    {
        Console.WriteLine($"  - {eventType.Name}");
    }
    Console.WriteLine();
}

void ShowCurrentSystemState(Workstation ws)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine("[STATE] Поточний стан системи та модулів:");
    Console.ResetColor();

    Console.WriteLine($"    • Робоча станція: {ws.Name}");
    Console.WriteLine($"    • Стан виробничого процесу: {ws.State}");
    Console.WriteLine($"    • Модуль живлення: {(ws.IsPowerOn ? "УВІМКНЕНО" : "ВІДСУТНЄ")}");
    Console.WriteLine($"    • Модуль повітряної тривоги: {(ws.IsAirAlarmActive ? "АКТИВНИЙ" : "НЕ АКТИВНИЙ")}");
    Console.WriteLine();
    Console.WriteLine("    Деталі стану станції:");
    ws.PrintStatus();
}

void WaitWithProgress(TimeSpan waitDuration)
{
    if (waitDuration <= TimeSpan.Zero)
    {
        return;
    }

    const int updateIntervalMs = 1000;
    var totalMilliseconds = (int)Math.Round(waitDuration.TotalMilliseconds);

    var elapsed = 0;
    while (elapsed + updateIntervalMs <= totalMilliseconds)
    {
        Thread.Sleep(updateIntervalMs);
        elapsed += updateIntervalMs;

        var remaining = Math.Max(totalMilliseconds - elapsed, 0);
        Console.WriteLine(
            $"    ... минуло {elapsed / 1000} с (залишилось ≈ {Math.Ceiling(remaining / 1000.0)} с)");
    }

    var remainder = totalMilliseconds - elapsed;
    if (remainder > 0)
    {
        Thread.Sleep(remainder);
    }

    Console.WriteLine("    ... очікування завершено!");
}

void ShowEventDetails(SimulationEvent simulationEvent)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("[EVENT] Згенеровано нову подію в системі:");
    Console.ResetColor();
    PrintEvent(simulationEvent, indentLevel: 0);
}

void PrintEvent(SimulationEvent simulationEvent, int indentLevel)
{
    var indent = new string(' ', indentLevel * 4);
    Console.WriteLine(
        $"{indent}- {simulationEvent.EventName} (тривалість: {FormatDuration(simulationEvent.Duration)})");

    foreach (var subEvent in simulationEvent.SubEvents)
    {
        PrintEvent(subEvent, indentLevel + 1);
    }
}

string FormatDuration(TimeSpan duration)
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

void DisplayInitialSystemState(Workstation workstation, WorkstationPhysicsEngine engine)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine("[INIT] Початковий стан системи та активні модулі фізики:");
    Console.ResetColor();

    Console.WriteLine($"    • Робоча станція: {workstation.Name}");
    Console.WriteLine(
        $"    • Зареєстровано модулів фізики: {engine.RegisteredPhysicsTypes.Count}");

    foreach (var module in engine.RegisteredPhysicsTypes.OrderBy(t => t.Name))
    {
        Console.WriteLine($"       - {module.Name}");
    }

    Console.WriteLine();
    ShowCurrentSystemState(workstation);
}
