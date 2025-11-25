using Microsoft.Extensions.Options;

namespace PipelineRunner.Options;

internal sealed class TrainingSettingsValidator : IValidateOptions<TrainingSettings>
{
    public ValidateOptionsResult Validate(string? name, TrainingSettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.WorkingDirectory))
        {
            failures.Add("WorkingDirectory is required.");
        }

        if (string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            failures.Add("OutputDirectory is required.");
        }

        if (options.TrainFraction <= 0 || options.TrainFraction >= 1)
        {
            failures.Add("TrainFraction must be between 0 and 1.");
        }

        if (options.EvaluationFraction < 0 || options.EvaluationFraction > 1)
        {
            failures.Add("EvaluationFraction must be between 0 and 1.");
        }

        if (string.IsNullOrWhiteSpace(options.ModelType))
        {
            failures.Add("ModelType is required.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
