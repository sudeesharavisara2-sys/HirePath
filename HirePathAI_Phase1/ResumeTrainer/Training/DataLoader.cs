using Microsoft.ML;

namespace ResumeTrainer.Training
{
    public class DataLoader
    {
        private readonly MLContext _mlContext;

        public DataLoader(MLContext mlContext)
        {
            _mlContext = mlContext;
        }

        public (IDataView TrainData, IDataView TestData) LoadData(string dataPath)
        {
            IDataView dataView = _mlContext.Data.LoadFromTextFile<ResumeData>(
                path: dataPath,
                hasHeader: true,
                separatorChar: ',');

            var dataSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2, seed: 123);
            return (dataSplit.TrainSet, dataSplit.TestSet);
        }
    }
}
