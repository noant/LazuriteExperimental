using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechRecognition
{
    public interface ISpeechActivator
    {
        void Start();
        void Stop();
        bool Started { get; }
        IReadOnlyCollection<string> Keywords { get; set; }
        bool IgnoreRecognition { get; set; }
        event EventHandler Activated;
        event EventHandler Stopped;
    }
}
