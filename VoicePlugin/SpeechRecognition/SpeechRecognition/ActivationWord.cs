using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechRecognition
{
    public abstract class ActivationWord
    {
        public string Word { get; set; }

        public abstract void Activate(ActivatorEventArgs args);
    }
}
