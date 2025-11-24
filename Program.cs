using System.Text;
using System.Text.Json;
using WorkstationJobSimulator.EventPhysic;
using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Logging;
using WorkstationJobSimulator.Models;
using WorkstationJobSimulator.Models.Workstation;
using WorkstationJobSimulator.Simulation;

Console.OutputEncoding = Encoding.UTF8;

var parameters = LoadParameters();
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

static SimulationParameters LoadParameters()
{
    var defaults = SimulationParameters.Default;
    var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    if (!File.Exists(configPath))
    {
        return defaults;
    }

    try
    {
        var json = File.ReadAllText(configPath);
        var loaded = JsonSerializer.Deserialize<SimulationParameters>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (loaded is null)
        {
            return defaults;
        }

        return new SimulationParameters
        {
            WorkstationName = string.IsNullOrWhiteSpace(loaded.WorkstationName)
                ? defaults.WorkstationName
                : loaded.WorkstationName,
            Iterations = loaded.Iterations > 0 ? loaded.Iterations : defaults.Iterations,
            MinEventIntervalSeconds = loaded.MinEventIntervalSeconds > 0
                ? loaded.MinEventIntervalSeconds
                : defaults.MinEventIntervalSeconds,
            MaxEventIntervalSeconds = loaded.MaxEventIntervalSeconds > 0
                ? loaded.MaxEventIntervalSeconds
                : defaults.MaxEventIntervalSeconds,
            StartupDelay = loaded.StartupDelay > TimeSpan.Zero ? loaded.StartupDelay : defaults.StartupDelay,
            EventWeightsConfigPath = string.IsNullOrWhiteSpace(loaded.EventWeightsConfigPath)
                ? defaults.EventWeightsConfigPath
                : loaded.EventWeightsConfigPath,
            RandomSeed = loaded.RandomSeed ?? defaults.RandomSeed
        };
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[config] Не вдалося прочитати appsettings.json: {ex.Message}");
        return defaults;
    }
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
