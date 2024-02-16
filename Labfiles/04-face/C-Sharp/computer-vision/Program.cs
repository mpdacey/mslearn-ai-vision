using System;
using System.Drawing;
using Microsoft.Extensions.Configuration;
using Azure;
using System.IO;

// Import namespaces
using Azure.AI.Vision.Common;
using Azure.AI.Vision.ImageAnalysis;

namespace detect_people
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string aiSvcEndpoint = configuration["AIServicesEndpoint"];
                string aiSvcKey = configuration["AIServiceKey"];

                // Get image
                string imageFile = "images/people.jpg";
                if (args.Length > 0)
                {
                    imageFile = args[0];
                }

                // Authenticate Azure AI Vision client
                VisionServiceOptions cvClient = new(aiSvcEndpoint, new AzureKeyCredential(aiSvcKey));
                
                // Analyze image
                AnalyzeImage(imageFile, cvClient);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void AnalyzeImage(string imageFile, VisionServiceOptions serviceOptions)
        {
            Console.WriteLine($"\nAnalyzing {imageFile} \n");

            var analysisOptions = new ImageAnalysisOptions()
            {
                // Specify features to be retrieved
                Features = ImageAnalysisFeature.People
            };

            // Get image analysis
            using var imageSource = VisionSource.FromFile(imageFile);
            using var analyzer = new ImageAnalyzer(serviceOptions,imageSource,analysisOptions);
            var result = analyzer.Analyze();

            if (result.Reason == ImageAnalysisResultReason.Analyzed)
            {
                if(result.People == null) return;

                Console.WriteLine(" People:");

                // Prepare image for drawing
                System.Drawing.Image image = System.Drawing.Image.FromFile(imageFile);
                Graphics graphics = Graphics.FromImage(image);
                Pen pen = new(Color.Cyan, 3);
                Font font = new("Ariel", 16);
                SolidBrush brush = new(Color.Cyan);

                foreach (var person in result.People)
                {
                    // Draw object bounding box if confidence > 50%
                    if (person.Confidence > 0.5f)
                    {
                        // Draw object bounding box
                        var box = person.BoundingBox;
                        Rectangle rect = new(box.X,box.Y,box.Width,box.Height);
                        graphics.DrawRectangle(pen,rect);

                        // Return the confidence of the person detected
                        Console.WriteLine($"    Bounding box {person.BoundingBox}, Confidence {person.Confidence:0.0000}");
                    }
                }

                // Save annotated image
                string outputFile = "detected_people.jpg";
                image.Save(outputFile);
                Console.WriteLine($"  Results saved in {outputFile}\n");
            }
            else
            {
                var errorDetails = ImageAnalysisErrorDetails.FromResult(result);
                Console.WriteLine(" Analysis failed.");
                Console.WriteLine($"   Error reason: {errorDetails.Reason}");
                Console.WriteLine($"   Error code: {errorDetails.ErrorCode}");
                Console.WriteLine($"   Error message: {errorDetails.Message}");
            }
        }


    }
}
