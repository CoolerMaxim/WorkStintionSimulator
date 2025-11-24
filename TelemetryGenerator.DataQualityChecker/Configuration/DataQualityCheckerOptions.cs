using System.Text.Json;

namespace TelemetryGenerator.DataQualityChecker.Configuration;

public sealed class DataQualityCheckerOptions
{
    public bool EnableStructureAnalyzer { get; init; } = true;
    public bool EnableTimeGridAnalyzer { get; init; } = true;
    public bool EnablePhysicsAnalyzer { get; init; } = true;
    public bool EnableAnomalyAnalyzer { get; init; } = true;
    public bool EnableScenarioAnalyzer { get; init; } = true;
    public bool EnableMlFitnessAnalyzer { get; init; } = true;

    public static DataQualityCheckerOptions LoadDefault()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var projectSpecific = Path.Combine(baseDirectory, "dataquality.appsettings.json");
        var generic = Path.Combine(baseDirectory, "appsettings.json");

        if (File.Exists(projectSpecific))
        {
            return Load(projectSpecific);
        }

        return File.Exists(generic) ? Load(generic) : new DataQualityCheckerOptions();
    }

    public static DataQualityCheckerOptions Load(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                return new DataQualityCheckerOptions();
            }

            var json = File.ReadAllText(configPath);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty("dataQualityChecker", out var section))
            {
                return Deserialize(section.GetRawText());
            }

            return Deserialize(json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[config] Failed to parse {configPath}: {ex.Message}");
            return new DataQualityCheckerOptions();
        }
    }

    private static DataQualityCheckerOptions Deserialize(string json)
    {
        return JsonSerializer.Deserialize<DataQualityCheckerOptions>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new DataQualityCheckerOptions();
    }
}
