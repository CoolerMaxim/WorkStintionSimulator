using AI.Training.Configuration;
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
    private readonly TrainingPipelineOptions _options;
    private readonly MLContext _mlContext;
    private readonly DatasetLoader _loader;
    private readonly FeatureExtractor _extractor;
    private readonly LabelProcessor _labels;
    private readonly ModelTrainer _trainer;
    private readonly ModelEvaluator _evaluator;
    private readonly ModelExporter _exporter;
    private readonly TrainingReportBuilder _reportBuilder;

    public TrainingPipeline(TrainingPipelineOptions? options = null)
    {
        _options = options ?? TrainingPipelineOptions.LoadDefault();
        _mlContext = new MLContext(_options.Seed);
        _loader = new DatasetLoader(_mlContext);
        _extractor = new FeatureExtractor();
        _labels = new LabelProcessor();
        _trainer = new ModelTrainer(_mlContext);
        var baseline = new BaselineRuleModel();
        _evaluator = new ModelEvaluator(_mlContext, baseline);
        _exporter = new ModelExporter(_mlContext);
        _reportBuilder = new TrainingReportBuilder();
        Seed = _options.Seed;
    }

    public int Seed { get; }

    public (EvaluationReport Report, string ModelPath, string MetadataPath) Run(string csvPath, string outputDirectory)
    {
        var records = _loader.Load(csvPath);
        var (trainRecords, testRecords) = _loader.TemporalSplit(records, _options.TrainFraction);

        var trainFeatures = _extractor.Extract(trainRecords).ToList();
        var testFeatures = _extractor.Extract(testRecords).ToList();

        var trainInputs = _labels.ToModelInputs(trainFeatures).ToList();
        var testInputs = _labels.ToModelInputs(testFeatures).ToList();

        EnsureTrainingHasMultipleClasses(trainInputs, trainFeatures, testInputs, testFeatures);

        var evaluationCount = CalculateEvaluationCount(testInputs.Count);
        var evaluationInputs = testInputs.Take(evaluationCount).ToList();
        var evaluationFeatures = testFeatures.Take(evaluationCount).ToList();

        var trainingResult = _trainer.Train(trainInputs, evaluationInputs);
        var report = _evaluator.Evaluate(trainingResult.Model, evaluationInputs, evaluationFeatures);

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

    private int CalculateEvaluationCount(int testCount)
    {
        if (testCount == 0)
        {
            return 0;
        }

        var evaluationCount = (int)(testCount * _options.ClampedEvaluationFraction);

        if (evaluationCount == 0)
        {
            evaluationCount = 1;
        }

        return Math.Min(evaluationCount, testCount);
    }

    private static void EnsureTrainingHasMultipleClasses(
        List<ModelInput> trainInputs,
        List<FeatureVector> trainFeatures,
        List<ModelInput> testInputs,
        List<FeatureVector> testFeatures)
    {
        var trainLabels = trainInputs.Select(i => i.Label).Distinct().ToHashSet();

        if (trainLabels.Count >= 2)
        {
            return;
        }

        var missingFromTraining = testInputs
            .Select((input, index) => (input, index))
            .Where(entry => !trainLabels.Contains(entry.input.Label))
            .ToList();

        if (missingFromTraining.Count == 0)
        {
            throw new InvalidOperationException(
                "Training data must contain at least two classes, but only one was found.");
        }

        foreach (var entry in missingFromTraining)
        {
            trainInputs.Add(entry.input);
            trainFeatures.Add(testFeatures[entry.index]);
        }

        foreach (var index in missingFromTraining.Select(e => e.index).OrderByDescending(i => i))
        {
            testInputs.RemoveAt(index);
            testFeatures.RemoveAt(index);
        }
    }
}
