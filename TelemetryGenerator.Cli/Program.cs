using System.Globalization;
using System.Linq;
using TelemetryGenerator.Core;
using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.Modules;
using TelemetryGenerator.Core.Scenarios;
using TelemetryGenerator.Core.Services;

var arguments = ParseArguments(args);

if (!arguments.TryGetValue("--scenario", out var scenarioName))
{
    Console.Error.WriteLine("Missing required --scenario argument.");
    return 1;
}

if (!arguments.TryGetValue("--difficulty", out var difficultyValue) || !Enum.TryParse<Difficulty>(difficultyValue, true, out var difficulty))
{
    Console.Error.WriteLine("Missing or invalid --difficulty argument (easy|normal|hard).");
    return 1;
}

if (!arguments.TryGetValue("--start", out var startValue) || !DateTime.TryParse(startValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var startTime))
{
    Console.Error.WriteLine("Missing or invalid --start argument (ISO timestamp).");
    return 1;
}

if (!arguments.TryGetValue("--duration", out var durationValue) || !TryParseDuration(durationValue, out var duration))
{
    Console.Error.WriteLine("Missing or invalid --duration argument (e.g. 24h or 2d).");
    return 1;
}

if (!arguments.TryGetValue("--step-minutes", out var stepValue) || !int.TryParse(stepValue, out var stepMinutes) || stepMinutes <= 0)
{
    Console.Error.WriteLine("Missing or invalid --step-minutes argument.");
    return 1;
}

if (!arguments.TryGetValue("--workstation-id", out var workStationId) || string.IsNullOrWhiteSpace(workStationId))
{
    Console.Error.WriteLine("Missing --workstation-id argument.");
    return 1;
}

if (!arguments.TryGetValue("--speakers-configured", out var speakersValue) || !int.TryParse(speakersValue, out var speakers) || speakers < 1)
{
    Console.Error.WriteLine("Missing or invalid --speakers-configured argument.");
    return 1;
}

if (!arguments.TryGetValue("--output", out var outputPath) || string.IsNullOrWhiteSpace(outputPath))
{
    Console.Error.WriteLine("Missing --output argument.");
    return 1;
}

var step = TimeSpan.FromMinutes(stepMinutes);
var config = NodeConfig.CreateDefault(workStationId, speakers);
var anomalyInjector = new AnomalyInjector();
var maintenanceScheduler = new MaintenanceScheduler();
var batteryModel = new BatteryModel();
var temperatureModel = new TemperatureModel();
var profile = DifficultyProfiles.Create(difficulty);
var modules = new List<IModule>
{
    new PowerSupplyModule(),
    new MicroControllerModule(),
    new AmplifierModule(),
    new EnvironmentModule(temperatureModel),
    new NetworkControllerModule(new NetworkModel(), profile)
};

var generator = new TelemetryGenerator.Core.TelemetryGenerator(
    modules,
    batteryModel,
    anomalyInjector,
    maintenanceScheduler);

Scenario scenario;
try
{
    scenario = CreateScenario(scenarioName, difficulty, anomalyInjector);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
var endTime = startTime + duration;
var rnd = new Random();
var samples = generator
    .Run(config, scenario, startTime, step, rnd)
    .TakeWhile(sample => sample.Timestamp < endTime);

WriteCsv(outputPath, samples);

return 0;

static Scenario CreateScenario(string name, Difficulty difficulty, AnomalyInjector injector)
{
    return name.ToLowerInvariant() switch
    {
        "normal" or "normalday" or "normal-day" => ScenarioFactory.CreateNormalDayScenario(difficulty),
        "powerloss" or "longpowerloss" => ScenarioFactory.CreateLongPowerLossWithCutoffScenario(difficulty, injector),
        "speakers" or "speakers-degradation" => ScenarioFactory.CreateSpeakersDegradationWithRepairScenario(difficulty, injector),
        "net" or "net-failure" => ScenarioFactory.CreateNetDegradationToFailureScenario(difficulty, injector),
        "cooling" or "cooling-service" => ScenarioFactory.CreateCoolingDegradationWithServiceScenario(difficulty, injector),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown scenario.")
    };
}

static void WriteCsv(string outputPath, IEnumerable<TelemetryGenerator.Core.Telemetry.TelemetrySample> samples)
{
    var directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrEmpty(directory))
    {
        Directory.CreateDirectory(directory);
    }

    using var writer = new StreamWriter(outputPath);
    writer.WriteLine("Timestamp,WorkStationId,PowerStatus,BatteryStatus,BatteryVoltage,CpuTemperature,Temperature,DiskSpaceUse,DoorOpenStatus,AmplifierStatus,AmplifierOutPower,SoundStatus,SignalStrength,NetworkLatency,SpeakersConfigured,IsAnomaly,AnomalyType,MaintenanceType");

    foreach (var sample in samples)
    {
        writer.WriteLine(string.Join(',', new[]
        {
            sample.Timestamp.ToString("O", CultureInfo.InvariantCulture),
            sample.WorkStationId,
            sample.PowerStatus ? "true" : "false",
            sample.BatteryStatus ? "true" : "false",
            sample.BatteryVoltage.ToString("F2", CultureInfo.InvariantCulture),
            sample.CpuTemperature.ToString("F1", CultureInfo.InvariantCulture),
            sample.Temperature.ToString(CultureInfo.InvariantCulture),
            sample.DiskSpaceUse.ToString(CultureInfo.InvariantCulture),
            sample.DoorOpenStatus ? "true" : "false",
            sample.AmplifierStatus ? "true" : "false",
            sample.AmplifierOutPower.ToString("F3", CultureInfo.InvariantCulture),
            sample.SoundStatus ? "true" : "false",
            sample.SignalStrength.ToString("F1", CultureInfo.InvariantCulture),
            sample.NetworkLatency.ToString(CultureInfo.InvariantCulture),
            sample.SpeakersConfigured.ToString(CultureInfo.InvariantCulture),
            sample.IsAnomaly ? "true" : "false",
            sample.AnomalyType.ToString(),
            sample.MaintenanceType.ToString()
        }));
    }
}

static Dictionary<string, string> ParseArguments(string[] args)
{
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        var key = args[i];
        if (!key.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            dict[key] = args[i + 1];
            i++;
        }
        else
        {
            dict[key] = "true";
        }
    }

    return dict;
}

static bool TryParseDuration(string value, out TimeSpan duration)
{
    value = value.Trim();
    if (value.EndsWith("d", StringComparison.OrdinalIgnoreCase) && double.TryParse(value[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var days))
    {
        duration = TimeSpan.FromDays(days);
        return true;
    }

    if (value.EndsWith("h", StringComparison.OrdinalIgnoreCase) && double.TryParse(value[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var hours))
    {
        duration = TimeSpan.FromHours(hours);
        return true;
    }

    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var plainHours))
    {
        duration = TimeSpan.FromHours(plainHours);
        return true;
    }

    duration = TimeSpan.Zero;
    return false;
}
