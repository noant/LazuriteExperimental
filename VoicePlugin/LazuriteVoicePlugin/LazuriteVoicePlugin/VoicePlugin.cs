using Lazurite.ActionsDomain;
using Lazurite.ActionsDomain.Attributes;
using Lazurite.ActionsDomain.ValueTypes;
using Lazurite.Shared;
using Lazurite.Shared.ActionCategory;
using LazuriteUI.Icons;
using RecognitionAbstractions;
using SpeechRecognition;
using SpeechRecognitionUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LazuriteVoicePlugin
{
    [LazuriteIcon(Icon.Microphone)]
    [HumanFriendlyName("Управление голосом")]
    [SuitableValueTypes(typeof(ToggleValueType))]
    [Lazurite.Shared.ActionCategory.Category(Category.Control)]
    public class VoicePlugin : IAction, IScenariosEnumerator, IDisposable
    {
        public static GoogleSpeechRecognizer Recognizer;

        private Func<ScenarioCast[]> _needCasts;
        private SpeechTextHandler _handler;
        
        private ScenarioCast[] GetCasts() => _needCasts();

        public string Caption { get; set; } = "Управление голосом";

        public ValueTypeBase ValueType { get; set; } = new ToggleValueType();

        public bool IsSupportsEvent => true;

        public bool IsSupportsModification => true;

        public event ValueChangedEventHandler ValueChanged;

        public string GetValue(ExecutionContext context) => Recognizer.Started ? ToggleValueType.ValueON : ToggleValueType.ValueOFF;

        public void Initialize()
        {
            Recognizer?.Stop();

            _handler = new SpeechTextHandler();

            var casts = GetCasts().ToDictionary(x => x.ID);

            var activators = new List<ActivationWord>();

            foreach (var scenId in VoicePluginData.Current.WithoutActivationVoiceScenariosIds)
                if (casts.ContainsKey(scenId))
                    activators.Add(new SimpleScenarioActivator(casts[scenId]));

            foreach (var keyword in VoicePluginData.Current.Keywords)
                activators.Add(new ActivatorWord_StartGoogleRecognition(keyword));

            Recognizer = new GoogleSpeechRecognizer(
                new Output(_needCasts, _handler), 
                activators.ToArray(), 
                VoicePluginData.Current.ActivationConfidence,
                VoicePluginData.Current.ExecutionConfidence,
                VoicePluginData.Current.CaptureDevice);

            Recognizer.Stopped += (o, e) => ValueChanged?.Invoke(this, ToggleValueType.ValueOFF);
        }

        public void SetCasts(Func<ScenarioCast[]> needCasts) => _needCasts = needCasts;

        public void SetValue(ExecutionContext context, string value)
        {
            if (value == ToggleValueType.ValueOFF)
                Stop();
            else
            {
                if (VoicePluginData.Current.ActiveVoiceScenariosIds.Any())
                    Start();
                else value = ToggleValueType.ValueOFF;
            }

            ValueChanged?.Invoke(this, value);
        }

        private void Start()
        {
            if (!Recognizer.Started)
                Recognizer.Start();
        }

        private void Stop() => Recognizer.Stop();

        public bool UserInitializeWith(ValueTypeBase valueType, bool inheritsSupportedValues)
        {
            var casts = _needCasts().Where(x => x.CanSet).ToArray();
            var selected = casts.Where(x => VoicePluginData.Current.ActiveVoiceScenariosIds.Contains(x.ID)).ToArray();
            var selectedWithoutActivation = casts.Where(x => VoicePluginData.Current.WithoutActivationVoiceScenariosIds.Contains(x.ID)).ToArray();
            var volumeScen = casts.FirstOrDefault(x => x.ID == VoicePluginData.Current.VolumeLevelScenarioId);
            var infoScen = casts.FirstOrDefault(x => x.ID == VoicePluginData.Current.CommandsViewScenarioId);
            var soundNotifyScen = casts.FirstOrDefault(x => x.ID == VoicePluginData.Current.SoundNotifyViewScenarioId);
            var ctrl = new MainWindow(new ScenariosInfo(
                casts, 
                selected, selectedWithoutActivation, 
                volumeScen, 
                infoScen,
                soundNotifyScen,
                VoicePluginData.Current.ActivationConfidence, 
                VoicePluginData.Current.ExecutionConfidence,
                VoicePluginData.Current.CaptureDevice,
                VoicePluginData.Current.Keywords));
            if (ctrl.ShowDialog() ?? false)
            {
                var info = ctrl.Info;
                VoicePluginData.Current.ActiveVoiceScenariosIds = info.SelectedScenarios.Select(x => x.ID).ToArray();
                VoicePluginData.Current.WithoutActivationVoiceScenariosIds = info.WithoutActivationScenarios.Select(x => x.ID).ToArray();
                VoicePluginData.Current.CommandsViewScenarioId = info.InfoScenario?.ID;
                VoicePluginData.Current.VolumeLevelScenarioId = info.VolumeScenario?.ID;
                VoicePluginData.Current.SoundNotifyViewScenarioId = info.SoundNotifyScenario?.ID;
                VoicePluginData.Current.ActivationConfidence = info.ActivationConfidence;
                VoicePluginData.Current.ExecutionConfidence = info.ExecutionConfidence;
                VoicePluginData.Current.Keywords = info.Keywords;
                VoicePluginData.Current.Save();
                if (!VoicePluginData.Current.ActiveVoiceScenariosIds.Any() || !VoicePluginData.Current.Keywords.Any())
                {
                    Stop();
                    ValueChanged?.Invoke(this, ToggleValueType.ValueOFF);
                }
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            Recognizer?.Stop();
        }

        private class SimpleScenarioActivator : ActivationWord
        {
            public SimpleScenarioActivator(ScenarioCast cast) {
                Cast = cast;
                Word = cast.Name;
            }

            public ScenarioCast Cast { get; }

            public override void Activate(ActivatorEventArgs args)
            {
                Cast.Value = string.Empty;
                args.ActivatorContinue();
            }
        }

        private class Output : ISpeechOutput
        {
            private readonly Func<ScenarioCast[]> _needCasts;
            private ScenarioCast[] _cache;
            private SpeechTextHandler _handler;
            private ScenarioCast[] GetCasts() => _needCasts();

            public Output(Func<ScenarioCast[]> needCasts, SpeechTextHandler handler)
            {
                _needCasts = needCasts;
                _handler = handler;
            }

            public void RecordCanceled()
            {
                //_handler.HandleCancel(_cache);
            }

            public void RecordStarted()
            {
                _cache = GetCasts();
                _handler.HandleReady(_cache);
            }

            public void IntermediateResult(string str)
            {
                _handler.HandleIntermediate(str, _cache);
            }

            public void Result(string text)
            {
                _handler.Handle(text, _cache);
            }

            public void RecordFinished()
            {
                _handler.HandleFinish(_cache);
                _cache = null;
            }
        }
    }
}
