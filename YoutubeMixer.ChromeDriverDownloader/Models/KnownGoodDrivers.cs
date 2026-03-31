namespace YoutubeMixer.ChromeDriverDownloader.Models;

public class KnownGoodDrivers
{
    public DateTime timestamp { get; set; }
    public List<Version> versions { get; set; } = [];
}
