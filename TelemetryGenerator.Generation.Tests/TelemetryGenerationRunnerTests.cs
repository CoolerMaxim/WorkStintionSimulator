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

        Assert.NotNull(service.Config);
        Assert.Equal(simulationSettings.Scenario, service.Config!.Scenario);
        Assert.Equal(Difficulty.Hard, service.Config.Difficulty);
        Assert.Equal(DateTime.Parse(simulationSettings.Start), service.Config.Start);
        Assert.Equal(TimeSpan.FromHours(2), service.Config.Duration);
        Assert.Equal(TimeSpan.FromMinutes(simulationSettings.StepMinutes), service.Config.Step);
        Assert.Equal(simulationSettings.WorkstationId, service.Config.WorkstationId);
        Assert.Equal(simulationSettings.SpeakersConfigured, service.Config.SpeakersConfigured);
        Assert.Equal(simulationSettings.NodeProfile, service.Config.NodeProfile);
        Assert.Equal(Path.GetFullPath(simulationSettings.OutputPath), service.Config.OutputPath);
        Assert.Equal(simulationSettings.Seed, service.Config.Seed);
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

        Assert.NotNull(service.Config);
        Assert.Equal(request.Scenario, service.Config!.Scenario);
        Assert.Equal(Difficulty.Easy, service.Config.Difficulty);
        Assert.Equal(DateTime.Parse(request.Start), service.Config.Start);
        Assert.Equal(TimeSpan.FromHours(1), service.Config.Duration);
        Assert.Equal(TimeSpan.FromMinutes(request.StepMinutes), service.Config.Step);
        Assert.Equal(request.WorkstationId, service.Config.WorkstationId);
        Assert.Equal(request.SpeakersConfigured, service.Config.SpeakersConfigured);
        Assert.Equal(request.NodeProfile, service.Config.NodeProfile);
        Assert.Equal(Path.GetFullPath(request.OutputPath), service.Config.OutputPath);
        Assert.Equal(request.Seed, service.Config.Seed);
    }

    private sealed class CapturingGenerationService : ITelemetryGenerationService
    {
        public GenerationConfig? Config { get; private set; }

        public Task GenerateAsync(GenerationConfig config, CancellationToken cancellationToken = default)
        {
            Config = config;
            return Task.CompletedTask;
        }
    }
}
