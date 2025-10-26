using UnityEngine;
using Whisper;
using Whisper.Utils;

namespace Code
{
    public class VoiceCapture : MonoBehaviour
    {
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;
        private WhisperStream _stream;

        private async void Start()
        {
            _stream = await whisper.CreateStream(microphoneRecord);
            _stream.OnSegmentUpdated += OnSegmentFinished;
            _stream.OnSegmentFinished += OnsegmentUpdated;
            _stream.OnStreamFinished += OnStreamFinished;
            microphoneRecord.OnRecordStop += ResetStream;
            _stream.StartStream();
            microphoneRecord.StartRecord();
        }
    
        private void OnSegmentFinished(WhisperResult segment)
        {
            print($"Segment finished: {segment.Result}");
            VoiceNavigationSystem.Instance.RequestNavigation(segment.Result);
        }
        
        private void OnsegmentUpdated(WhisperResult segment)
        {
            VoiceNavigationSystem.Instance.OnRecordingPulse();
        }
        
        private void OnStreamFinished(string finalResult)
        {
            print("Stream finished!");
        }

        private void ResetStream(AudioChunk recordedAudio)
        {
        
        }
    }
}