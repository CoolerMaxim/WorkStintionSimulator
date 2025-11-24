using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using AI.Training.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Services;
using TelemetryGenerator.DataQualityChecker;
using TelemetryGenerator.DataQualityChecker.Models;
using TelemetryGenerator.Generation;

var config = PipelineConfig.Parse(args);
using var serviceProvider = CreateServiceProvider();
var telemetryService = serviceProvider.GetRequiredService<ITelemetryGenerationService>();

var steps = new List<PipelineStep>
{
    new("build", config.ShouldRunBuild, () => Task.FromResult(RunBuild(config))),
    new("telemetry", config.ShouldRunTelemetry, () => RunTelemetryAsync(config, telemetryService)),
    new("check", config.ShouldRunCheck, () => Task.FromResult(RunQualityCheck(config))),
    new("train", config.ShouldRunTrain, () => Task.FromResult(RunTraining(config)))
};

foreach (var step in steps)
{
    if (!step.Enabled)
    {
        Console.WriteLine($"[skip] {step.Name} (not requested)");
        continue;
    }

    Console.WriteLine($"\n=== {step.Name.ToUpperInvariant()} ===");
    var exit = await step.Action();
    Console.WriteLine($"[{step.Name}] completed with exit code {exit}\n");

    if (exit != 0)
    {
        return exit;
    }
}

return 0;

static int RunBuild(PipelineConfig config)
{
    Console.WriteLine($"Building solution: {config.SolutionPath}");
    return RunProcess("dotnet", ["build", config.SolutionPath], config.WorkingDirectory);
}

static async Task<int> RunTelemetryAsync(PipelineConfig config, ITelemetryGenerationService telemetryService)
{
    Console.WriteLine("Generating telemetry with:");
    Console.WriteLine($"  scenario={config.Scenario}, duration={config.Duration}, stepMinutes={config.StepMinutes}");
    Console.WriteLine($"  workstation={config.WorkstationId}, output={config.OutputPath}");

    if (!Enum.TryParse<Difficulty>(config.Difficulty, true, out var difficulty))
    {
        Console.Error.WriteLine($"[telemetry] invalid difficulty: {config.Difficulty}");
        return 1;
    }

    if (!DateTime.TryParse(config.Start, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var start))
    {
        Console.Error.WriteLine($"[telemetry] invalid start timestamp: {config.Start}");
        return 1;
    }

    if (!TryParseDuration(config.Duration, out var duration))
    {
        Console.Error.WriteLine($"[telemetry] invalid duration: {config.Duration}");
        return 1;
    }

    var options = new TelemetryGenerationOptions
    {
        Scenario = config.Scenario,
        Difficulty = difficulty,
        Start = start,
        Duration = duration,
        Step = TimeSpan.FromMinutes(config.StepMinutes),
        WorkstationId = config.WorkstationId,
        SpeakersConfigured = config.SpeakersConfigured,
        NodeProfile = config.NodeProfile,
        OutputPath = config.OutputPath
    };

    try
    {
        await telemetryService.GenerateAsync(options);
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[telemetry] generation failed: {ex.Message}");
        return 1;
    }
}

static int RunQualityCheck(PipelineConfig config)
{
    Console.WriteLine("Running data quality checks with:");
    Console.WriteLine($"  csv={config.OutputPath}");
    Console.WriteLine($"  dataset={config.DatasetName}");
    Console.WriteLine($"  markdown={config.QualityMarkdownPath}");
    Console.WriteLine($"  json={config.QualityJsonPath}");

    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(config.QualityMarkdownPath) ?? config.WorkingDirectory);
        var checker = new DataQualityChecker();
        var (markdown, json, report) = checker.Run(config.OutputPath, config.DatasetName);

        File.WriteAllText(config.QualityMarkdownPath, markdown);
        File.WriteAllText(config.QualityJsonPath, json);

        Console.WriteLine($"  outcome={report.Summary.Outcome} ({report.Summary.Details})");
        var exitCode = report.Summary.Outcome == QualityOutcome.Fail ? 2 : 0;
        return exitCode;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[check] failed: {ex.Message}");
        return 1;
    }
}

static int RunTraining(PipelineConfig config)
{
    Console.WriteLine("Training model with:");
    Console.WriteLine($"  csv={config.OutputPath}");
    Console.WriteLine($"  outputDir={config.TrainingOutput}");

    try
    {
        Directory.CreateDirectory(config.TrainingOutput);
        var pipeline = new TrainingPipeline();
        var (report, modelPath, metadataPath) = pipeline.Run(config.OutputPath, config.TrainingOutput);

        Console.WriteLine($"  metrics: MacroF1={report.ModelMetrics.MacroF1:F3}, CriticalF1={report.ModelMetrics.CriticalF1:F3}, FailedRecall={report.ModelMetrics.FailedRecall:F3}");
        Console.WriteLine($"  model={modelPath}");
        Console.WriteLine($"  metadata={metadataPath}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[train] failed: {ex.Message}");
        return 1;
    }
}

static ServiceProvider CreateServiceProvider()
{
    return new ServiceCollection()
        .AddTelemetryGeneration()
        .BuildServiceProvider();
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

static int RunProcess(string fileName, IEnumerable<string> arguments, string workingDirectory)
{
    using var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false
        }
    };

    foreach (var argument in arguments)
    {
        process.StartInfo.ArgumentList.Add(argument);
    }

    process.Start();
    process.WaitForExit();
    return process.ExitCode;
}

internal sealed class PipelineConfig
{
    private PipelineConfig(Dictionary<string, string> args, PipelineDefaults defaults)
    {
        WorkingDirectory = Directory.GetCurrentDirectory();
        SolutionPath = GetPath(args, "--solution", defaults.SolutionPath ?? Path.Combine(WorkingDirectory, "TelemetryGenerator.sln"));
        TelemetryProjectPath = GetPath(args, "--telemetry-project", defaults.TelemetryProjectPath ?? Path.Combine(WorkingDirectory, "TelemetryGenerator.Cli", "TelemetryGenerator.Cli.csproj"));

        Scenario = args.GetValueOrDefault("--scenario", defaults.Scenario ?? "normal-day");
        Duration = args.GetValueOrDefault("--duration", defaults.Duration ?? "24h");
        StepMinutes = ParseInt(args.GetValueOrDefault("--step-minutes"), defaults.StepMinutes ?? 5);
        WorkstationId = args.GetValueOrDefault("--workstation-id", defaults.WorkstationId ?? "WS-001");
        Difficulty = args.GetValueOrDefault("--difficulty", defaults.Difficulty ?? "Normal");
        Start = args.GetValueOrDefault("--start", defaults.Start ?? DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
        SpeakersConfigured = ParseInt(args.GetValueOrDefault("--speakers-configured"), defaults.SpeakersConfigured ?? 4);
        NodeProfile = args.GetValueOrDefault("--node-profile", defaults.NodeProfile ?? "randomized");

        OutputPath = GetPath(args, "--output", defaults.OutputPath ?? Path.Combine(WorkingDirectory, "out", "telemetry.csv"));
        var outputDirectory = Path.GetDirectoryName(OutputPath) ?? WorkingDirectory;
        DatasetName = args.GetValueOrDefault("--dataset-name", defaults.DatasetName ?? Path.GetFileNameWithoutExtension(OutputPath));
        QualityMarkdownPath = GetPath(args, "--quality-report", defaults.QualityMarkdownPath ?? Path.Combine(outputDirectory, "DataQualityReport.md"));
        QualityJsonPath = GetPath(args, "--quality-json", defaults.QualityJsonPath ?? Path.Combine(outputDirectory, "DataQualityReport.json"));
        TrainingOutput = GetPath(args, "--training-output", defaults.TrainingOutput ?? Path.Combine(outputDirectory, "training-output"));

        var stepFlags = new[] { "--build", "--telemetry", "--check", "--train" };
        var hasSpecificSteps = args.Keys.Any(k => stepFlags.Contains(k, StringComparer.OrdinalIgnoreCase));
        RunAll = args.ContainsKey("--all") || (!hasSpecificSteps && defaults.RunAll);
        Build = args.ContainsKey("--build") || defaults.Build;
        Telemetry = args.ContainsKey("--telemetry") || defaults.Telemetry;
        Check = args.ContainsKey("--check") || defaults.Check;
        Train = args.ContainsKey("--train") || defaults.Train;
    }

    public bool ShouldRunBuild => RunAll || Build;
    public bool ShouldRunTelemetry => RunAll || Telemetry;
    public bool ShouldRunCheck => RunAll || Check;
    public bool ShouldRunTrain => RunAll || Train;

    public bool RunAll { get; }
    public bool Build { get; }
    public bool Telemetry { get; }
    public bool Check { get; }
    public bool Train { get; }
    public string Scenario { get; }
    public string Duration { get; }
    public int StepMinutes { get; }
    public string WorkstationId { get; }
    public string OutputPath { get; }
    public string Difficulty { get; }
    public string Start { get; }
    public int SpeakersConfigured { get; }
    public string NodeProfile { get; }
    public string DatasetName { get; }
    public string QualityMarkdownPath { get; }
    public string QualityJsonPath { get; }
    public string TrainingOutput { get; }
    public string SolutionPath { get; }
    public string TelemetryProjectPath { get; }
    public string WorkingDirectory { get; }

    public static PipelineConfig Parse(string[] args)
    {
        var map = ParseArgs(args);
        var defaults = PipelineDefaults.Load(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));

        return new PipelineConfig(map, defaults);
    }

    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var key = args[i];
            if (!key.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                map[key] = args[i + 1];
                i++;
            }
            else
            {
                map[key] = string.Empty;
            }
        }

        return map;
    }

    private static string GetPath(Dictionary<string, string> args, string key, string defaultValue)
    {
        var value = args.GetValueOrDefault(key, defaultValue);
        return Path.GetFullPath(value, Directory.GetCurrentDirectory());
    }

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }
}

internal sealed record PipelineStep(string Name, bool Enabled, Func<Task<int>> Action);

internal sealed class PipelineDefaults
{
    public string? Scenario { get; set; }
    public string? Duration { get; set; }
    public int? StepMinutes { get; set; }
    public string? WorkstationId { get; set; }
    public string? Difficulty { get; set; }
    public string? Start { get; set; }
    public int? SpeakersConfigured { get; set; }
    public string? NodeProfile { get; set; }
    public string? OutputPath { get; set; }
    public string? DatasetName { get; set; }
    public string? QualityMarkdownPath { get; set; }
    public string? QualityJsonPath { get; set; }
    public string? TrainingOutput { get; set; }
    public string? SolutionPath { get; set; }
    public string? TelemetryProjectPath { get; set; }

    public bool RunAll { get; set; } = true;
    public bool Build { get; set; }
    public bool Telemetry { get; set; }
    public bool Check { get; set; }
    public bool Train { get; set; }

    public static PipelineDefaults Load(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return new PipelineDefaults();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<PipelineDefaults>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new PipelineDefaults();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[config] Failed to parse appsettings.json: {ex.Message}");
            return new PipelineDefaults();
        }
    }
}
