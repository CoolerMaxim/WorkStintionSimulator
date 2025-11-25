using Microsoft.Extensions.Options;

namespace PipelineRunner.Options;

internal sealed class DatasetSettingsValidator : IValidateOptions<DatasetSettings>
{
    public ValidateOptionsResult Validate(string? name, DatasetSettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.WorkingDirectory))
        {
            failures.Add("WorkingDirectory is required.");
        }

        if (string.IsNullOrWhiteSpace(options.OutputPath))
        {
            failures.Add("OutputPath is required.");
        }

        if (string.IsNullOrWhiteSpace(options.QualityMarkdownPath))
        {
            failures.Add("QualityMarkdownPath is required.");
        }

        if (string.IsNullOrWhiteSpace(options.QualityJsonPath))
        {
            failures.Add("QualityJsonPath is required.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
