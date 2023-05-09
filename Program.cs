using System.Text.Json;
using DeepAI;

namespace GlamLahToolkit;
class Program
{
    private const string ScaleApiKey = "<api-key-goes-here>";
    private const string BGColorApiKey = "<api-key-goes-here>";

    static async Task Main(string[] args)
    {
        string scaleOption = "--scale";
        string clearbgOption = "--removebg";
        string intputFile = "--input";
        string outputFile = "--output";

        if (args.Length < 4)
        {
            Console.WriteLine("Usage: dotnet run -- [OPTIONS] [ARGUMENTS] [<value1> <value2>");
            Console.WriteLine("Options:");
            Console.WriteLine($"  {scaleOption,-18} Enable scaling");
            Console.WriteLine($"  {clearbgOption,-18} Enable background clearing");
            Console.WriteLine("Arguments:");
            Console.WriteLine($"  {intputFile,-18} <value1> filepath to process");
            Console.WriteLine($"  {outputFile,-18} <value2> path to write to file");
            return;
        }

        bool enableScaling = args.Contains(scaleOption);
        bool enableClearBg = args.Contains(clearbgOption);

        string? inputValue = null;
        string? outputValue = null;

        // assign the input file and output file
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == intputFile && i + 1 < args.Length)
            {
                inputValue = args[i + 1];
            }

            if (args[i] == outputFile && i + 1 < args.Length)
            {
                outputValue = args[i + 1];
            }
        }

        // set filename placeholders for use later
#pragma warning disable CS8600
        string directoryPath = Path.GetDirectoryName(inputValue);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputValue);
        string extension = Path.GetExtension(inputValue);
#pragma warning restore CS8600

        // scale the image
        if (enableScaling)
        {
#pragma warning disable CS8604
            string scaledFilepath = Path.Combine(directoryPath, $"{fileNameWithoutExtension}_scaled{extension}");
#pragma warning restore CS8604

            DeepAI_API api = new DeepAI_API(apiKey: ScaleApiKey);

            StandardApiResponse resp = api.callStandardApi("torch-srgan", new
            {
#pragma warning disable CS8604
                image = File.OpenRead(inputValue),
#pragma warning restore CS8604
            });

            JsonDocument jsonDoc = JsonDocument.Parse(api.objectAsJsonString(resp));
            JsonElement root = jsonDoc.RootElement;
#pragma warning disable CS8600
            string url = root.GetProperty("output_url").GetString();
#pragma warning restore CS8600

            //Console.WriteLine(DateTime.Now + " -> " + url);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    using (HttpResponseMessage response = await client.GetAsync(url))
                    {
                        response.EnsureSuccessStatusCode();
                        using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                        {
                            using FileStream fileStream = new FileStream(scaledFilepath, FileMode.Create, FileAccess.Write, FileShare.None);
                            await contentStream.CopyToAsync(fileStream);
                        }
                    }
                    Console.WriteLine($"File scaled successfully -> {scaledFilepath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error occurred while scaling file: " + ex.Message);
                    Environment.Exit(2);
                }
            }
        }

        // remove background color
        if (enableClearBg)
        {
            var apiUrl = "https://clipdrop-api.co/remove-background/v1";
#pragma warning disable CS8604
            string clearedbgFilepath = Path.Combine(directoryPath, $"{fileNameWithoutExtension}_clearedbg{extension}");
#pragma warning restore CS8604

            // Set up the HTTP client
            var httpClient = new HttpClient();

            // Set the request headers
            httpClient.DefaultRequestHeaders.Add("x-api-key", BGColorApiKey);

            // Set up the HTTP request message
            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);

            // Set the request content
            var content = new MultipartFormDataContent();
#pragma warning disable CS8604
            var fileContent = new ByteArrayContent(File.ReadAllBytes(inputValue));
#pragma warning restore CS8604
            content.Add(fileContent, "image_file", Path.GetFileName(inputValue));
            request.Content = content;

            // Send the HTTP request and get the response
            var response = await httpClient.SendAsync(request);

            // Read the response content as a byte array
            var responseBytes = await response.Content.ReadAsByteArrayAsync();

            // Save the output image to a file
            File.WriteAllBytes(clearedbgFilepath, responseBytes);

            Console.WriteLine($"File removeBG successfully -> {clearedbgFilepath}");
        }
    }
}

