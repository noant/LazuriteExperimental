using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechRecognition
{
    public class ActivatorEventArgs: EventArgs
    {
        private ISpeechActivator _recognizer;

        public string Word { get; set; }

        public ActivatorEventArgs(ISpeechActivator recognizer) => _recognizer = recognizer;

        public void ActivatorContinue() => _recognizer.IgnoreRecognition = false;
    }
}
