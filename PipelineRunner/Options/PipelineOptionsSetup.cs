using System.Linq;
using Microsoft.Extensions.Options;
using TelemetryGenerator.Core.Configuration;

namespace PipelineRunner.Options;

internal sealed class PipelineOptionsSetup : IPostConfigureOptions<PipelineOptions>
{
    public void PostConfigure(string? name, PipelineOptions options)
    {
        options.WorkingDirectory = string.IsNullOrWhiteSpace(options.WorkingDirectory)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(options.WorkingDirectory);

        if (string.IsNullOrWhiteSpace(options.Start))
        {
            options.Start = TelemetryDefaults.StartIsoString;
        }

        options.OutputPath = Normalize(options.OutputPath, options.WorkingDirectory);
        options.QualityMarkdownPath = Normalize(options.QualityMarkdownPath, options.WorkingDirectory);
        options.QualityJsonPath = Normalize(options.QualityJsonPath, options.WorkingDirectory);
        options.TrainingOutput = Normalize(options.TrainingOutput, options.WorkingDirectory);
        options.SolutionPath = Normalize(options.SolutionPath, options.WorkingDirectory);
        options.TelemetryProjectPath = Normalize(options.TelemetryProjectPath, options.WorkingDirectory);

        options.DatasetName ??= Path.GetFileNameWithoutExtension(options.OutputPath);

        options.Steps = options.Steps
            .SelectMany(step => step.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Normalize(string path, string basePath)
    {
        return Path.GetFullPath(path, basePath);
    }
}
