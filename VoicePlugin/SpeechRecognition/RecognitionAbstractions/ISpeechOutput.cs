using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecognitionAbstractions
{
    public interface ISpeechOutput
    {
        void Result(string text);
        void RecordCanceled();
        void RecordStarted();
        void RecordFinished();
        void IntermediateResult(string text);
    }
}
