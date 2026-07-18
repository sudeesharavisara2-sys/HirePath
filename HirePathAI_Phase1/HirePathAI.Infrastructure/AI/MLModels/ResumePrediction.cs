using Microsoft.ML.Data;

namespace HirePathAI.Infrastructure.AI.MLModels
{
    public class ResumePrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Selected { get; set; }

        public float Probability { get; set; }
        public float Score { get; set; }
        public string Decision { get; set; } = string.Empty;
    }
}
