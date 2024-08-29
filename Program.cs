using System.Diagnostics;
using OpenQA.Selenium.Chrome;

class Program
{
    static async Task Main(string[] args)
    {
        string baseDirectory = Environment.CurrentDirectory;
        MemoryLogger logger = new();

        Timer memoryTimer = new((e) =>
        {
            logger.LogMemoryUsage();
        }, null, 0, 100);
        try
        {
            var tasks = new List<Task>();


            for (int i = 1; i <= 50; i++)
            {
                int fileIndex = i;
                tasks.Add(Task.Run(() => ConvertHtmlToPdf(baseDirectory, fileIndex)));
            }


            await Task.WhenAll(tasks);
            Console.WriteLine("Done.");
        }

        finally
        {
            memoryTimer?.Dispose();
        }
    }

    static void ConvertHtmlToPdf(string baseDirectory, int fileIndex)
    {
        var chromeDriverService = ChromeDriverService.CreateDefaultService();
        chromeDriverService.HideCommandPromptWindow = true;

        var chromeOptions = new ChromeOptions();
        chromeOptions.AddArgument("--headless");
        chromeOptions.AddArgument("--disable-gpu");
        chromeOptions.AddArgument("--no-sandbox");
        chromeOptions.AddArgument("--disable-dev-shm-usage");

        string localHtmlFilePath = Path.Combine(baseDirectory, $"dummy1.html");
        string fileUrl = new Uri(localHtmlFilePath).AbsoluteUri;

        using var driver = new ChromeDriver(chromeDriverService, chromeOptions);
        try
        {
            driver.Navigate().GoToUrl(fileUrl);
            var printOptions = new Dictionary<string, object>
                {
                    { "paperWidth", 8.5 },
                    { "paperHeight", 11 },
                    { "printBackground", true }
                };

            var result = (Dictionary<string, object>)driver.ExecuteCdpCommand("Page.printToPDF", printOptions);
            byte[] pdfData = Convert.FromBase64String(result["data"].ToString()!);

            string outputPdfPath = Path.Combine(baseDirectory, $"data/output{fileIndex}.pdf");

            Console.WriteLine(outputPdfPath);

            File.WriteAllBytes(outputPdfPath, pdfData);

            Console.WriteLine("PDF saved at: " + outputPdfPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting dummy{fileIndex}.html to PDF: {ex.Message}");
        }
    }
}


class MemoryLogger
{
    private long HighestMemoryUsed { get; set; } = 0;

    public void LogMemoryUsage()
    {


        try
        {
            long totalMemoryUsed = 0;
            Process[] chromeProcesses = Process.GetProcessesByName("chrome");

            foreach (var chromeProcess in chromeProcesses)
            {
                if (!chromeProcess.HasExited)
                {
                    totalMemoryUsed += chromeProcess.WorkingSet64;
                }
            }

            long memoryUsedInMB = totalMemoryUsed / 1024 / 1024;

            HighestMemoryUsed = Math.Max(HighestMemoryUsed, memoryUsedInMB);

            Console.WriteLine($"Chrome RAM Usage: {memoryUsedInMB} MB Peak: {HighestMemoryUsed} MB");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error logging memory usage: {ex.Message}");
        }
    }
}