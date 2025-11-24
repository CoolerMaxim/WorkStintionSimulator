using System.Text.Json;

namespace AI.Training.Configuration;

public sealed class TrainingPipelineOptions
{
    public int Seed { get; init; } = 7;
    public double TrainFraction { get; init; } = 0.7;
    public double EvaluationFraction { get; init; } = 0.5;

    public static TrainingPipelineOptions LoadDefault()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var projectSpecific = Path.Combine(baseDirectory, "training.appsettings.json");
        var generic = Path.Combine(baseDirectory, "appsettings.json");

        if (File.Exists(projectSpecific))
        {
            return Load(projectSpecific);
        }

        return File.Exists(generic) ? Load(generic) : new TrainingPipelineOptions();
    }

    public static TrainingPipelineOptions Load(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                return new TrainingPipelineOptions();
            }

            var json = File.ReadAllText(configPath);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty("training", out var trainingSection))
            {
                return Deserialize(trainingSection.GetRawText());
            }

            return Deserialize(json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[config] Failed to parse {configPath}: {ex.Message}");
            return new TrainingPipelineOptions();
        }
    }

    private static TrainingPipelineOptions Deserialize(string json)
    {
        return JsonSerializer.Deserialize<TrainingPipelineOptions>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new TrainingPipelineOptions();
    }
}
