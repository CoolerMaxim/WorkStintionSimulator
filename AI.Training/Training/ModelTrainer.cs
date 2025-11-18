using AI.Training.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.LightGbm;

namespace AI.Training.Training;

public record TrainingResult(ITransformer Model);

public class ModelTrainer
{
    private readonly MLContext _mlContext;

    public ModelTrainer(MLContext mlContext)
    {
        _mlContext = mlContext;
    }

public TrainingResult Train(IEnumerable<ModelInput> trainData, IEnumerable<ModelInput> validationData)
    {
        var trainView = _mlContext.Data.LoadFromEnumerable(trainData);
        var validationView = _mlContext.Data.LoadFromEnumerable(validationData);

        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(_mlContext.Transforms.NormalizeMeanVariance("Features"))
            .Append(_mlContext.MulticlassClassification.Trainers.LightGbm(new LightGbmMulticlassTrainer.Options
            {
                NumberOfLeaves = 63,
                LearningRate = 0.05,
                NumberOfIterations = 400,
                MinimumExampleCountPerLeaf = 10,
                LabelColumnName = "Label",
                FeatureColumnName = "Features"
            }))
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"))
            .AppendCacheCheckpoint(_mlContext);

        var model = pipeline.Fit(trainView);
        return new TrainingResult(model);
    }
}
