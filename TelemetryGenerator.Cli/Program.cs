using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Services;
using TelemetryGenerator.Generation;
using System.Linq;

var config = TelemetryCliConfig.Load(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
return await BuildCommandLine(config).Build().InvokeAsync(args);

static CommandLineBuilder BuildCommandLine(TelemetryCliConfig config)
{
    var scenarioOption = new Option<string>("--scenario", () => config.Scenario ?? "normal-day", "Telemetry scenario to run (normal-day, powerloss, speakers, net, cooling)");

    var difficultyOption = new Option<Difficulty>(
        "--difficulty",
        parseArgument: result => ParseDifficulty(result, config.Difficulty),
        description: "Difficulty preset for anomaly frequency (easy, normal, hard)");

    var startOption = new Option<DateTime>(
        "--start",
        parseArgument: result => ParseStart(result, config.Start),
        description: "ISO-8601 timestamp for when telemetry generation starts");

    var durationOption = new Option<TimeSpan>(
        "--duration",
        parseArgument: result => ParseDuration(result, config.Duration),
        description: "Generation duration (e.g. 24h, 2d, 1.5)");

    var stepMinutesOption = new Option<int>(
        "--step-minutes",
        () => config.StepMinutes ?? 5,
        "Sampling cadence in minutes (must be positive)");
    stepMinutesOption.AddValidator(ctx =>
    {
        if (ctx.GetValueForOption(stepMinutesOption) <= 0)
        {
            ctx.ErrorMessage = "--step-minutes must be greater than zero.";
        }
    });

    var workstationIdOption = new Option<string>(
        "--workstation-id",
        () => config.WorkstationId ?? "WS-001",
        "Identifier for the workstation in telemetry records");

    var speakersConfiguredOption = new Option<int>(
        "--speakers-configured",
        () => config.SpeakersConfigured ?? 4,
        "Number of speakers configured (must be at least 1)");
    speakersConfiguredOption.AddValidator(ctx =>
    {
        if (ctx.GetValueForOption(speakersConfiguredOption) < 1)
        {
            ctx.ErrorMessage = "--speakers-configured must be at least 1.";
        }
    });

    var nodeProfileOption = new Option<string>(
        "--node-profile",
        () => string.IsNullOrWhiteSpace(config.NodeProfile) ? "randomized" : config.NodeProfile!,
        "Node profile: randomized (default), fixed/baseline, or mixed");
    nodeProfileOption.AddValidator(ctx =>
    {
        var value = ctx.GetValueForOption(nodeProfileOption);
        if (!IsSupportedNodeProfile(value))
        {
            ctx.ErrorMessage = "--node-profile must be one of: randomized, random, mixed, fixed, baseline.";
        }
    });

    var outputOption = new Option<string>(
        "--output",
        () => ResolveOutputPath(config.Output),
        "Path to the CSV file where telemetry will be written");

    var seedOption = new Option<int?>(
        "--seed",
        () => config.Seed,
        "Seed for deterministic telemetry generation (non-negative integer)");
    seedOption.AddValidator(ctx =>
    {
        var seed = ctx.GetValueForOption(seedOption);
        if (seed is < 0)
        {
            ctx.ErrorMessage = "--seed must be non-negative.";
        }
    });

    var rootCommand = new RootCommand("Generate synthetic workstation telemetry CSV files.")
    {
        scenarioOption,
        difficultyOption,
        startOption,
        durationOption,
        stepMinutesOption,
        workstationIdOption,
        speakersConfiguredOption,
        nodeProfileOption,
        outputOption,
        seedOption
    };

    rootCommand.AddValidator(ctx =>
    {
        if (string.IsNullOrWhiteSpace(ctx.GetValueForOption(workstationIdOption)))
        {
            ctx.ErrorMessage = "--workstation-id cannot be empty.";
            }

        if (string.IsNullOrWhiteSpace(ctx.GetValueForOption(scenarioOption)))
        {
            ctx.ErrorMessage = "--scenario cannot be empty.";
        }

        if (ctx.GetValueForOption(durationOption) <= TimeSpan.Zero)
        {
            ctx.ErrorMessage = "--duration must be greater than zero.";
        }

        var stepMinutes = ctx.GetValueForOption(stepMinutesOption);
        var durationMinutes = ctx.GetValueForOption(durationOption).TotalMinutes;
        if (durationMinutes > 0 && stepMinutes > durationMinutes)
        {
            ctx.ErrorMessage = "--step-minutes cannot exceed the total duration in minutes.";
        }

        if (string.IsNullOrWhiteSpace(ctx.GetValueForOption(outputOption)))
        {
            ctx.ErrorMessage = "--output cannot be empty.";
        }
    });

    rootCommand.SetHandler(async context =>
    {
        var options = BuildTelemetryGenerationOptions(
            context.ParseResult.GetValueForOption(scenarioOption)!,
            context.ParseResult.GetValueForOption(difficultyOption),
            context.ParseResult.GetValueForOption(startOption),
            context.ParseResult.GetValueForOption(durationOption),
            context.ParseResult.GetValueForOption(stepMinutesOption),
            context.ParseResult.GetValueForOption(workstationIdOption)!,
            context.ParseResult.GetValueForOption(speakersConfiguredOption),
            context.ParseResult.GetValueForOption(nodeProfileOption)!,
            context.ParseResult.GetValueForOption(outputOption)!,
            context.ParseResult.GetValueForOption(seedOption));

        await RunGeneratorAsync(options);
    });

    return new CommandLineBuilder(rootCommand)
        .UseDefaults()
        .UseExceptionHandler((exception, context) =>
        {
            Console.Error.WriteLine($"[telemetry] {exception.Message}");
            context.ExitCode = 1;
        });
}

static TelemetryGenerationOptions BuildTelemetryGenerationOptions(
    string scenario,
    Difficulty difficulty,
    DateTime start,
    TimeSpan duration,
    int stepMinutes,
    string workstationId,
    int speakersConfigured,
    string nodeProfile,
    string output,
    int? seed)
{
    var resolvedProfile = string.IsNullOrWhiteSpace(nodeProfile) ? "randomized" : nodeProfile;

    return new TelemetryGenerationOptions
    {
        Scenario = scenario,
        Difficulty = difficulty,
        Start = start,
        Duration = duration,
        Step = TimeSpan.FromMinutes(stepMinutes),
        WorkstationId = workstationId.Trim(),
        SpeakersConfigured = speakersConfigured,
        NodeProfile = resolvedProfile,
        OutputPath = Path.GetFullPath(output),
        Seed = seed
    };
}

static async Task RunGeneratorAsync(TelemetryGenerationOptions options)
{
    using var provider = new ServiceCollection()
        .AddTelemetryGeneration()
        .BuildServiceProvider();

    var generator = provider.GetRequiredService<ITelemetryGenerationService>();
    await generator.GenerateAsync(options).ConfigureAwait(false);
}

static Difficulty ParseDifficulty(ArgumentResult result, string? configuredDifficulty)
{
    var token = result.Tokens.FirstOrDefault()?.Value ?? configuredDifficulty ?? Difficulty.Normal.ToString();
    if (Enum.TryParse<Difficulty>(token, true, out var difficulty))
    {
        return difficulty;
    }

    result.ErrorMessage = "--difficulty must be one of: easy, normal, hard.";
    return Difficulty.Normal;
}

static DateTime ParseStart(ArgumentResult result, string? configuredStart)
{
    var token = result.Tokens.FirstOrDefault()?.Value ?? configuredStart ?? DateTime.Now.ToString("O", CultureInfo.InvariantCulture);
    if (DateTime.TryParse(token, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var start))
    {
        return start;
    }

    result.ErrorMessage = "--start must be a valid ISO-8601 timestamp (e.g. 2024-01-01T09:00:00).";
    return DateTime.Now;
}

static TimeSpan ParseDuration(ArgumentResult result, string? configuredDuration)
{
    var token = result.Tokens.FirstOrDefault()?.Value ?? configuredDuration ?? "24h";
    if (TryParseDuration(token, out var duration))
    {
        return duration;
    }

    result.ErrorMessage = "--duration must be a number of hours or suffixed with h/d (e.g. 24h, 2d, 1.5).";
    return TimeSpan.FromHours(24);
}

static string ResolveOutputPath(string? configuredOutput)
{
    var configured = string.IsNullOrWhiteSpace(configuredOutput)
        ? Path.Combine(Environment.CurrentDirectory, "telemetry.csv")
        : configuredOutput!;

    return Path.GetFullPath(configured, Environment.CurrentDirectory);
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

static bool IsSupportedNodeProfile(string? profile)
{
    if (string.IsNullOrWhiteSpace(profile))
    {
        return false;
    }

    return profile.ToLowerInvariant() switch
    {
        "fixed" => true,
        "baseline" => true,
        "random" => true,
        "randomized" => true,
        "mixed" => true,
        _ => false
    };
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
    public int? Seed { get; set; }

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
