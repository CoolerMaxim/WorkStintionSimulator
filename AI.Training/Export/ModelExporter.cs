using System.Text.Json;
using AI.Training.Models;
using Microsoft.ML;

namespace AI.Training.Export;

public class TrainingMetadata
{
    public string GeneratorVersion { get; init; } = "unknown";
    public string DataQualityVersion { get; init; } = "unknown";
    public string TrainingVersion { get; init; } = "v1";
    public DateTimeOffset TrainingTimestamp { get; init; } = DateTimeOffset.UtcNow;
    public int Seed { get; init; }
    public IReadOnlyDictionary<string, object>? ModelParameters { get; init; }
    public IReadOnlyList<string> FeatureList { get; init; } = Array.Empty<string>();
    public string Target { get; init; } = "HealthState";
    public IReadOnlyDictionary<string, object>? Metrics { get; init; }
    public IReadOnlyDictionary<string, object>? DataProfile { get; init; }
}

public class ModelExporter
{
    private readonly MLContext _mlContext;

    public ModelExporter(MLContext mlContext)
    {
        _mlContext = mlContext;
    }

    public void SaveModel(ITransformer model, DataViewSchema schema, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        using var fs = File.Create(outputPath);
        _mlContext.Model.Save(model, schema, fs);
    }

    public string ExportOnnx(ITransformer model, DataViewSchema schema, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var onnxPath = Path.Combine(outputDirectory, "model.onnx");
        using var stream = File.Create(onnxPath);
        _mlContext.Model.ConvertToOnnx(model, null, stream);
        return onnxPath;
    }

    public string WriteMetadata(string outputDirectory, TrainingMetadata metadata)
    {
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, "TrainingMetadata.json");
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        return path;
    }
}
