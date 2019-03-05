using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LazuriteVoicePlugin
{
    public static class Utils
    {
        public static string ConvertToNormalString(int number)
        {
            switch (number)
            {
                case 0: return "ноль";
                case 5: return "пять";
                case 10: return "десять";
                case 15: return "пятнадцать";
                case 20: return "двадцать";
                case 25: return "двадцать пять";
                case 30: return "тридцать";
                case 35: return "тридцать пять";
                case 40: return "сорок";
                case 45: return "сорок пять";
                case 50: return "пятьдесят";
                case 55: return "пятьдесят пять";
                case 60: return "шестьдесят";
                case 65: return "шестьдесят пять";
                case 70: return "семьдесят";
                case 75: return "семьдесят пять";
                case 80: return "восемдесят";
                case 85: return "восемдесят пять";
                case 90: return "девяносто";
                case 95: return "девяносто пять";
                case 100: return "сто";
            }

            throw new Exception("Even 5");
        }
    }
}
