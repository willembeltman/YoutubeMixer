using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Diagnostics;
using YoutubeMixer.Library.Interfaces;
using YoutubeMixer.Library.Models;

namespace YoutubeMixer.Library.AudioSources
{
    public class YoutubeAudioSource : IAudioSource, IDisposable
    {
        public YoutubeAudioSource(IPitchBendController pitchBendController, ITimelineProcessor timelineProcessor)
        {
            PitchBendController = pitchBendController;
            TimelineProcessor = timelineProcessor;

            Driver = new ChromeDriver();
            JsExecutor = Driver;
            Thread = new Thread(new ThreadStart(StartDriver));
            Thread.Start();
        }

        private IPitchBendController PitchBendController { get; }
        private ITimelineProcessor TimelineProcessor { get; }
        private ChromeDriver Driver { get; }
        private IJavaScriptExecutor JsExecutor { get; }
        private Thread Thread { get; }
        private IWebElement? VideoElement { get; set; }
        private bool KillSwitch { get; set; }

        private bool _SetVolume { get; set; }
        private bool _SetEqualizer { get; set; }
        private bool _SetPlaybackSpeed { get; set; }

        private double _Volume { get; set; }
        private double _BassVolume { get; set; }
        private double _MidVolume { get; set; }
        private double _HighVolume { get; set; }
        private double _PlaybackSpeed { get; set; } = 1;

        public double Volume { get => _Volume; set { _Volume = value; _SetVolume = true; } }
        public double BassVolume { get => _BassVolume; set { _BassVolume = value; _SetEqualizer = true; } }
        public double MidVolume { get => _MidVolume; set { _MidVolume = value; _SetEqualizer = true; } }
        public double HighVolume { get => _HighVolume; set { _HighVolume = value; _SetEqualizer = true; } }
        public double PlaybackSpeed { get => _PlaybackSpeed; set { _PlaybackSpeed = value; _SetPlaybackSpeed = true; } }
        public bool PitchControl { get; set; }

        public string? Title { get; private set; }
        public double CurrentTime { get; private set; }
        public double TotalDuration { get; private set; }
        public double VuMeter { get; private set; }
        public bool KickDetected { get; private set; }
        public double PreviousTime { get; private set; }
        public bool Disposed { get; private set; }

        public bool IsPlaying { get; private set; }
        public bool IsReady { get; private set; }

        public void PlayPause()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Driver.Quit();
            Driver.Dispose();
            KillSwitch = true;
            // Als ik het niet zelf ben
            if (Thread.CurrentThread != Thread)
            {
                // Wachten op de thread
                Thread.Join();
            }
            Disposed = true;
        }

        private void StartDriver()
        {
            // Navigate browser to YouTube
            Driver.Navigate().GoToUrl("https://www.youtube.com");
            while (!KillSwitch)
            {
                var delay = ReadState();
                if (delay)
                    Thread.Sleep(10);
                else
                    Thread.Sleep(1);
            }
        }
        private bool ReadState()
        {
            try
            {
                // If the video element has not been loaded
                if (VideoElement == null)
                {
                    // Try to ge the video element
                    VideoElement = Driver.FindElement(By.CssSelector("video.html5-main-video"));

                    // Try to inject the equalizer and vu meter
                    TryInjectEqualizerAndVuMeter();
                }

                // If the video element has been found
                if (VideoElement != null)
                {
                    // If video is playing
                    IsReady = Convert.ToDouble(VideoElement.GetAttribute("currentTime")) > 0;
                    IsPlaying = VideoElement.GetAttribute("paused") != "true";
                    if (IsPlaying)
                    {
                        // Do playing operations
                        SyncVideoInformation();
                        SetPitchbendIfNeeded();
                        SetVolumeIfNeeded();
                        SetEqualizerIfNeeded();

                        // Ask for short delay
                        return true;
                    }
                    else
                    {
                        // Reset videoelement and try again
                        VideoElement = null;
                    }
                }
                else
                {
                    IsReady = false;
                }
            }
            catch (Exception ex)
            {
                // Error occured, write error to console
                Debug.WriteLine(ex.Message);

                // Reset videoelement and try again
                VideoElement = null;
            }

            // Ask for long delay
            return false;
        }

        private void TryInjectEqualizerAndVuMeter()
        {
            bool hasAttribute = (bool)JsExecutor.ExecuteScript("return arguments[0].hasAttribute('eq-injected')", VideoElement);
            if (!hasAttribute)
            {
                string filterCode = File.ReadAllText(Environment.CurrentDirectory + "/Library/AudioSources/YoutubeScript.js");

                JsExecutor.ExecuteScript(filterCode, VideoElement);

            }
        }

        private void SetEqualizerIfNeeded()
        {
            if (_SetEqualizer)
            {
                _SetEqualizer = false;
                double[] newGains = { _BassVolume, _BassVolume, _MidVolume, _MidVolume, _HighVolume, _HighVolume };
                JsExecutor.ExecuteScript("window.updateFilterParams(arguments[0]);", newGains);
            }
        }
        private void SetVolumeIfNeeded()
        {
            if (_SetVolume)
            {
                _SetVolume = false;
                JsExecutor.ExecuteScript("arguments[0].volume = arguments[1];", VideoElement, _Volume);
            }
        }
        private void SetPitchbendIfNeeded()
        {
            var pitchbendState = PitchBendController.GetPitchbendState();
            if (pitchbendState.IsDragging)
            {
                _SetPlaybackSpeed = true;

                int deltaY = pitchbendState.DeltaY;
                Debug.WriteLine($"DeltaY = {deltaY}");

                if (pitchbendState.DeltaY > 0)
                {
                    double speedChange = deltaY * 0.01; // adjust this constant to control the sensitivity of the control
                    double newSpeed = Math.Max(0.25, Math.Min(4.0, _PlaybackSpeed + speedChange)); // limit the speed to a reasonable range
                    JsExecutor.ExecuteScript($"arguments[0].playbackRate = {newSpeed.ToString("F3").Replace(",", ".")};", VideoElement);
                }
                else
                {
                    double speedChange = deltaY * 0.005; // adjust this constant to control the sensitivity of the control
                    double newSpeed = Math.Max(0.25, Math.Min(4.0, _PlaybackSpeed + speedChange)); // limit the speed to a reasonable range
                    JsExecutor.ExecuteScript($"arguments[0].playbackRate = {newSpeed.ToString("F3").Replace(",", ".")};", VideoElement);
                }
            }
            else if (_SetPlaybackSpeed)
            {
                JsExecutor.ExecuteScript($"arguments[0].playbackRate = {_PlaybackSpeed.ToString("F3").Replace(",", ".")};", VideoElement);
                _SetPlaybackSpeed = false;
            }
        }
        private void SyncVideoInformation()
        {
            Title = Driver?.Title ?? "";

            var state = (Dictionary<string, object>)JsExecutor
                .ExecuteScript(@"
return { 
    currentTime: arguments[0].currentTime, 
    totalDuration: arguments[0].duration,
    vuMeter: window.getVuMeter(), 
    kickTimestamps: window.getKickTimestamps(),
    kickDetected: window.getKickDetected(),
    timelineTimestamps: window.getTimelineTimestamps(),
    timelineKickVolumes: window.getTimelineKickVolumes(),
    timelineLows: window.getTimelineLows(),
    timelineMids: window.getTimelineMids(),
    timelineHighs: window.getTimelineHighs(),
    timelineVolumes: window.getTimelineVolumes(),
    timelineKickDetected: window.getTimelineKickDetected()    
};", VideoElement);

            CurrentTime = Convert.ToDouble(state["currentTime"]);
            TotalDuration = Convert.ToDouble(state["totalDuration"]);
            VuMeter = Convert.ToDouble(state["vuMeter"]);
            KickDetected = Convert.ToBoolean(state["kickDetected"]);

            var kickTimestamps = ((IEnumerable<object>)state["kickTimestamps"]).Select(Convert.ToDouble).ToArray();

            var timelineTimestamps = ((IEnumerable<object>)state["timelineTimestamps"]).Select(Convert.ToDouble).ToArray();
            var timelineKickVolumes = ((IEnumerable<object>)state["timelineKickVolumes"]).Select(Convert.ToDouble).ToArray();
            var timelineLows = ((IEnumerable<object>)state["timelineLows"]).Select(Convert.ToDouble).ToArray();
            var timelineMids = ((IEnumerable<object>)state["timelineMids"]).Select(Convert.ToDouble).ToArray();
            var timelineHighs = ((IEnumerable<object>)state["timelineHighs"]).Select(Convert.ToDouble).ToArray();
            var timelineVolumes = ((IEnumerable<object>)state["timelineVolumes"]).Select(Convert.ToDouble).ToArray();
            var timelineKickDetected = ((IEnumerable<object>)state["timelineKickDetected"]).Select(Convert.ToBoolean).ToArray();

            var lengths = new int[] 
            {
                timelineTimestamps.Length,
                timelineKickVolumes.Length,
                timelineLows.Length,
                timelineMids.Length,
                timelineHighs.Length,
                timelineVolumes.Length,
                timelineKickDetected.Length
            };

            if (lengths.GroupBy(a => a).Count() != 1)
                throw new Exception("Stop! communicatie uit sync");

            var timelineItemList = new List<TimeLineItem>();
            for ( int i = 0; i < timelineTimestamps.Length; i++ )
            {
                var timelineItem = new TimeLineItem()
                {
                    Timestamp = timelineTimestamps[i],
                    KickVolume = timelineKickVolumes[i],
                    LowVolume = timelineLows[i],
                    MidVolume = timelineMids[i],
                    HighVolume = timelineHighs[i],
                    Volume = timelineVolumes[i],
                    KickDetected = timelineKickDetected[i]
                };
                timelineItemList.Add( timelineItem );
            }
            var timelineItems = timelineItemList.ToArray();
            TimelineProcessor.ProcessTimelineData(CurrentTime, PreviousTime, timelineItems);
            PreviousTime = CurrentTime;
        }
    }
}
