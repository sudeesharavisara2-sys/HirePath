using Microsoft.ML;
using HirePathAI.Infrastructure.AI.MLModels;
using Microsoft.Extensions.Logging;


namespace HirePathAI.Infrastructure.AI.Services
{
    public class ResumeService
    {
        private readonly MLContext _mlContext;
        private readonly PredictionEngine<ResumeData, ResumePrediction> _predictionEngine;
        private readonly ILogger<ResumeService> _logger;

        public ResumeService(ILogger<ResumeService> logger)
        {
            _logger = logger;
            _mlContext = new MLContext(seed: 0);

            string modelPath = Path.Combine(AppContext.BaseDirectory, "model.zip");
            ITransformer loadedModel = _mlContext.Model.Load(modelPath, out var modelInputSchema);

            _predictionEngine = _mlContext.Model.CreatePredictionEngine<ResumeData, ResumePrediction>(loadedModel);
        }

        public ResumePrediction Predict(string resumeText)
        {
            try
            {
                var inputData = new ResumeData
                {
                    ResumeText = resumeText
                };

                var prediction = _predictionEngine.Predict(inputData);

                var decision = GetDecisionLevel(prediction.Probability, resumeText);

                prediction.Selected = decision == "Selected";
                prediction.Decision = decision;

                _logger.LogInformation("Resume analysis completed: {Decision}, Confidence: {Probability:P2}",
                    decision, prediction.Probability);

                return prediction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during resume prediction");
                throw new InvalidOperationException("Failed to analyze resume", ex);
            }
        }

        private string GetDecisionLevel(float probability, string resumeText)
        {
            var fresherKeywords = new[] { "fresher", "internship", "graduate", "entry level", "entry-level" };
            var lowerText = resumeText.ToLower();
            var isFresher = fresherKeywords.Any(keyword => lowerText.Contains(keyword));

            if (isFresher && probability < 0.50f)
            {
                return "Consider";
            }

            if (probability > 0.70f)
            {
                return "Selected";
            }
            else if (probability >= 0.40f)
            {
                return "Consider";
            }
            else
            {
                return "Rejected";
            }
        }
    }
}
