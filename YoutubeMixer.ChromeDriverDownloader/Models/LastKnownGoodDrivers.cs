namespace YoutubeMixer.ChromeDriverDownloader.Models;

public class LastKnownGoodDrivers
{
    public DateTime timestamp { get; set; }
    public Channels channels { get; set; } = new();
}
