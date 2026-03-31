using Newtonsoft.Json;

namespace YoutubeMixer.ChromeDriverDownloader.Models;

public class Downloads
{
    public List<Chrome> chrome { get; set; } = [];
    public List<ChromeDriver> chromedriver { get; set; } = [];

    [JsonProperty("chrome-headless-shell")]
    public List<ChromeHeadlessShell>? chromeheadlessshell { get; set; }
}
