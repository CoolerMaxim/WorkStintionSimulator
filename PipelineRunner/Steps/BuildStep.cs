using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PipelineRunner.Options;

namespace PipelineRunner.Steps;

internal sealed class BuildStep
{
    private readonly ILogger<BuildStep> _logger;
    private readonly PipelineOptions _options;

    public BuildStep(IOptions<PipelineOptions> options, ILogger<BuildStep> logger)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building solution: {Solution}", _options.SolutionPath);
        var exitCode = ProcessRunner.Run(
            "dotnet",
            ["build", _options.SolutionPath, "-o", _options.BuildOutputDirectory],
            _options.WorkingDirectory,
            _logger);
        return Task.FromResult(exitCode);
    }
}
