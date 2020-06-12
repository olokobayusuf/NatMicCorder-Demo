/* 
*   NatSuite Examples
*   Copyright (c) 2020 Yusuf Olokoba
*/

namespace NatSuite.Examples {

    using UnityEngine;
    using UnityEngine.UI;
    using Devices;
    using Recorders;
    using Recorders.Clocks;
    using Recorders.Inputs;

    public class ReplayCam : MonoBehaviour {

        [Header(@"Recording")]
        public int videoWidth = 720;
        public int videoHeight = 1280;
        public bool recordMicrophone;

        [Header(@"UI")]
        public RawImage rawImage;
        public AspectRatioFitter aspectFitter;

        private IMediaRecorder recorder;
        private CameraInput cameraInput;
        private IAudioDevice audioDevice; // Used to record microphone

        async void Start () {
            // Request permissions
            if (!await MediaDeviceQuery.RequestPermissions<CameraDevice>()) {
                Debug.LogError("User did not grant camera permissions");
                return;
            }
            if (!await MediaDeviceQuery.RequestPermissions<AudioDevice>()) {
                Debug.LogError("User did not grant microphone permissions");
                return;
            }
            // Get a microphone
            var micQuery = new MediaDeviceQuery(MediaDeviceQuery.Criteria.AudioDevice);
            audioDevice = micQuery.currentDevice as IAudioDevice;
            // Get rear camera
            var query = new MediaDeviceQuery(MediaDeviceQuery.Criteria.GenericCameraDevice);
            var device = query.currentDevice as ICameraDevice;
            // Start the camera preview
            device.previewResolution = (1280, 720);
            var previewTexture = await device.StartRunning();
            // Display preview
            rawImage.texture = previewTexture;
            aspectFitter.aspectRatio = (float)previewTexture.width / previewTexture.height;
        }

        public void StartRecording () {
            // Create recorder
            var clock = new RealtimeClock();
            if (recordMicrophone)
                recorder = new MP4Recorder(videoWidth, videoHeight, 30, audioDevice.sampleRate, audioDevice.channelCount);
            else
                recorder = new MP4Recorder(videoWidth, videoHeight, 30);
            // Stream media samples to the recorder
            cameraInput = new CameraInput(recorder, clock, Camera.main);
            if (recordMicrophone)
                audioDevice.StartRunning((sampleBuffer, timestamp) => recorder.CommitSamples(sampleBuffer, clock.timestamp));
        }

        public async void StopRecording () {
            // Stop committing media samples
            if (audioDevice.running)
                audioDevice.StopRunning();
            cameraInput.Dispose();
            // Finish writing video
            var recordingPath = await recorder.FinishWriting();
            Debug.Log($"Saved recording to: {recordingPath}");
            // Playback recording
            Handheld.PlayFullScreenMovie($"file://{recordingPath}");
        }
    }
}