namespace PipelineRunner.Options;

public sealed class DatasetSettings
{
    public required string WorkingDirectory { get; set; }

    public required string OutputPath { get; set; }

    public string? DatasetName { get; set; }

    public required string QualityMarkdownPath { get; set; }

    public required string QualityJsonPath { get; set; }

    public string ResolvedWorkingDirectory => Resolve(WorkingDirectory, ApplicationPaths.ApplicationRoot);

    public string ResolvedOutputPath => Resolve(OutputPath, ResolvedWorkingDirectory);

    public string ResolvedDatasetName => string.IsNullOrWhiteSpace(DatasetName)
        ? Path.GetFileNameWithoutExtension(ResolvedOutputPath)
        : DatasetName;

    public string ResolvedQualityMarkdownPath => Resolve(QualityMarkdownPath, ResolvedWorkingDirectory);

    public string ResolvedQualityJsonPath => Resolve(QualityJsonPath, ResolvedWorkingDirectory);

    private static string Resolve(string path, string basePath)
    {
        return Path.GetFullPath(path, basePath);
    }
}
