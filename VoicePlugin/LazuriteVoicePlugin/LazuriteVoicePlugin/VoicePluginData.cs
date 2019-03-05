using Lazurite.Data;
using Lazurite.IOC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LazuriteVoicePlugin
{
    public class VoicePluginData
    {
        private static PluginsDataManagerBase DataManager;

        private static VoicePluginData _data;

        public static VoicePluginData Current
        {
            get
            {
                if (_data == null)
                {
                    if (Singleton.Any<PluginsDataManagerBase>())
                    {
                        DataManager = Singleton.Resolve<PluginsDataManagerBase>();
                        if (DataManager.Has(nameof(VoicePluginData)))
                            _data = DataManager.Get<VoicePluginData>(nameof(VoicePluginData));
                        else _data = new VoicePluginData();
                    }
                    else _data = new VoicePluginData();
                }
                return _data;
            }
        }

        public VoicePluginData()
        {
            CaptureDevice = SpeechRecognition.Utils.GetCaptureDevices().FirstOrDefault();
        }

        public void Save() => DataManager.Set(nameof(VoicePluginData), _data);

        public List<ScenarioSynonyms> Synonyms { get; private set; } = new List<ScenarioSynonyms>();
        public string VolumeLevelScenarioId { get; set; }
        public string CommandsViewScenarioId { get; set; }
        public string SoundNotifyViewScenarioId { get; set; }
        public string[] ActiveVoiceScenariosIds { get; set; } = new string[0];
        public string[] WithoutActivationVoiceScenariosIds { get; set; } = new string[0];
        public string[] Keywords { get; set; } = new[] { "окей лазурит" };
        public float ActivationConfidence { get; set; } = 0.66f;
        public float ExecutionConfidence { get; set; } = 0.7f;

        public string CaptureDevice { get; set; }
    }
}
