namespace PipelineRunner.Options;

public sealed class DatasetSettings
{
    public string WorkingDirectory { get; set; } = ApplicationPaths.ApplicationRoot;

    public string OutputPath { get; set; } = "./out/telemetry.csv";

    public string? DatasetName { get; set; }

    public string QualityMarkdownPath { get; set; } = "./out/DataQualityReport.md";

    public string QualityJsonPath { get; set; } = "./out/DataQualityReport.json";

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
