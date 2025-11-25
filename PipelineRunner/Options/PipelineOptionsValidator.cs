using System.Globalization;
using Microsoft.Extensions.Options;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Utilities;

namespace PipelineRunner.Options;

internal sealed class PipelineOptionsValidator : IValidateOptions<PipelineOptions>
{
    public ValidateOptionsResult Validate(string? name, PipelineOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Scenario))
        {
            failures.Add("Scenario is required.");
        }

        if (options.StepMinutes <= 0)
        {
            failures.Add("StepMinutes must be positive.");
        }

        if (options.SpeakersConfigured < 1)
        {
            failures.Add("SpeakersConfigured must be positive.");
        }

        if (string.IsNullOrWhiteSpace(options.WorkstationId))
        {
            failures.Add("WorkstationId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.OutputPath))
        {
            failures.Add("OutputPath is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SolutionPath))
        {
            failures.Add("SolutionPath is required.");
        }

        if (string.IsNullOrWhiteSpace(options.TelemetryProjectPath))
        {
            failures.Add("TelemetryProjectPath is required.");
        }

        if (!Enum.TryParse<Difficulty>(options.Difficulty, true, out _))
        {
            failures.Add($"Difficulty is invalid: {options.Difficulty}");
        }

        if (!DurationParser.TryParse(options.Duration, out _))
        {
            failures.Add($"Duration is invalid: {options.Duration}");
        }

        if (!DateTime.TryParse(options.Start, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _))
        {
            failures.Add($"Start is invalid: {options.Start}");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
