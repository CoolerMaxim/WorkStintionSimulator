using System.Diagnostics;
using System.Globalization;
using AI.Training.Pipeline;
using TelemetryGenerator.DataQualityChecker;
using TelemetryGenerator.DataQualityChecker.Models;

var config = PipelineConfig.Parse(args);

var steps = new List<PipelineStep>
{
    new("build", config.ShouldRunBuild, () => RunBuild(config)),
    new("telemetry", config.ShouldRunTelemetry, () => RunTelemetry(config)),
    new("check", config.ShouldRunCheck, () => RunQualityCheck(config)),
    new("train", config.ShouldRunTrain, () => RunTraining(config)),
    new("simulate", config.ShouldRunSimulate, () => RunSimulation(config))
};

foreach (var step in steps)
{
    if (!step.Enabled)
    {
        Console.WriteLine($"[skip] {step.Name} (not requested)");
        continue;
    }

    Console.WriteLine($"\n=== {step.Name.ToUpperInvariant()} ===");
    var exit = step.Action();
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

static int RunTelemetry(PipelineConfig config)
{
    var telemetryArgs = new List<string>
    {
        "run",
        "--project",
        config.TelemetryProjectPath,
        "--",
        "--scenario",
        config.Scenario,
        "--duration",
        config.Duration,
        "--step-minutes",
        config.StepMinutes.ToString(CultureInfo.InvariantCulture),
        "--workstation-id",
        config.WorkstationId,
        "--output",
        config.OutputPath,
        "--difficulty",
        config.Difficulty,
        "--start",
        config.Start,
        "--speakers-configured",
        config.SpeakersConfigured.ToString(CultureInfo.InvariantCulture),
        "--node-profile",
        config.NodeProfile
    };

    Console.WriteLine("Generating telemetry with:");
    Console.WriteLine($"  scenario={config.Scenario}, duration={config.Duration}, stepMinutes={config.StepMinutes}");
    Console.WriteLine($"  workstation={config.WorkstationId}, output={config.OutputPath}");

    return RunProcess("dotnet", telemetryArgs, config.WorkingDirectory);
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

static int RunSimulation(PipelineConfig config)
{
    Console.WriteLine($"Running workstation simulator: {config.WorkstationProjectPath}");
    return RunProcess("dotnet", ["run", "--project", config.WorkstationProjectPath], config.WorkingDirectory);
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
    private PipelineConfig(Dictionary<string, string> args)
    {
        WorkingDirectory = Directory.GetCurrentDirectory();
        SolutionPath = GetPath(args, "--solution", Path.Combine(WorkingDirectory, "TelemetryGenerator.sln"));
        TelemetryProjectPath = GetPath(args, "--telemetry-project", Path.Combine(WorkingDirectory, "TelemetryGenerator.Cli", "TelemetryGenerator.Cli.csproj"));
        WorkstationProjectPath = GetPath(args, "--simulate-project", Path.Combine(WorkingDirectory, "WorkstationJobSimulator.csproj"));

        Scenario = args.GetValueOrDefault("--scenario", "normal-day");
        Duration = args.GetValueOrDefault("--duration", "24h");
        StepMinutes = ParseInt(args.GetValueOrDefault("--step-minutes"), 5);
        WorkstationId = args.GetValueOrDefault("--workstation-id", "WS-001");
        Difficulty = args.GetValueOrDefault("--difficulty", "Normal");
        Start = args.GetValueOrDefault("--start", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
        SpeakersConfigured = ParseInt(args.GetValueOrDefault("--speakers-configured"), 4);
        NodeProfile = args.GetValueOrDefault("--node-profile", "randomized");

        OutputPath = GetPath(args, "--output", Path.Combine(WorkingDirectory, "out", "telemetry.csv"));
        var outputDirectory = Path.GetDirectoryName(OutputPath) ?? WorkingDirectory;
        DatasetName = args.GetValueOrDefault("--dataset-name", Path.GetFileNameWithoutExtension(OutputPath));
        QualityMarkdownPath = GetPath(args, "--quality-report", Path.Combine(outputDirectory, "DataQualityReport.md"));
        QualityJsonPath = GetPath(args, "--quality-json", Path.Combine(outputDirectory, "DataQualityReport.json"));
        TrainingOutput = GetPath(args, "--training-output", Path.Combine(outputDirectory, "training-output"));

        var stepFlags = new[] { "--build", "--telemetry", "--check", "--train", "--simulate" };
        var hasSpecificSteps = args.Keys.Any(k => stepFlags.Contains(k, StringComparer.OrdinalIgnoreCase));
        RunAll = args.ContainsKey("--all") || !hasSpecificSteps;
        Build = args.ContainsKey("--build");
        Telemetry = args.ContainsKey("--telemetry");
        Check = args.ContainsKey("--check");
        Train = args.ContainsKey("--train");
        Simulate = args.ContainsKey("--simulate");
    }

    public bool ShouldRunBuild => RunAll || Build;
    public bool ShouldRunTelemetry => RunAll || Telemetry;
    public bool ShouldRunCheck => RunAll || Check;
    public bool ShouldRunTrain => RunAll || Train;
    public bool ShouldRunSimulate => RunAll || Simulate;

    public bool RunAll { get; }
    public bool Build { get; }
    public bool Telemetry { get; }
    public bool Check { get; }
    public bool Train { get; }
    public bool Simulate { get; }
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
    public string WorkstationProjectPath { get; }
    public string WorkingDirectory { get; }

    public static PipelineConfig Parse(string[] args)
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

        return new PipelineConfig(map);
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

internal sealed record PipelineStep(string Name, bool Enabled, Func<int> Action);
