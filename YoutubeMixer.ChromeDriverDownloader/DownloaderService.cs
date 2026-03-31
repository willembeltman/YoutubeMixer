using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Compression;
using YoutubeMixer.ChromeDriverDownloader.Models;

namespace YoutubeMixer.ChromeDriverDownloader;

public static class DownloaderService
{
    public static async Task<DownloadResult> DownloadTo(string driverPath, string driverVersionPath)
    {
        try
        {
            var currentVersion = GetChromeVersion();
            if (currentVersion == null)
                throw new Exception("Cannot find chrome on your C drive.");
            var currentDriverVersion = await GetCurrentDriverVersion(driverVersionPath);

            if (currentVersion == currentDriverVersion)
                return new DownloadResult()
                {
                    Succes = true,
                    AlreadyGotDriver = true
                };

            var knownDrivers = await DownloadKnownGoodVersions();
            var driver = knownDrivers.versions.FirstOrDefault(a => a.version == currentVersion);
            if (driver == null)
                throw new Exception($"Unknown chrome version is used: {currentVersion}"); 
            var driverForPlatform = driver.downloads.chromedriver.FirstOrDefault(a => a.platform == "win64");
            if (driverForPlatform == null)
                throw new Exception($"Unknown chrome version is used: {currentVersion}");
            var driverUrl = driverForPlatform.url;
            await DownloadChromeDriver(driverPath, driverUrl);
            await SetCurrentDriverVersion(driverVersionPath, currentVersion);
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

    public static string? GetChromeVersion()
    {
        var chromePaths = new[]
        {
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
        };

        foreach (var path in chromePaths)
        {
            if (File.Exists(path))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                return versionInfo.FileVersion;
            }
        }

        return null;
    }

    private static async Task<string?> GetCurrentDriverVersion(string driverVersionPath)
    {
        if (!File.Exists(driverVersionPath)) return null;

        using (var stream = File.OpenRead(driverVersionPath))
        using (var reader = new StreamReader(stream))
        {
            var version = await reader.ReadToEndAsync();
            return version;
        }
    }
    private static async Task SetCurrentDriverVersion(string driverVersionPath, string version)
    {
        using (var stream = File.OpenWrite(driverVersionPath))
        using (var writer = new StreamWriter(stream))
        {
            await writer.WriteAsync(version);
        }
    }

    private static async Task<string> DownloadUrlAsString(string url)
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
    private static async Task<LastKnownGoodDrivers> DownloadLastKnownGoodVersions()
    {
        var url = @"https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions-with-downloads.json";
        var jsonString = await DownloadUrlAsString(url);
        var errorMessage = "There was a problem downloading the new chrome driver: Cannot deserialize version json for chromedriver url.";

        LastKnownGoodDrivers? lastKnownDrivers;
        try
        {
            lastKnownDrivers = JsonConvert.DeserializeObject<LastKnownGoodDrivers>(jsonString);
        }
        catch (Exception ex)
        {
            throw new Exception(errorMessage, ex);
        }

        if (lastKnownDrivers == null)
            throw new Exception(errorMessage, new Exception(errorMessage));

        return lastKnownDrivers;
    }
    private static async Task<KnownGoodDrivers> DownloadKnownGoodVersions()
    {
        var url = @"https://googlechromelabs.github.io/chrome-for-testing/known-good-versions-with-downloads.json";
        var jsonString = await DownloadUrlAsString(url);
        var errorMessage = "There was a problem downloading the new chrome driver: Cannot deserialize version json for chromedriver url.";

        KnownGoodDrivers? knownGoodDrivers;
        try
        {
            knownGoodDrivers = JsonConvert.DeserializeObject<KnownGoodDrivers>(jsonString);
        }
        catch (Exception ex)
        {
            throw new Exception(errorMessage, ex);
        }

        if (knownGoodDrivers == null)
            throw new Exception(errorMessage, new Exception(errorMessage));

        return knownGoodDrivers;
    }

    private static string GetDriverUrlFrom(LastKnownGoodDrivers lastKnownDrivers)
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

    private static async Task DownloadChromeDriver(string driverPath, string driverUrl)
    {
        var errorMessage = $"There was a problem downloading the new chrome driver: Cannot download chromedriver from {driverUrl}.";

        try
        {
            using (HttpClient client = new HttpClient())
            using (var stream = await client.GetStreamAsync(driverUrl))
            {
                Unzip(driverPath, stream);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(errorMessage, ex);
        }
    }
    private static void Unzip(string driverPath, Stream stream)
    {
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
        {
            EnumerateZipArchive(driverPath, archive);
        }
    }
    private static void EnumerateZipArchive(string driverPath, ZipArchive archive)
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
}
