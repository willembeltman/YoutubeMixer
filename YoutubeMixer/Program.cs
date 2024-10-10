using YoutubeMixer.ChromeDriverDownloader;
using YoutubeMixer.Forms;

namespace YoutubeMixer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var driverPath = Path.Combine(Environment.CurrentDirectory, "chromedriver.exe");
            var driverVersionPath = Path.Combine(Environment.CurrentDirectory, "chromedriverversion.json");
            var res = DownloaderService.DownloadTo(driverPath, driverVersionPath).Result;

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new TwoDeckForm(driverPath));
        }
    }
}