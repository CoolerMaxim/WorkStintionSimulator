using AI.Training.Baseline;
using AI.Training.Data;
using AI.Training.Evaluation;
using AI.Training.Export;
using AI.Training.Features;
using AI.Training.Labeling;
using AI.Training.Models;
using AI.Training.Reports;
using AI.Training.Training;
using Microsoft.ML;

namespace AI.Training.Pipeline;

public class TrainingPipeline
{
    private readonly MLContext _mlContext;
    private readonly DatasetLoader _loader;
    private readonly FeatureExtractor _extractor;
    private readonly LabelProcessor _labels;
    private readonly ModelTrainer _trainer;
    private readonly ModelEvaluator _evaluator;
    private readonly ModelExporter _exporter;
    private readonly TrainingReportBuilder _reportBuilder;

    public TrainingPipeline(int seed = 7)
    {
        _mlContext = new MLContext(seed);
        _loader = new DatasetLoader(_mlContext);
        _extractor = new FeatureExtractor();
        _labels = new LabelProcessor();
        _trainer = new ModelTrainer(_mlContext);
        var baseline = new BaselineRuleModel();
        _evaluator = new ModelEvaluator(_mlContext, baseline);
        _exporter = new ModelExporter(_mlContext);
        _reportBuilder = new TrainingReportBuilder();
        Seed = seed;
    }

    public int Seed { get; }

    public (EvaluationReport Report, string ModelPath, string MetadataPath) Run(string csvPath, string outputDirectory)
    {
        var records = _loader.Load(csvPath);
        var (trainRecords, testRecords) = _loader.TemporalSplit(records);

        var trainFeatures = _extractor.Extract(trainRecords);
        var testFeatures = _extractor.Extract(testRecords);

        var trainInputs = _labels.ToModelInputs(trainFeatures);
        var testInputs = _labels.ToModelInputs(testFeatures);

        var trainingResult = _trainer.Train(trainInputs, testInputs.Take((int)(testInputs.Count * 0.5)));
        var report = _evaluator.Evaluate(trainingResult.Model, testInputs, testFeatures);

        var modelPath = Path.Combine(outputDirectory, "artifacts", "model.zip");
        _exporter.SaveModel(trainingResult.Model, _mlContext.Data.LoadFromEnumerable(trainInputs).Schema, modelPath);

        var metadata = new TrainingMetadata
        {
            GeneratorVersion = "v1",
            DataQualityVersion = "v1",
            TrainingVersion = "v1",
            Seed = Seed,
            FeatureList = new[]
            {
                nameof(FeatureVector.BatteryVoltage),
                nameof(FeatureVector.CpuTemperature),
                nameof(FeatureVector.InsideTemperature),
                nameof(FeatureVector.AmplifierOutPower),
                nameof(FeatureVector.NetworkLatencyMs),
                nameof(FeatureVector.SpeakersConfigured),
                nameof(FeatureVector.NodeUptimeMinutes),
                nameof(FeatureVector.TotalRuntimeHours),
                nameof(FeatureVector.RestartRate)
            },
            Metrics = new Dictionary<string, object>
            {
                ["MacroF1"] = report.ModelMetrics.MacroF1,
                ["CriticalF1"] = report.ModelMetrics.CriticalF1,
                ["FailedRecall"] = report.ModelMetrics.FailedRecall
            },
            DataProfile = new Dictionary<string, object>
            {
                ["TrainSamples"] = trainInputs.Count,
                ["TestSamples"] = testInputs.Count
            }
        };

        var metadataPath = _exporter.WriteMetadata(outputDirectory, metadata);

        var markdown = _reportBuilder.BuildMarkdown(report);
        File.WriteAllText(Path.Combine(outputDirectory, "TrainingReport.md"), markdown);

        return (report, modelPath, metadataPath);
    }
}
