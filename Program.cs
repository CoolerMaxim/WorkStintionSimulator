using System.Text;
using WorkstationJobSimulator.EventPhysic;
using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Logging;
using WorkstationJobSimulator.Models;
using WorkstationJobSimulator.Models.Workstation;
using WorkstationJobSimulator.Simulation;

Console.OutputEncoding = Encoding.UTF8;

var parameters = SimulationParameters.Default;
var logger = new ConsoleSimulationLogger();
var generator = new SimulationEventGenerator(logger, parameters);
var workstation = new Workstation(parameters.WorkstationName, logger);
var physicsEngine = new WorkstationPhysicsEngine(logger);
PhysicsRegistry.RegisterAllEventPhysics(physicsEngine, logger);
var runner = new SimulationRunner(logger, parameters, generator, workstation, physicsEngine);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) =>
{
    logger.LogWarning("App", "Отримано Ctrl+C. Завершуємо симуляцію...");
    args.Cancel = true;
    cts.Cancel();
};

ShowBanner();
PrintBasicFunctionality();
PrintParameters(parameters, logger);

Console.WriteLine("Натисніть будь-яку клавішу, щоб почати симуляцію або Ctrl+C для виходу...");
if (!Console.IsInputRedirected)
{
    Console.ReadKey(true);
}

await runner.RunAsync(cts.Token);

Console.WriteLine("Симуляція завершена. Натисніть будь-яку клавішу для виходу...");
if (!Console.IsInputRedirected)
{
    Console.ReadKey(true);
}

static void ShowBanner()
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("============================================");
    Console.WriteLine("   WORKSTATION JOB SIMULATOR – СТАРТ");
    Console.WriteLine("============================================");
    Console.ResetColor();
}

static void PrintBasicFunctionality()
{
    Console.WriteLine("Базовий функціонал симулятора:");
    Console.WriteLine(" • Генерація випадкових виробничих подій з ваговими коефіцієнтами.");
    Console.WriteLine(" • Застосування фізики подій до робочої станції.");
    Console.WriteLine(" • Відстеження станів: живлення, повітряна тривога та активність.");
    Console.WriteLine(" • Логування кожного кроку та відображення поточного стану системи.");
    Console.WriteLine();
}

static void PrintParameters(SimulationParameters simulationParameters, ISimulationLogger logger)
{
    logger.LogInformation("Параметри", "Поточні параметри запуску:");
    logger.LogInformation("Параметри", $" • Робоча станція: {simulationParameters.WorkstationName}");
    logger.LogInformation("Параметри", $" • Кількість ітерацій: {simulationParameters.Iterations}");
    logger.LogInformation("Параметри", $" • Інтервал між подіями: {simulationParameters.MinEventIntervalSeconds}-{simulationParameters.MaxEventIntervalSeconds} сек.");
    logger.LogInformation("Параметри", $" • Затримка перед стартом: {simulationParameters.StartupDelay.TotalSeconds:F0} сек.");
    logger.LogInformation("Параметри", $" • Файл ваг подій: {simulationParameters.EventWeightsConfigPath ?? "<за замовчуванням>"}");
    logger.LogInformation("Параметри", $" • Seed генератора випадкових чисел: {simulationParameters.RandomSeed?.ToString() ?? "<random>"}");
    logger.LogInformation("Параметри", string.Empty);
}
