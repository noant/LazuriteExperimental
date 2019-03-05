using Lazurite.ActionsDomain;
using Lazurite.ActionsDomain.Attributes;
using Lazurite.ActionsDomain.ValueTypes;
using Lazurite.Shared.ActionCategory;
using LazuriteUI.Icons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LazuriteVoicePlugin
{
    [LazuriteIcon(Icon.Microphone)]
    [HumanFriendlyName("Голосовая команда")]
    [SuitableValueTypes(typeof(ToggleValueType))]
    [Lazurite.Shared.ActionCategory.Category(Category.Control)]
    public class VoicePluginManualActivation : IAction
    {
        public string Caption { get => "Голосовая команда"; set { } }

        public ValueTypeBase ValueType { get; set; } = new ToggleValueType();

        public bool IsSupportsEvent => false;

        public bool IsSupportsModification => false;

        public event ValueChangedEventHandler ValueChanged;

        public string GetValue(ExecutionContext context) => ToggleValueType.ValueOFF;

        public void Initialize() { }

        public void SetValue(ExecutionContext context, string value) => VoicePlugin.Recognizer?.ActivateManually();

        public bool UserInitializeWith(ValueTypeBase valueType, bool inheritsSupportedValues) => true;
    }
}
