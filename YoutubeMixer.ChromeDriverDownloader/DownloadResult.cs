
namespace YoutubeMixer.ChromeDriverDownloader
{
    public class DownloadResult
    {
        public bool Succes { get; internal set; }
        public string ErrorMessage { get; internal set; }
        public Exception Exception { get; internal set; }
    }
}