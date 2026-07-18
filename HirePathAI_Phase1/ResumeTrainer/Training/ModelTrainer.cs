using Microsoft.ML;

namespace ResumeTrainer.Training
{
    public class ModelTrainer
    {
        private readonly MLContext _mlContext;

        public ModelTrainer(MLContext mlContext)
        {
            _mlContext = mlContext;
        }

        public ITransformer TrainModel(IDataView trainData)
        {
            var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(ResumeData.ResumeText))
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                    labelColumnName: "Label",
                    featureColumnName: "Features"));

            return pipeline.Fit(trainData);
        }
    }
}
