namespace YoutubeMixer.ChromeDriverDownloader.Models;

public class Version
{
    public string version { get; set; } = string.Empty;
    public string revision { get; set; } = string.Empty;
    public Downloads downloads { get; set; } = new();
}