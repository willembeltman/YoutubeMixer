using Newtonsoft.Json;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using YoutubeMixer.ChromeDriverDownloader.Models;

namespace YoutubeMixer.ChromeDriverDownloader
{
    public static class DownloaderService
    {
        public static async Task<DownloadResult> DownloadTo(string driverPath, string driverVersionPath)
        {
            var url = @"https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions-with-downloads.json";

            try
            {
                var jsonString = await GetJsonSting(url);
                var lastKnownDrivers = ConvertToLastKnownDrivers(jsonString);
                var downloadRevision = GetRevisionFrom(lastKnownDrivers);
                var currentRevision = GetCurrentRevision(driverVersionPath);
                if (currentRevision < downloadRevision)
                {
                    var driverUrl = GetDriverUrlFrom(lastKnownDrivers);
                    await DownloadChromeDriver(driverPath, url, driverUrl);
                    SetCurrentRevision(driverVersionPath, downloadRevision);
                }
            }
            catch (Exception ex)
            {
                return new DownloadResult()
                {
                    ErrorMessage = ex.Message,
                    Exception = ex.InnerException!,
                };
            }

            return new DownloadResult()
            {
                Succes = true
            };
        }


        private static long GetCurrentRevision(string driverVersionPath)
        {
            if (!File.Exists(driverVersionPath)) return -1;

            using (var stream = File.OpenRead(driverVersionPath))
            using (var reader = new StreamReader(stream))
            {
                var versionString = reader.ReadToEnd();
                var version = Convert.ToInt64(versionString);
                return version;
            }
        }
        private static void SetCurrentRevision(string driverVersionPath, long revision)
        {
            using (var stream = File.OpenWrite(driverVersionPath))
            using (var reader = new StreamWriter(stream))
            {
                var versionString = revision.ToString();
                reader.Write(versionString);
            }
        }

        private static async Task<string> GetJsonSting(string url)
        {
            var errorMessage = "There was a problem downloading the new chrome driver: Cannot download version json for chromedriver url.";

            string jsonString;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    jsonString = await client.GetStringAsync(url);
                }
                catch (Exception ex)
                {
                    throw new Exception(errorMessage, ex);
                }
            }

            if (string.IsNullOrEmpty(jsonString))
                throw new Exception(errorMessage, new Exception(errorMessage));

            return jsonString;
        }
        private static LastKnownDrivers ConvertToLastKnownDrivers(string jsonString)
        {
            var errorMessage = "There was a problem downloading the new chrome driver: Cannot deserialize version json for chromedriver url.";

            LastKnownDrivers? lastKnownDrivers;
            try
            {
                lastKnownDrivers = JsonConvert.DeserializeObject<LastKnownDrivers>(jsonString);
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }

            if (lastKnownDrivers == null)
                throw new Exception(errorMessage, new Exception(errorMessage));

            return lastKnownDrivers;
        }
        private static string GetDriverUrlFrom(LastKnownDrivers lastKnownDrivers)
        {
            var errorMessage = "There was a problem downloading the new chrome driver: Cannot extract chromedriver url from json.";

            string driverurl;
            try
            {
                driverurl = lastKnownDrivers!.channels.Stable.downloads.chromedriver.First(a => a.platform == "win64").url;
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }

            if (string.IsNullOrEmpty(driverurl))
                throw new Exception(errorMessage, new Exception(errorMessage));

            return driverurl;
        }
        private static long GetRevisionFrom(LastKnownDrivers lastKnownDrivers)
        {
            var errorMessage = "There was a problem downloading the new chrome driver: Cannot extract chromedriver url from json.";

            long revision;
            try
            {
                revision = lastKnownDrivers!.channels.Stable.revision;
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }

            return revision;
        }
        private static async Task DownloadChromeDriver(string driverPath, string url, string driverUrl)
        {
            var errorMessage = $"There was a problem downloading the new chrome driver: Cannot download chromedriver from {url}.";

            try
            {
                using (HttpClient client = new HttpClient())
                using (var stream = await client.GetStreamAsync(driverUrl))
                {
                    Unzip(driverPath, url, stream);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }
        }
        private static void Unzip(string driverPath, string url, Stream stream)
        {
            var errorMessage = $"There was a problem downloading the new chrome driver: Cannot open downloaded ziparchive from {url}.";
            try
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    EnumerateZipArchive(driverPath, url, archive);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }
        }
        private static void EnumerateZipArchive(string driverPath, string url, ZipArchive archive)
        {
            var errorMessage = $"There was a problem downloading the new chrome driver: Error while reading ziparchive from {url}.";

            try
            {
                // Loop door elk item in het ZIP-archief
                foreach (var entry in archive.Entries)
                {
                    if (entry.Name.EndsWith("chromedriver.exe"))
                    {
                        if (File.Exists(driverPath)) { File.Delete(driverPath); }

                        // Open de stream van het bestand in het ZIP-archief
                        using (Stream entryStream = entry.Open())
                        using (Stream outputStream = File.OpenWrite(driverPath))
                        {
                            entryStream.CopyTo(outputStream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }
        }


    }
}
