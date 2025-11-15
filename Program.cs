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

Console.WriteLine("Натисніть будь-яку клавішу, щоб почати симуляцію...");
Console.ReadKey();
Console.Clear();

for (int i = 0; i < parameters.Iterations; i++)
{
    Console.WriteLine(new string('=', 70));
    Console.WriteLine($"ІТЕРАЦІЯ #{i + 1}");

    var wait = generator.RollNextInterval();
    Console.WriteLine($"[LOG] Очікуємо наступну подію приблизно через {wait.TotalSeconds:F0} секунд...");
    Thread.Sleep(wait);

    var simulationEvent = generator.Generate();

    Console.WriteLine();
    Console.WriteLine($"[ENGINE] Згенеровано подію: \"{simulationEvent.EventName}\" (тривалість: {simulationEvent.Duration})");

    physicsEngine.ApplyPhysics(workstation, simulationEvent);

    ShowCurrentSystemState(workstation);
}

Console.WriteLine(new string('=', 70));
Console.WriteLine("Симуляцію завершено. Натисніть будь-яку клавішу для виходу...");
Console.ReadKey();

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
    Console.WriteLine("[STATE] Підсумок поточного стану системи:");
    Console.ResetColor();
    ws.PrintStatus();
}
