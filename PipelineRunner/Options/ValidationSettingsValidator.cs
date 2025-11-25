using Microsoft.Extensions.Options;

namespace PipelineRunner.Options;

internal sealed class ValidationSettingsValidator : IValidateOptions<ValidationSettings>
{
    public ValidateOptionsResult Validate(string? name, ValidationSettings options)
    {
        if (options.EnableStructureAnalyzer
            || options.EnableTimeGridAnalyzer
            || options.EnablePhysicsAnalyzer
            || options.EnableAnomalyAnalyzer
            || options.EnableScenarioAnalyzer
            || options.EnableMlFitnessAnalyzer)
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail("At least one analyzer must be enabled.");
    }
}
