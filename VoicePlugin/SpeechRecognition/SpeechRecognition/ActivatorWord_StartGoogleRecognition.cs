using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechRecognition
{
    public class ActivatorWord_StartGoogleRecognition : ActivationWord
    {
        private Action<ActivatorEventArgs> _startRecognition;

        public ActivatorWord_StartGoogleRecognition(string word)
        {
            Word = word;
        }

        public void SetAction(Action<ActivatorEventArgs> startRecognition)
        {
            _startRecognition = startRecognition;
        }

        public override void Activate(ActivatorEventArgs args)
        {
            _startRecognition?.Invoke(args);
        }
    }
}
