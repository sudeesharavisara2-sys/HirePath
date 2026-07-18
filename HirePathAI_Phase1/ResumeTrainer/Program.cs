using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.IO;
using ResumeTrainer.Training;

namespace ResumeTrainer
{
    public class ResumeData
    {
        [LoadColumn(0)]
        public string ResumeText { get; set; } = string.Empty;

        [LoadColumn(1), ColumnName("Label")]
        public bool Selected { get; set; }
    }

    public class ResumePrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Selected { get; set; }

        public float Probability { get; set; }
        public float Score { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var mlContext = new MLContext(seed: 0);

            Console.WriteLine("=== ML.NET Resume Screening System - Improved Model Training ===");
            Console.WriteLine("Using improved balanced dataset with realistic resume examples...\n");

            var dataPath = Path.Combine("Data", "active-training-data.csv");
            var modelPath = Path.Combine("Models", "model.zip");

            var dataLoader = new DataLoader(mlContext);
            var trainer = new ModelTrainer(mlContext);
            var evaluator = new ModelEvaluator(mlContext);

            // Load fixed data from CSV
            var (trainData, testData) = dataLoader.LoadData(dataPath);

            Console.WriteLine($"Training samples: {trainData.GetRowCount()}");
            Console.WriteLine($"Test samples: {testData.GetRowCount()}");

            // Train the model
            Console.WriteLine("\nTraining the model with enhanced pipeline...");
            var model = trainer.TrainModel(trainData);
            Console.WriteLine("Model training completed!");

            // Evaluate the model on test data with error handling
            Console.WriteLine("\nEvaluating model performance...");
            evaluator.Evaluate(model, testData);

            // Save the model
            mlContext.Model.Save(model, trainData.Schema, modelPath);
            Console.WriteLine($"\nImproved model saved as {modelPath}");

            // Create prediction engine
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ResumeData, ResumePrediction>(model);

            // Test with comprehensive test cases
            Console.WriteLine("\n=== COMPREHENSIVE TEST CASES ===");

            var testCases = new[]
            {
                // Strong candidates (should be Selected)
                new { Input = "Senior software engineer with 5 years experience in C# .NET Core and SQL Server. Led development of enterprise applications using ASP.NET MVC and Web API.", Expected = true, Category = "Strong" },
                new { Input = "Full-stack developer with 3 years experience in React Node.js and MongoDB. Built scalable web applications and RESTful APIs.", Expected = true, Category = "Strong" },
                new { Input = "Java developer with 4 years experience in Spring Boot and microservices. Developed high-performance backend systems.", Expected = true, Category = "Strong" },
                
                // Medium candidates (could go either way)
                new { Input = "Software engineer with 2 years experience in .NET development. Built web applications using ASP.NET Core and Entity Framework.", Expected = true, Category = "Medium" },
                new { Input = "Recent computer science graduate with 6-month internship in web development. Built responsive websites using HTML CSS JavaScript and React.", Expected = true, Category = "Medium" },
                new { Input = "Fresher with certification in full-stack web development. Completed 6-month bootcamp covering React Node.js and databases. Built 3 portfolio projects.", Expected = true, Category = "Medium" },
                new { Input = "Recent graduate with 3-month internship as junior developer. Assisted in developing e-commerce website using PHP and MySQL.", Expected = true, Category = "Medium" },
                
                // Weak candidates (should be Rejected)
                new { Input = "Fresher with basic computer knowledge and interest in programming. No professional experience or technical skills.", Expected = false, Category = "Weak" },
                new { Input = "Recent graduate with no programming experience but good communication skills. Willing to learn technical concepts.", Expected = false, Category = "Weak" },
                new { Input = "High school graduate with basic computer literacy. Looking for opportunity to learn programming.", Expected = false, Category = "Weak" },
                new { Input = "No programming experience or technical background. Seeking entry-level position in IT.", Expected = false, Category = "Weak" }
            };

            int correctPredictions = 0;
            int totalPredictions = testCases.Length;

            for (int i = 0; i < testCases.Length; i++)
            {
                var testCase = testCases[i];
                var resumeData = new ResumeData { ResumeText = testCase.Input };
                var prediction = predictionEngine.Predict(resumeData);

                var isCorrect = prediction.Selected == testCase.Expected;
                if (isCorrect) correctPredictions++;

                Console.WriteLine($"\nTest Case {i + 1} ({testCase.Category}):");
                Console.WriteLine($"Input: \"{testCase.Input}\"");
                Console.WriteLine($"Expected: {(testCase.Expected ? "Selected" : "Rejected")}");
                Console.WriteLine($"Predicted: {(prediction.Selected ? "Selected" : "Rejected")} {(isCorrect ? "✓" : "✗")}");
                Console.WriteLine($"Confidence: {prediction.Probability:P1}");
                Console.WriteLine($"Score: {prediction.Score:F3}");
            }

            Console.WriteLine($"\n=== TEST RESULTS ===");
            Console.WriteLine($"Correct Predictions: {correctPredictions}/{totalPredictions}");
            Console.WriteLine($"Test Accuracy: {(double)correctPredictions / totalPredictions:P2}");

            // Test specific real-world scenarios
            Console.WriteLine("\n=== REAL-WORLD SCENARIO TESTS ===");
            
            var realWorldTests = new[]
            {
                "I have 3 years experience in C# and SQL",
                "Internship experience in web development with React",
                "Worked on ASP.NET MVC and Web API for 2 years",
                "No programming experience but eager to learn",
                "React and Node.js developer with 1 year experience",
                "Recent computer science graduate with strong academic projects",
                "Self-taught developer with personal projects in GitHub"
            };

            for (int i = 0; i < realWorldTests.Length; i++)
            {
                var resumeData = new ResumeData { ResumeText = realWorldTests[i] };
                var prediction = predictionEngine.Predict(resumeData);

                Console.WriteLine($"\nReal Test {i + 1}:");
                Console.WriteLine($"Input: \"{realWorldTests[i]}\"");
                Console.WriteLine($"Output: {(prediction.Selected ? "Selected" : "Rejected")} {(prediction.Selected ? "✔" : "❌")}");
                Console.WriteLine($"Confidence: {prediction.Probability:P1}");
            }

            Console.WriteLine("\n=== TRAINING COMPLETED SUCCESSFULLY! ===");
            Console.WriteLine("The improved model should now provide more realistic predictions.");
        }
    }
}
