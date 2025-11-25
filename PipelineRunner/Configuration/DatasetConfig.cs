namespace PipelineRunner.Configuration;

public sealed class DatasetConfig
{
    public string WorkingDirectory { get; init; } = string.Empty;

    public string OutputPath { get; init; } = string.Empty;

    public string DatasetName { get; init; } = string.Empty;

    public string QualityMarkdownPath { get; init; } = string.Empty;

    public string QualityJsonPath { get; init; } = string.Empty;
}
