var context = new AudioContext();
var source = context.createMediaElementSource(arguments[0]);


// create an equalizer using BiquadFilterNodes
var filters = [];
var frequencies = [60, 170, 350, 1000, 3500, 10000];
var gains = [0, 0, 0, 0, 0, 0];
for (var i = 0; i < frequencies.length; i++) {
    var filter = context.createBiquadFilter();
    filter.type = 'peaking';
    filter.frequency.value = frequencies[i];
    filter.gain.value = gains[i];
    filters.push(filter);
}

// connect the filters in series
source.connect(filters[0]);
for (var i = 0; i < filters.length - 1; i++) {
    filters[i].connect(filters[i + 1]);
}
filters[filters.length - 1].connect(context.destination);



// Analyse the audio
var kickFrequency = 60;
var kickTresshold = 255;
var volumeLowMidCrossover = 250;
var volumeMidHighCrossover = 1000;
var lastKickTimestamp = 0;

var kickDetected = false;
var kickTimestamps = [];
var timelineTimestamps = [];
var timelineKickVolumes = [];
var timelineLows = [];
var timelineMids = [];
var timelineHighs = [];
var timelineVolumes = [];
var timelineKickDetected = [];

// create a meter using a ScriptProcessorNode
var bufferSize = 1024;
var meter = context.createScriptProcessor(bufferSize, 1, 1);
var maxLevel = 0;

// Create an AnalyserNode to perform FFT
var analyser = context.createAnalyser();
analyser.fftSize = 1024;

// Create a frequency data array to store FFT values
//var frequencyData = new Float32Array(analyser.frequencyBinCount);
var frequencyData = new Uint8Array(analyser.frequencyBinCount);

meter.onaudioprocess = function (event) {
    try {
        let inputBuffer = event.inputBuffer;
        let inputData1 = inputBuffer.getChannelData(0);
        //analyser.getFloatFrequencyData(frequencyData); // Get the frequency data
        analyser.getByteFrequencyData(frequencyData);

        let kickVolume = 0;
        let kickVolumeFound = false;
        let volumeLowTotal = 0;
        let volumeLowCount = 0;
        let volumeMidTotal = 0;
        let volumeMidCount = 0;
        let volumeHighTotal = 0;
        let volumeHighCount = 0;
        for (var i = 0; i < frequencyData.length; i++) {
            var frequency = i * (context.sampleRate / analyser.fftSize);
            if (frequency > kickFrequency && !kickVolumeFound) {
                kickVolume = frequencyData[i];
                kickVolumeFound = true;
            }
            if (frequency < volumeLowMidCrossover) {
                volumeLowTotal += frequencyData[i];
                volumeLowCount++;
            }
            if (frequency >= volumeLowMidCrossover && frequency < volumeMidHighCrossover) {
                volumeMidTotal += frequencyData[i];
                volumeMidCount++;
            }
            if (frequency >= volumeMidHighCrossover) {
                volumeHighTotal += frequencyData[i];
                volumeHighCount++;
            }
        }

        if (kickVolume >= kickTresshold && !kickDetected) {

            if (context.currentTime - lastKickTimestamp > 0.3) {
                kickTimestamps.push(context.currentTime);
                lastKickTimestamp = context.currentTime;
                console.log('Kick detected at ' + context.currentTime);
            }

            kickDetected = true;
        }
        else if (kickVolume < kickTresshold) {
            kickDetected = false;
        }

        let timelineMaxLevel = 0;
        for (var i = 0; i < inputData1.length; i++) {
            let data = inputData1[i];
            if (data < 0) {
                data = data * -1;
            }
            if (maxLevel < data) {
                maxLevel = data;
            }
            if (timelineMaxLevel < data) {
                timelineMaxLevel = data;
            }
        }

        let timelineLow = volumeLowTotal / volumeLowCount;
        let timelineMid = volumeMidTotal / volumeMidCount;
        let timelineHigh = volumeHighTotal / volumeHighCount;

        timelineTimestamps.push(context.currentTime);
        timelineKickVolumes.push(kickVolume);
        timelineLows.push(timelineLow);
        timelineMids.push(timelineMid);
        timelineHighs.push(timelineHigh);
        timelineVolumes.push(timelineMaxLevel);
        timelineKickDetected.push(kickDetected);

    } catch (e) {
        console.log(e);
    }
};

// connect the meter and analyser to the source
source.connect(analyser);
analyser.connect(meter);
meter.connect(context.destination);



window.updateFilterParams = function (newGains) {
    for (var i = 0; i < filters.length; i++) {
        filters[i].gain.value = newGains[i];
    }
};
window.getKickTimestamps = function () {
    let res = kickTimestamps;
    kickTimestamps = [];
    return res;
};
window.getVuMeter = function () {
    let res = maxLevel;
    maxLevel = 0;
    return res;
};
window.getKickDetected = function () {
    return kickDetected;
};
window.getTimelineTimestamps = function () {
    let res = timelineTimestamps;
    timelineTimestamps = [];
    return res;
};
window.getTimelineKickVolumes = function () {
    let res = timelineKickVolumes;
    timelineKickVolumes = [];
    return res;
};
window.getTimelineLows = function () {
    let res = timelineLows;
    timelineLows = [];
    return res;
};
window.getTimelineMids = function () {
    let res = timelineMids;
    timelineMids = [];
    return res;
};
window.getTimelineHighs = function () {
    let res = timelineHighs;
    timelineHighs = [];
    return res;
};
window.getTimelineVolumes = function () {
    let res = timelineVolumes;
    timelineVolumes = [];
    return res;
};
window.getTimelineKickDetected = function () {
    let res = timelineKickDetected;
    timelineKickDetected = [];
    return res;
};

arguments[0].setAttribute('eq-injected', 'true');