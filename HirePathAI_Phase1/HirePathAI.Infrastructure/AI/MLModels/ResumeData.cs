using Microsoft.ML.Data;

namespace HirePathAI.Infrastructure.AI.MLModels
{
    public class ResumeData
    {
        [LoadColumn(0)]
        public string ResumeText { get; set; }

        [LoadColumn(1), ColumnName("Label")]
        public bool Selected { get; set; }
    }
}
