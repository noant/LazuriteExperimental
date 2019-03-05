using Lazurite.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechRecognitionUI
{
    public class ScenariosInfo
    {
        public ScenariosInfo(ScenarioCast[] allScenarios, ScenarioCast[] selectedScenarios, ScenarioCast[] withoutActivationScenarios, ScenarioCast volumeScenario, ScenarioCast infoScenario, ScenarioCast soundNotifyScenario, float activationConfidence, float executionConfidence, string captureDevice, string[] keywords)
        {
            AllScenarios = allScenarios ?? throw new ArgumentNullException(nameof(allScenarios));
            SelectedScenarios = selectedScenarios ?? throw new ArgumentNullException(nameof(selectedScenarios));
            WithoutActivationScenarios = withoutActivationScenarios;
            VolumeScenario = volumeScenario;
            InfoScenario = infoScenario;
            SoundNotifyScenario = soundNotifyScenario;
            ExecutionConfidence = executionConfidence;
            CaptureDevice = captureDevice;
            Keywords = keywords ?? throw new ArgumentNullException(nameof(keywords));
            ActivationConfidence = activationConfidence;
        }

        public ScenarioCast[] AllScenarios { get; private set; }
        public ScenarioCast[] SelectedScenarios { get; private set; }
        public ScenarioCast[] WithoutActivationScenarios { get; private set; }
        public string[] Keywords { get; private set; }
        public ScenarioCast VolumeScenario { get; private set; }
        public ScenarioCast InfoScenario { get; private set; }
        public ScenarioCast SoundNotifyScenario { get; private set; }
        public string CaptureDevice { get; private set; }

        public float ExecutionConfidence { get; private set; }
        public float ActivationConfidence { get; private set; }
    }
}
