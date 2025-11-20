using System.Text.Json;
using WorkstationJobSimulator.Logging;

namespace WorkstationJobSimulator.Events;

public record EventWeightConfiguration
{
    public string EventType { get; init; } = string.Empty;
    public double Weight { get; init; }
}

public static class EventConfigurationLoader
{
    public static IReadOnlyDictionary<string, double> LoadWeights(string? path, ISimulationLogger logger)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            logger.LogWarning(nameof(EventConfigurationLoader), "Файл конфігурації подій не заданий, використовуємо значення атрибутів за замовчуванням.");
            return new Dictionary<string, double>();
        }

        if (!File.Exists(path))
        {
            logger.LogWarning(nameof(EventConfigurationLoader), $"Файл конфігурації подій \"{path}\" не знайдено, використовуємо значення атрибутів за замовчуванням.");
            return new Dictionary<string, double>();
        }

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<EventWeightConfiguration[]>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (config is null || config.Length == 0)
            {
                logger.LogWarning(nameof(EventConfigurationLoader), "Конфігураційний файл порожній, використовуємо значення атрибутів за замовчуванням.");
                return new Dictionary<string, double>();
            }

            return config.ToDictionary(x => x.EventType, x => x.Weight, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            logger.LogError(nameof(EventConfigurationLoader), $"Не вдалося прочитати конфігурацію подій: {ex.Message}. Використовуємо значення атрибутів за замовчуванням.");
            return new Dictionary<string, double>();
        }
    }
}
