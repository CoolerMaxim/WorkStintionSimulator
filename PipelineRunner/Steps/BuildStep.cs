using Microsoft.Extensions.Logging;
using PipelineRunner.Configuration;

namespace PipelineRunner.Steps;

internal sealed class BuildStep
{
    private readonly ILogger<BuildStep> _logger;
    private readonly PipelineConfig _config;

    public BuildStep(PipelineConfig config, ILogger<BuildStep> logger)
    {
        _logger = logger;
        _config = config;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building solution: {Solution}", _config.SolutionPath);
        var exitCode = ProcessRunner.Run("dotnet", ["build", _config.SolutionPath], _config.WorkingDirectory, _logger);
        return Task.FromResult(exitCode);
    }
}
