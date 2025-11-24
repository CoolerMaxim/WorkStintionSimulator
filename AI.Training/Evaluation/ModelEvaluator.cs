using AI.Training.Baseline;
using AI.Training.Features;
using AI.Training.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace AI.Training.Evaluation;

public record EvaluationReport(
    (double MacroF1, double CriticalF1, double FailedRecall) ModelMetrics,
    (double MacroF1, double CriticalF1, double FailedRecall) BaselineMetrics,
    IReadOnlyDictionary<HealthState, int> LabelDistribution);

public class ModelEvaluator
{
    private readonly MLContext _mlContext;
    private readonly BaselineRuleModel _baseline;

    private class PredictionRow
    {
        public uint PredictedLabel { get; set; }
        public uint Label { get; set; }
    }

    public ModelEvaluator(MLContext mlContext, BaselineRuleModel baseline)
    {
        _mlContext = mlContext;
        _baseline = baseline;
    }

    public EvaluationReport Evaluate(ITransformer model, IEnumerable<ModelInput> testData, IReadOnlyList<FeatureVector> rawFeatures)
    {
        var testView = _mlContext.Data.LoadFromEnumerable(testData);
        var predictions = model.Transform(testView);
        var modelMetrics = ComputeModelMetrics(predictions);
        var baselineMetrics = ComputeBaselineMetrics(rawFeatures);
        var distribution = rawFeatures
            .GroupBy(f => f.Label)
            .ToDictionary(g => g.Key, g => g.Count());

        return new EvaluationReport(
            modelMetrics,
            baselineMetrics,
            distribution);
    }

    private (double MacroF1, double CriticalF1, double FailedRecall) ComputeModelMetrics(IDataView predictions)
    {
        var confusion = new int[4, 4];
        var rows = _mlContext.Data.CreateEnumerable<PredictionRow>(predictions, reuseRowObject: false);
        foreach (var row in rows)
        {
            var actual = (HealthState)row.Label;
            var predicted = (HealthState)row.PredictedLabel;
            confusion[(int)actual, (int)predicted]++;
        }

        return ScoreConfusion(confusion);
    }

    private (double MacroF1, double CriticalF1, double FailedRecall) ComputeBaselineMetrics(IReadOnlyList<FeatureVector> features)
    {
        var confusion = new int[4, 4];
        foreach (var feature in features)
        {
            var predicted = _baseline.Predict(feature);
            confusion[(int)feature.Label, (int)predicted]++;
        }

        return ScoreConfusion(confusion);
    }

    private static (double MacroF1, double CriticalF1, double FailedRecall) ScoreConfusion(int[,] confusion)
    {
        double macroF1 = 0;
        for (var c = 0; c < 4; c++)
        {
            var tp = confusion[c, c];
            var precision = tp / (double)Math.Max(1, Enumerable.Range(0, 4).Sum(p => confusion[p, c]));
            var recall = tp / (double)Math.Max(1, Enumerable.Range(0, 4).Sum(p => confusion[c, p]));
            var f1 = (precision + recall) > 0 ? 2 * precision * recall / (precision + recall) : 0;
            macroF1 += f1;
        }

        macroF1 /= 4.0;
        var criticalF1 = ClassF1(confusion, (int)HealthState.Critical);
        var failedRecall = ClassRecall(confusion, (int)HealthState.Failed);
        return (macroF1, criticalF1, failedRecall);
    }

    private static double ClassF1(int[,] confusion, int idx)
    {
        var tp = confusion[idx, idx];
        var precision = tp / (double)Math.Max(1, Enumerable.Range(0, 4).Sum(p => confusion[p, idx]));
        var recall = tp / (double)Math.Max(1, Enumerable.Range(0, 4).Sum(p => confusion[idx, p]));
        return (precision + recall) > 0 ? 2 * precision * recall / (precision + recall) : 0;
    }

    private static double ClassRecall(int[,] confusion, int idx)
    {
        var tp = confusion[idx, idx];
        var positives = Enumerable.Range(0, 4).Sum(p => confusion[idx, p]);
        return tp / (double)Math.Max(1, positives);
    }
}
