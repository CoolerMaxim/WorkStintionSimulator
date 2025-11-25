using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TelemetryGenerator.Cli;
using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Services;
using Xunit;

namespace TelemetryGenerator.Generation.Tests;

public sealed class TelemetryGenerationRunnerTests
{
    [Fact]
    public async Task Uses_simulation_settings_when_request_is_empty()
    {
        var simulationSettings = new SimulationSettings
        {
            Scenario = "config-scenario",
            Difficulty = "Hard",
            Start = "2024-01-01T00:00:00Z",
            Duration = "2h",
            StepMinutes = 7,
            WorkstationId = "WS-123",
            SpeakersConfigured = 3,
            NodeProfile = "profile-a",
            OutputPath = "./data/out.csv",
            Seed = 42
        };

        var service = new CapturingGenerationService();
        var runner = new TelemetryGenerationRunner(
            service,
            Options.Create(simulationSettings),
            NullLogger<TelemetryGenerationRunner>.Instance);

        await runner.RunAsync(new TelemetryGenerationRequest());

        Assert.NotNull(service.Options);
        Assert.Equal(simulationSettings.Scenario, service.Options!.Scenario);
        Assert.Equal(Difficulty.Hard, service.Options.Difficulty);
        Assert.Equal(DateTime.Parse(simulationSettings.Start), service.Options.Start);
        Assert.Equal(TimeSpan.FromHours(2), service.Options.Duration);
        Assert.Equal(TimeSpan.FromMinutes(simulationSettings.StepMinutes), service.Options.Step);
        Assert.Equal(simulationSettings.WorkstationId, service.Options.WorkstationId);
        Assert.Equal(simulationSettings.SpeakersConfigured, service.Options.SpeakersConfigured);
        Assert.Equal(simulationSettings.NodeProfile, service.Options.NodeProfile);
        Assert.Equal(Path.GetFullPath(simulationSettings.OutputPath), service.Options.OutputPath);
        Assert.Equal(simulationSettings.Seed, service.Options.Seed);
    }

    [Fact]
    public async Task Request_overrides_simulation_settings()
    {
        var simulationSettings = new SimulationSettings
        {
            Scenario = "config-scenario",
            Difficulty = "Normal",
            Start = "2024-01-01T00:00:00Z",
            Duration = "2h",
            StepMinutes = 7,
            WorkstationId = "WS-123",
            SpeakersConfigured = 3,
            NodeProfile = "profile-a",
            OutputPath = "./data/out.csv",
            Seed = 42
        };

        var request = new TelemetryGenerationRequest
        {
            Scenario = "requested",
            Difficulty = "Easy",
            Start = "2024-03-01T10:00:00Z",
            Duration = "1h",
            StepMinutes = 10,
            WorkstationId = "OVERRIDE",
            SpeakersConfigured = 8,
            NodeProfile = "override-profile",
            OutputPath = "./override/out.csv",
            Seed = 7
        };

        var service = new CapturingGenerationService();
        var runner = new TelemetryGenerationRunner(
            service,
            Options.Create(simulationSettings),
            NullLogger<TelemetryGenerationRunner>.Instance);

        await runner.RunAsync(request);

        Assert.NotNull(service.Options);
        Assert.Equal(request.Scenario, service.Options!.Scenario);
        Assert.Equal(Difficulty.Easy, service.Options.Difficulty);
        Assert.Equal(DateTime.Parse(request.Start), service.Options.Start);
        Assert.Equal(TimeSpan.FromHours(1), service.Options.Duration);
        Assert.Equal(TimeSpan.FromMinutes(request.StepMinutes), service.Options.Step);
        Assert.Equal(request.WorkstationId, service.Options.WorkstationId);
        Assert.Equal(request.SpeakersConfigured, service.Options.SpeakersConfigured);
        Assert.Equal(request.NodeProfile, service.Options.NodeProfile);
        Assert.Equal(Path.GetFullPath(request.OutputPath), service.Options.OutputPath);
        Assert.Equal(request.Seed, service.Options.Seed);
    }

    private sealed class CapturingGenerationService : ITelemetryGenerationService
    {
        public TelemetryGenerationOptions? Options { get; private set; }

        public Task GenerateAsync(TelemetryGenerationOptions options, CancellationToken cancellationToken = default)
        {
            Options = options;
            return Task.CompletedTask;
        }
    }
}
