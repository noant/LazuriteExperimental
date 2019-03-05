using Lazurite.ActionsDomain.ValueTypes;
using Lazurite.Shared;
using LazuriteVoicePlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Test
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            VoicePluginData.Current.ActiveVoiceScenariosIds = new string[] { "1", "2", "3" };
            VoicePluginData.Current.WithoutActivationVoiceScenariosIds = new string[] { "2" };
            VoicePluginData.Current.CommandsViewScenarioId = "4";
            VoicePluginData.Current.VolumeLevelScenarioId = "1";

            var scenarios = new ScenarioCast[] {
                new ScenarioCastTest(new FloatValueType(){ AcceptedValues=new string[]{"0", "100" } }, "Звук", "1"),
                new ScenarioCastTest(new InfoValueType(), "Вывод", "4"),
                new ScenarioCastTest(new ButtonValueType(), "Тест", "2"),
                new ScenarioCastTest(new InfoValueType(), "сообщение наде", "3"),
            };

            var voicePlugin = new VoicePlugin();
            voicePlugin.SetCasts(() => scenarios);
            voicePlugin.Initialize();
            voicePlugin.SetValue(null, ToggleValueType.ValueON);

            voicePlugin.UserInitializeWith(null, false);
        }

        public class ScenarioCastTest : ScenarioCast
        {
            public ScenarioCastTest(Lazurite.ActionsDomain.ValueTypes.ValueTypeBase valueType, string name, string id)
                : base((s) => Console.WriteLine(name + " " + s), () => valueType.DefaultValue, valueType, name, id, true, true, true)
            {

            }

        }
    }
}
