using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using Grpc.Auth;
using Grpc.Core;
using RecognitionAbstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SpeechRecognition
{
    public static class GoogleSpeechApiUtils
    {
        private static string GetAssemblyPath(Assembly assembly)
        {
            string codeBase = assembly.CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            return Path.GetFullPath(Uri.UnescapeDataString(uri.Path));
        }

        private static string GetAssemblyFolder(Assembly assembly) => Path.GetDirectoryName(GetAssemblyPath(assembly));

        private static GoogleCredential Credential;
        private static Channel Channel;
        private static SpeechClient Client;

        public static void Authorize()
        {
            var fileStream = new FileStream(Path.Combine(GetAssemblyFolder(Assembly.GetExecutingAssembly()), @"google_auth.json"), FileMode.Open);
            Credential = GoogleCredential
                .FromStream(fileStream)
                .CreateScoped(SpeechClient.DefaultScopes);
            Channel = new Grpc.Core.Channel(SpeechClient.DefaultEndpoint.Host, Credential.ToChannelCredentials());

            Client = SpeechClient.Create(Channel);
        }

        public static async Task StreamingMicRecognizeAsync(int ms, ISpeechOutput output, Action finish, float minConfidence, int captureDeviceIndex)
        {
            var streamingCall = Client.StreamingRecognize();
            await streamingCall.WriteAsync(
                new StreamingRecognizeRequest()
                {
                    StreamingConfig = new StreamingRecognitionConfig()
                    {
                        Config = new RecognitionConfig()
                        {
                            Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                            SampleRateHertz = 16000,
                            LanguageCode = "ru"
                        },
                        InterimResults = false,
                    }
                });

            var normalSpeechDetected = false;
            Task printResponses = Task.Run(async () =>
            {
                var outText = string.Empty;

                while (await streamingCall.ResponseStream.MoveNext())
                {
                    foreach (var result in streamingCall.ResponseStream.Current.Results)
                    {
                        var confidence = result.Alternatives.Max(x => x.Confidence);
                        Debug.WriteLine("SpeechRecognition");
                        foreach (var alt in result.Alternatives)
                        {
                            Debug.WriteLine(alt.Transcript + " " + alt.Confidence);
                        }

                        if (confidence >= minConfidence)
                        {
                            var alternative = result.Alternatives.LastOrDefault(x => x.Confidence == confidence);
                            output.IntermediateResult(alternative.Transcript);
                            outText = alternative.Transcript;
                            normalSpeechDetected = true;
                        }
                    }
                }

                if (normalSpeechDetected)
                {
                    output.Result(outText);
                }
            });

            object writeLock = new object();
            bool writeMore = true;
            var waveIn = new NAudio.Wave.WaveInEvent();
            waveIn.DeviceNumber = captureDeviceIndex;
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);
            waveIn.DataAvailable +=
                (object sender, NAudio.Wave.WaveInEventArgs args) =>
                {
                    lock (writeLock)
                    {
                        if (!writeMore)
                        {
                            return;
                        }

                        streamingCall.WriteAsync(
                            new StreamingRecognizeRequest()
                            {
                                AudioContent = Google.Protobuf.ByteString
                                    .CopyFrom(args.Buffer, 0, args.BytesRecorded)
                            }).Wait();
                    }
                };
            output.RecordStarted();
            try
            {
                waveIn.StartRecording();
                await Task.Delay(TimeSpan.FromMilliseconds(ms));
                if (!normalSpeechDetected)
                {
                    output.RecordCanceled();
                }

                waveIn.StopRecording();
                lock (writeLock)
                {
                    writeMore = false;
                }

                await streamingCall.WriteCompleteAsync();
                await printResponses;
                finish?.Invoke();
                output.RecordFinished();
            }
            catch
            {
                output.RecordCanceled();
            }
        }
    }
}