using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Services;
using TelemetryGenerator.Generation;

return await RunAsync(args);

static async Task<int> RunAsync(string[] args)
{
    var arguments = CreateDefaultArguments();
    var parsedArguments = ParseArguments(args);

    foreach (var (key, value) in parsedArguments)
    {
        arguments[key] = value;
    }

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

    var options = new TelemetryGenerationOptions
    {
        Scenario = scenarioName,
        Difficulty = difficulty,
        Start = startTime,
        Duration = duration,
        Step = TimeSpan.FromMinutes(stepMinutes),
        WorkstationId = workStationId,
        SpeakersConfigured = speakers,
        NodeProfile = arguments.TryGetValue("--node-profile", out var nodeProfileValue) ? nodeProfileValue : "randomized",
        OutputPath = outputPath
    };

    try
    {
        using var provider = new ServiceCollection()
            .AddTelemetryGeneration()
            .BuildServiceProvider();

        var generator = provider.GetRequiredService<ITelemetryGenerationService>();
        await generator.GenerateAsync(options);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[telemetry] generation failed: {ex.Message}");
        return 1;
    }

    return 0;
}

static Dictionary<string, string> CreateDefaultArguments()
{
    var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["--scenario"] = "normal-day",
        ["--difficulty"] = Difficulty.Normal.ToString(),
        ["--start"] = DateTime.Now.ToString("O", CultureInfo.InvariantCulture),
        ["--duration"] = "24h",
        ["--step-minutes"] = "5",
        ["--workstation-id"] = "WS-001",
        ["--speakers-configured"] = "4",
        ["--node-profile"] = "randomized",
        ["--output"] = Path.Combine(Environment.CurrentDirectory, "telemetry.csv")
    };

    var config = TelemetryCliConfig.Load(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
    Apply(defaults, "--scenario", config.Scenario);
    Apply(defaults, "--difficulty", config.Difficulty);
    Apply(defaults, "--start", config.Start);
    Apply(defaults, "--duration", config.Duration);
    Apply(defaults, "--step-minutes", config.StepMinutes?.ToString(CultureInfo.InvariantCulture));
    Apply(defaults, "--workstation-id", config.WorkstationId);
    Apply(defaults, "--speakers-configured", config.SpeakersConfigured?.ToString(CultureInfo.InvariantCulture));
    Apply(defaults, "--node-profile", config.NodeProfile);
    Apply(defaults, "--output", string.IsNullOrWhiteSpace(config.Output)
        ? null
        : Path.GetFullPath(config.Output!, Environment.CurrentDirectory));

    return defaults;
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

static void Apply(IDictionary<string, string> defaults, string key, string? value)
{
    if (!string.IsNullOrWhiteSpace(value))
    {
        defaults[key] = value;
    }
}

internal sealed class TelemetryCliConfig
{
    public string? Scenario { get; set; }
    public string? Difficulty { get; set; }
    public string? Start { get; set; }
    public string? Duration { get; set; }
    public int? StepMinutes { get; set; }
    public string? WorkstationId { get; set; }
    public int? SpeakersConfigured { get; set; }
    public string? NodeProfile { get; set; }
    public string? Output { get; set; }

    public static TelemetryCliConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return new TelemetryCliConfig();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<TelemetryCliConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new TelemetryCliConfig();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[config] Failed to parse appsettings.json: {ex.Message}");
            return new TelemetryCliConfig();
        }
    }
}
