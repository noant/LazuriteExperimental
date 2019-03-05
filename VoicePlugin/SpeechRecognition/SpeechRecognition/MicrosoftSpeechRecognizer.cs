using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SpeechRecognition
{
    public class MicrosoftSpeechRecognizer: ISpeechActivator
    {
        private SpeechRecognitionEngine _sre;
        //private WaveInEvent _waveIn;
        private string[] _keywords;
        private readonly float _confidence;
        private readonly string _captureDeviceName;

        public IReadOnlyCollection<string> Keywords
        {
            get => _keywords;
            set {
                _keywords = value.ToArray();
                if (Started)
                    Restart();
            }
        }

        public MicrosoftSpeechRecognizer(float activationConfidence, string captureDeviceName)
        {
            _confidence = activationConfidence;
            _captureDeviceName = captureDeviceName;
        }

        public event EventHandler Activated;

        public bool Started => _sre != null;

        public void Stop()
        {
            _sre?.RecognizeAsyncStop();
            _sre = null;
            //_waveIn?.StopRecording();
            //_waveIn = null;
            //_sre = null;
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public bool IgnoreRecognition { get; set; } = false;

        public event EventHandler Stopped;

        private void StopInternal()
        {
            Stopped?.Invoke(this, EventArgs.Empty);
            _sre = null;
        }

        public void Start()
        {
            if (_sre != null)
                throw new ArgumentException("Recognition is started");

            try
            {
                //var sfi = new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono);

                // Start in new high-priority thread
                new Thread(() => {
                    try
                    {
                        var ci = new System.Globalization.CultureInfo("ru-ru");
                        _sre = new SpeechRecognitionEngine(ci);

                        var activators = new Choices();
                        activators.Add(_keywords);

                        var gb = new GrammarBuilder();
                        gb.Culture = ci;
                        gb.Append(activators);

                        _sre.LoadGrammar(new Grammar(gb));

                        _sre.SetInputToDefaultAudioDevice();

                        _sre.SpeechRecognized += (o, e) =>
                        {
                            if (!IgnoreRecognition && e.Result.Confidence >= _confidence)
                            {
                                IgnoreRecognition = true;
                                Activated?.Invoke(this, new ActivatorEventArgs(this) { Word = e.Result.Text });
                            }
                        };

                        _sre.AudioSignalProblemOccurred += (o, e) =>
                        {
                            if (e.AudioSignalProblem == AudioSignalProblem.NoSignal)
                                StopInternal();
                        };
                        _sre.AudioStateChanged += (o, e) =>
                        {
                            if (e.AudioState == AudioState.Stopped)
                                StopInternal();
                        };

                        _sre.RecognizeCompleted += (o, e) => StopInternal();

                        _sre.RecognizeAsync(RecognizeMode.Multiple);
                    }
                    catch {
                        _sre = null;
                    }
                })
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Highest
                }
                .Start();

                //var writeLock = new object();
                //var waveIn = new WaveInEvent();
                //waveIn.DeviceNumber = Utils.GetCaptureDeviceIndex(_captureDeviceName);
                //if (waveIn.DeviceNumber == -1)
                //    throw new Exception("Cannot find capture device " + _captureDeviceName);
                //waveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);
                //waveIn.BufferMilliseconds = 1500;

                //var maxBufferLoadTimes = 7;
                //var bufferLoadTimes = 0;
                //var buffer = new byte[(maxBufferLoadTimes + 1) * 48000];
                //var bufferPosition = 0;
                //var checkLast_ms = 2500;
                
                //waveIn.DataAvailable +=
                //    (object sender, NAudio.Wave.WaveInEventArgs args) =>
                //    {
                //        if (!IgnoreRecognition)
                //            lock (writeLock)
                //            {
                //                if (bufferLoadTimes == maxBufferLoadTimes || bufferPosition + args.BytesRecorded > buffer.Length)
                //                {
                //                    var prev = new byte[args.Buffer.Length];
                //                    Array.Copy(buffer, buffer.Length - prev.Length, prev, 0, prev.Length);
                //                    Array.Clear(buffer, 0, bufferPosition);
                //                    Array.Copy(prev, buffer, prev.Length);
                //                    bufferPosition = prev.Length;
                //                    bufferLoadTimes = 0;
                //                }

                //                args.Buffer.CopyTo(buffer, bufferPosition);
                //                Array.Copy(args.Buffer, 0, buffer, bufferPosition, args.BytesRecorded);
                //                bufferPosition += args.BytesRecorded;
                //                bufferLoadTimes++;

                //                var start = bufferPosition - ((float)checkLast_ms / waveIn.BufferMilliseconds) * args.BytesRecorded;
                //                if (start < 0)
                //                    start = 0;

                //                var count = bufferPosition - (int)start;

                //                Debug.WriteLine("Bytes to recognize: " + count);

                //                _sre.SetInputToAudioStream(new MemoryStream(buffer, (int)start, count), sfi);
                //                var result = _sre.Recognize();
                //                if (result != null)
                //                {
                //                    if (result.Confidence >= _confidence)
                //                    {
                //                        Array.Clear(buffer, 0, bufferPosition);
                //                        bufferPosition = 0;
                //                        bufferLoadTimes = 0;
                //                        IgnoreRecognition = true;
                //                        Activated?.Invoke(this, new ActivatorEventArgs(this) { Word = result.Text });
                //                    }
                //                }
                //            }
                //    };
                //waveIn.StartRecording();
                
                Console.WriteLine("Record started...");
            }
            catch (Exception e)
            {
                _sre = null;
                throw e;
            }
        }
    }
}
