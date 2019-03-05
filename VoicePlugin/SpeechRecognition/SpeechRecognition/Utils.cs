using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using Microsoft.Speech.Synthesis;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechRecognition
{
    public static class Utils
    {
        public static bool CompareActivatorPhrases(string s1, string s2) => PrepareActivatorString(s1) == PrepareActivatorString(s2);

        public static string PrepareActivatorString(string s)
        {
            return s.Replace(",", "").Replace(".", "").Replace("  ", " ").Trim().ToLowerInvariant();
        }

        public static string[] GetCaptureDevices()
        {
            return Enumerable.Range(0, WaveIn.DeviceCount).Select(x => WaveIn.GetCapabilities(x)).Select(x => x.ProductName + "... "+ x.ProductGuid).ToArray();
        }
        
        public static int GetCaptureDeviceIndex(string name) => GetCaptureDevices().ToList().IndexOf(name);
    }
}
