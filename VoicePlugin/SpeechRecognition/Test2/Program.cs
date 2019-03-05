using RecognitionAbstractions;
using SpeechRecognition;
using System;

namespace Test2
{
    class Program
    {
        private class Output : ISpeechOutput
        {
            public void RecordCanceled()
            {
                Console.WriteLine("Отмена.");
            }

            public void RecordStarted()
            {
                Console.WriteLine("Говорите...");
            }

            public void Result(string text)
            {
                Console.WriteLine(text);
            }

            public void RecordFinished()
            {
                Console.WriteLine("Запись окончена");
            }

            public void IntermediateResult(string text)
            {
                Console.WriteLine("~" + text);
            }
        }

        static void Main(string[] args)
        {
            var recognizer = new GoogleSpeechRecognizer(new Output(), new ActivationWord[] {
                new ActivatorWord_StartGoogleRecognition("окей дом"),
                new TestActivator("Дом давай спать"),
                new TestActivator("Дом проснись и пой"),
            },
            0.65f, 0.7f, Utils.GetCaptureDevices()[0]);

            recognizer.Start();

            while (true)
            {
                Console.ReadLine();
                recognizer.ActivateManually();
            }
        }

        private class TestActivator : ActivationWord
        {
            public TestActivator(string scenarioName)
            {
                Word = scenarioName;
            }

            public override void Activate(ActivatorEventArgs args)
            {
                Console.WriteLine("Выполняется сценарий: " + Word);
                args.ActivatorContinue();
            }
        }
    }
}
