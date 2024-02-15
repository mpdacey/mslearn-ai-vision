using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Drawing;
using Microsoft.Extensions.Configuration;
using Azure;

// Import namespaces
using Azure.AI.Vision.ImageAnalysis;

namespace image_analysis
{
    class Program
    {

        static async Task Main(string[] args)
        {
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string aiSvcEndpoint = configuration["AIServicesEndpoint"];
                string aiSvcKey = configuration["AIServicesKey"];

                // Get image
                string imageFile = "images/street.jpg";
                if (args.Length > 0)
                {
                    imageFile = args[0];
                }

                // Authenticate Azure AI Vision client
                ImageAnalysisClient client = new(new Uri(aiSvcEndpoint),new AzureKeyCredential(aiSvcKey));
                
                // Analyze image
                AnalyzeImage(imageFile, client);

                // Remove the background or generate a foreground matte from the image
                await BackgroundForeground(imageFile, aiSvcEndpoint, aiSvcKey);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void AnalyzeImage(string imageFile, ImageAnalysisClient client)
        {
            Console.WriteLine($"\nAnalyzing {imageFile} \n");

            // Use a file stream to pass the image data to the analyze call
            using FileStream stream = new FileStream(imageFile, FileMode.Open);

            // Get result with specified features to be retrieved
            ImageAnalysisResult result = client.Analyze(
                BinaryData.FromStream(stream),
                VisualFeatures.Caption |
                VisualFeatures.DenseCaptions |
                VisualFeatures.Objects |
                VisualFeatures.Tags |
                VisualFeatures.People);
            
            // Display analysis results
            // Get image captions
            if(result.Caption.Text != null)
            {
                Console.WriteLine(" Caption:");
                Console.WriteLine($"    \"{result.Caption.Text}\", Confidence {result.Caption.Confidence:0.00}\n");
            }

            // Get image dense captions
            Console.WriteLine(" Dense Captions:");
            foreach (DenseCaption denseCaption in result.DenseCaptions.Values)
                Console.WriteLine($"    Caption: '{denseCaption.Text}', Confidence: {denseCaption.Confidence:0.00}");

            // Get image tags
            if(result.Tags.Values.Count > 0)
            {
                Console.WriteLine($"\n Tags:");
                foreach(DetectedTag tag in result.Tags.Values)
                    Console.WriteLine($"    '{tag.Name}', Confidence: {tag.Confidence:F2}");
            }

            // Get objects in the image
            if(result.Objects.Values.Count > 0)
            {
                Console.WriteLine(" Objects:");

                // Prepare image for drawing
                stream.Close();
                Image image = Image.FromFile(imageFile);
                Graphics graphics = Graphics.FromImage(image);
                Pen pen = new(Color.MediumVioletRed, 3);
                Font font = new("Impact", 14);
                SolidBrush brush = new(Color.MediumVioletRed);

                foreach (DetectedObject detectedObject in result.Objects.Values)
                {
                    Console.WriteLine($"    \"{detectedObject.Tags[0].Name}\"");

                    // Draw object bounding box
                    var box = detectedObject.BoundingBox;
                    Rectangle rect = new(box.X,box.Y,box.Width,box.Height);
                    graphics.DrawRectangle(pen, rect);
                    graphics.DrawString(detectedObject.Tags[0].Name, font, brush, box.X, box.Y);
                }

                // Save annotated image
                string output_file = "object.jpg";
                image.Save(output_file);
                Console.WriteLine($"  Results saved in {output_file}\n");
            }
        }
        static async Task BackgroundForeground(string imageFile, string endpoint, string key)
        {
            // Remove the background from the image or generate a foreground matte
            
        }
    }
}
