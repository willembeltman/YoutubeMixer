namespace YoutubeMixer.ChromeDriverDownloader.Models
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Beta
    {
        public string channel { get; set; }
        public string version { get; set; }
        public string revision { get; set; }
        public Downloads downloads { get; set; }
    }


}
