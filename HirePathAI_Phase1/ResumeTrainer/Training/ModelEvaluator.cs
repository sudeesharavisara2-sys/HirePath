using Microsoft.ML;
using System;

namespace ResumeTrainer.Training
{
    public class ModelEvaluator
    {
        private readonly MLContext _mlContext;

        public ModelEvaluator(MLContext mlContext)
        {
            _mlContext = mlContext;
        }

        public void Evaluate(ITransformer model, IDataView testData)
        {
            try
            {
                var predictions = model.Transform(testData);
                var metrics = _mlContext.BinaryClassification.Evaluate(data: predictions, labelColumnName: "Label", scoreColumnName: "Score");

                Console.WriteLine($"Model Accuracy: {metrics.Accuracy:P2}");
                Console.WriteLine($"AUC: {metrics.AreaUnderRocCurve:P2}");
                Console.WriteLine($"F1 Score: {metrics.F1Score:P2}");
                Console.WriteLine($"Positive Precision: {metrics.PositivePrecision:P2}");
                Console.WriteLine($"Positive Recall: {metrics.PositiveRecall:P2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Evaluation failed: {ex.Message}");
                Console.WriteLine("This can happen if the test set doesn't contain both positive and negative samples.");
                Console.WriteLine("The model was still trained successfully.");
            }
        }
    }
}
