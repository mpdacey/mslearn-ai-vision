using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

// Import namespaces
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace analyze_faces
{
    class Program
    {

        private static FaceClient faceClient;
        static async Task Main(string[] args)
        {
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string cogSvcEndpoint = configuration["AIServicesEndpoint"];
                string cogSvcKey = configuration["AIServiceKey"];

                // Authenticate Face client
                ApiKeyServiceClientCredentials credentials = new(cogSvcKey);
                faceClient = new(credentials)
                {
                    Endpoint = cogSvcEndpoint
                };

                // Menu for face functions
                Console.WriteLine("1: Detect faces\nAny other key to quit");
                Console.WriteLine("Enter a number:");
                string command = Console.ReadLine();
                switch (command)
                {
                    case "1":
                        await DetectFaces("images/people.jpg");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async Task DetectFaces(string imageFile)
        {
            Console.WriteLine($"Detecting faces in {imageFile}");

            // Specify facial features to be retrieved
            IList<FaceAttributeType> features = new FaceAttributeType[]
            {
                FaceAttributeType.Occlusion,
                FaceAttributeType.Blur,
                FaceAttributeType.Glasses
            };

            // Get faces
            using (var imageData = File.OpenRead(imageFile))
            {
                var detectFaces = await faceClient.Face.DetectWithStreamAsync(imageData, returnFaceAttributes: features, returnFaceId: false);

                if(detectFaces.Count() <= 0) return;

                Console.WriteLine($"{detectFaces.Count()} faces detected.");

                // Prepare image for drawing
                Image image = Image.FromFile(imageFile);
                Graphics graphics = Graphics.FromImage(image);
                Pen pen = new(Color.LightGreen, 3);
                Font font = new("Arial", 4);
                SolidBrush brush = new(Color.White);
                int faceCount = 0;

                foreach (var face in detectFaces)
                {
                    faceCount++;
                    Console.WriteLine($"\nFace number {faceCount}");

                    // Get face properties
                    Console.WriteLine($" - Mouth Occluded: {face.FaceAttributes.Occlusion.MouthOccluded}");
                    Console.WriteLine($" - Eye Occluded: {face.FaceAttributes.Occlusion.EyeOccluded}");
                    Console.WriteLine($" - Blur: {face.FaceAttributes.Blur.BlurLevel}");
                    Console.WriteLine($" - Glasses: {face.FaceAttributes.Glasses}");

                    var box = face.FaceRectangle;
                    Rectangle rect = new(box.Left,box.Top,box.Width,box.Height);
                    graphics.DrawRectangle(pen, rect);
                    string annotation = $"Face number {faceCount}";
                    graphics.DrawString(annotation,font,brush,box.Left,box.Top);
                }

                // Save annotated image
                string outputFile = "detected_faces.jpg";
                image.Save(outputFile);
                Console.WriteLine($" Results saved in {outputFile}");
            }
 
        }
    }
}
