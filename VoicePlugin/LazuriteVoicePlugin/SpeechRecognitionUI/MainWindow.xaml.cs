using Lazurite.ActionsDomain.ValueTypes;
using Lazurite.Shared;
using LazuriteUI.Windows.Controls;
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

namespace SpeechRecognitionUI
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(ScenariosInfo info)
        {
            InitializeComponent();

            cbScenariosWithActivation.Info = new ComboItemsViewInfo(
                ListViewItemsSelectionMode.Multiple,
                info.AllScenarios,
                (o) => (o as ScenarioCast)?.Name,
                (o) => LazuriteUI.Icons.Icon.ChevronRight,
                info.SelectedScenarios,
                "Выбрать сценарии для голосовой активации",
                panel);

            cbScenariosWithoutActivation.Info = new ComboItemsViewInfo(
                ListViewItemsSelectionMode.Multiple,
                info.AllScenarios.Where(x => x.ValueType is ButtonValueType).ToArray(),
                (o) => (o as ScenarioCast)?.Name,
                (o) => LazuriteUI.Icons.Icon.ChevronRight,
                info.WithoutActivationScenarios,
                "Сценарии для голосовой активации без ключевого слова",
                panel);

            cbInfoScenario.Info = new ComboItemsViewInfo(
                ListViewItemsSelectionMode.Single,
                info.AllScenarios.Where(x => x.ValueType is InfoValueType && x.CanSet).ToArray(),
                (o) => (o as ScenarioCast)?.Name,
                (o) => LazuriteUI.Icons.Icon.ChevronRight,
                new[] { info.InfoScenario },
                "Сценарий для голосовой нотификации",
                panel);

            cbVolumeScenario.Info = new ComboItemsViewInfo(
                ListViewItemsSelectionMode.Single,
                info.AllScenarios.Where(x => x.ValueType is FloatValueType && x.CanSet).ToArray(),
                (o) => (o as ScenarioCast)?.Name,
                (o) => LazuriteUI.Icons.Icon.ChevronRight,
                new[] { info.VolumeScenario },
                "Сценарий управления уровнем звука",
                panel);

            cbMics.Info = new ComboItemsViewInfo(
                ListViewItemsSelectionMode.Single,
                SpeechRecognition.Utils.GetCaptureDevices(),
                (x) => x?.ToString() ?? "Нет",
                (x) => LazuriteUI.Icons.Icon.Microphone,
                new[] { info.CaptureDevice },
                "Выберите устройство захвата звука",
                panel);

            cbSoundNotifyScens.Info = new ComboItemsViewInfo(
                ListViewItemsSelectionMode.Single,
                info.AllScenarios.Where(x => x.ValueType is StateValueType && x.ValueType.AcceptedValues.Length >= 3 && x.CanSet).ToArray(),
                (o) => (o as ScenarioCast)?.Name,
                (x) => LazuriteUI.Icons.Icon.Sound3,
                new[] { info.SoundNotifyScenario },
                "Выберите сценарий звуковых нотификаций",
                panel);

            tbKeyWords.Text = info.Keywords.Aggregate((x1, x2) => x1 + ", " + x2);

            sExecutionConfidence.Value = info.ExecutionConfidence;
            sActivationConfidence.Value = info.ActivationConfidence;
        }

        public ScenariosInfo Info
        {
            get
            {
                return new ScenariosInfo(
                    cbScenariosWithActivation.Info.Objects.Cast<ScenarioCast>().ToArray(),
                    cbScenariosWithActivation.Info.SelectedObjects.Cast<ScenarioCast>().ToArray(),
                    cbScenariosWithoutActivation.Info.SelectedObjects.Cast<ScenarioCast>().ToArray(),
                    cbVolumeScenario.Info.SelectedObjects.FirstOrDefault() as ScenarioCast,
                    cbInfoScenario.Info.SelectedObjects.FirstOrDefault() as ScenarioCast,
                    cbSoundNotifyScens.Info.SelectedObjects.FirstOrDefault() as ScenarioCast,
                    (float)sActivationConfidence.Value,
                    (float)sExecutionConfidence.Value,
                    cbMics.Info.SelectedObjects.FirstOrDefault() as string,

                    tbKeyWords
                        .Text
                        .Split(',')
                        .Select(x => x.Trim())
                        .Except(new[] { string.Empty })
                        .ToArray());
            }
        }

        private void ApplyClick(object sender, RoutedEventArgs e) => DialogResult = true;

        private void CancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
