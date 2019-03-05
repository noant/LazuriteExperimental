using RecognitionAbstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeechRecognition
{
    public class GoogleSpeechRecognizer
    {
        private ISpeechActivator _activator;
        private readonly ISpeechOutput _output;
        private ActivatorWord_StartGoogleRecognition _googleRecognition;

        public ActivationWord[] Activators { get; }
        public float ActivationConfidence { get; }
        public float CommandConfidence { get; }
        public int CaptureDeviceIndex { get; }

        public GoogleSpeechRecognizer(ISpeechOutput output, ActivationWord[] activators, float activationConfidence, float commandConfidence, string captureDeviceName)
        {
            GoogleSpeechApiUtils.Authorize();

            _output = output;
            Activators = activators;
            ActivationConfidence = activationConfidence;
            CommandConfidence = commandConfidence;
            CaptureDeviceIndex = Utils.GetCaptureDeviceIndex(captureDeviceName);

            _activator = new MicrosoftSpeechRecognizer(activationConfidence, captureDeviceName);
            _activator.Keywords = activators.Select(x => Utils.PrepareActivatorString(x.Word)).ToArray();
            foreach (var activator in activators)
            {
                if (activator is ActivatorWord_StartGoogleRecognition _googleRecognition)
                    _googleRecognition.SetAction(async (args) => await ActivateInternalAsync(args.ActivatorContinue));
            }
            _activator.Activated += (o, e) =>
            {
                new Thread(() => {
                    var args = e as ActivatorEventArgs;
                    bool found = false;
                    foreach (var activator in activators)
                        if (Utils.CompareActivatorPhrases(activator.Word, args.Word))
                        {
                            activator.Activate((ActivatorEventArgs)e);
                            found = true;
                            break;
                        }
                    if (!found)
                        args.ActivatorContinue();
                })
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Highest
                }
                .Start();
            };
        }

        public void Start() => _activator.Start();

        public void Stop() => _activator.Stop();

        private async Task ActivateInternalAsync(Action callback) => await GoogleSpeechApiUtils.StreamingMicRecognizeAsync(5700, _output, callback, CommandConfidence, CaptureDeviceIndex);

        public async void ActivateManually()
        {
            if (!_activator.IgnoreRecognition)
            {
                _activator.IgnoreRecognition = true;
                await ActivateInternalAsync(() => _activator.IgnoreRecognition = false);
            }
        }

        public bool Started => _activator.Started;

        public event EventHandler Stopped
        {
            add => _activator.Stopped += value;
            remove => _activator.Stopped -= value;
        }
    }
}
