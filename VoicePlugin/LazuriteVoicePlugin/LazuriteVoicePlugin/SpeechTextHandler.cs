using Lazurite.ActionsDomain.ValueTypes;
using Lazurite.Shared;
using SentencesFuzzyComparison;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LazuriteVoicePlugin
{
    /// <summary>
    /// Этот класс является времянкой и представляет собой много хардкода под русскую речь.
    /// Он работает и позволяет выполнять большинство сценариев путем составления списка всех вариаций использования.
    /// </summary>
    public class SpeechTextHandler
    {
        private static readonly Tuple<string, string>[] StandardSynonyms = new[] {
            new Tuple<string,string>("ё", "е")
        };

        private string _prevSoundVolume = "0";

        private FuzzyComparer _comparer = new FuzzyComparer();

        public void Handle(string text, ScenarioCast[] casts)
        {
            casts = casts.Where(x => x.CanSet && x.Enabled).ToArray();

            text = PrepareString(text);
            
            SendInfo("Команда: " + text + ".", casts);

            if (text.StartsWith("отмена"))
            {
                HandleCancel(casts);
            }
            else
            {
                var after = AfterAction(ref text);

                var execCasts = casts.Where(x => VoicePluginData.Current.ActiveVoiceScenariosIds.Contains(x.ID)).ToArray();

                var alt = FindAlternation(execCasts, text);
                if (alt != null)
                {
                    after.Execute(alt);

                    SendExecutionInfo(after, alt, casts);

                    if (alt.Scenario.ID == VoicePluginData.Current.VolumeLevelScenarioId && after.Value == 0)
                        _prevSoundVolume = null;

                    ReturnVolume(casts);
                }
                else
                {
                    SendSound(SoundNotification.Cancel, casts);
                    ReturnVolume(casts);
                    SendInfo(string.Format("Команда не найдена: {0}.", text), casts);
                }
            }
        }

        public void HandleIntermediate(string str, ScenarioCast[] casts)
        {
            SendInfo(str, casts);
        }

        private After AfterAction(ref string text)
        {
            var words = text.Split(' ').ToList();
            var index = words.IndexOf("через");
            if (index == -1)
                return new After(0);
            var numberIndex = index + 1;
            if (numberIndex >= words.Count)
                return new After(0);
            var number = GetNumber_After(words[numberIndex]);
            if (number == null)
                return new After(0);
            var unitIndex = numberIndex + 1;
            if (unitIndex >= words.Count)
                return new After(0);
            var unit = words[unitIndex];
            After after = null;

            switch (unit)
            {
                case "секунд": after = new After(number.Value); break;
                case "секунды": after = new After(number.Value); break;
                case "минут": after = new After(number.Value, TimeUnit.Minute); break;
                case "минуту": after = new After(number.Value, TimeUnit.Minute); break;
                case "минуты": after = new After(number.Value, TimeUnit.Minute); break;
                case "час": after = new After(number.Value, TimeUnit.Hour); break;
                case "часа": after = new After(number.Value, TimeUnit.Hour); break;
                case "часов": after = new After(number.Value, TimeUnit.Hour); break;
            }

            if (after != null)
            {
                text = text.Replace(words[index] + " " + words[numberIndex] + " " + words[unitIndex], string.Empty);
                return after;
            }
            return new After(0);
        }
        
        private void SendExecutionInfo(After after, Alternation alternation, ScenarioCast[] casts)
        {
            SendSound(SoundNotification.Apply, casts);

            if (after.Value != 0)
            {
                var unit = "секунд(ы)";
                switch (after.Unit)
                {
                    case TimeUnit.Minute: unit = "минут(ы)"; break;
                    case TimeUnit.Hour: unit = "часа(ов)"; break;
                }
                SendInfo(string.Format("Выполнить: {0}\r\nчерез {1} {2}.", alternation.Synonym, after.Value, unit), casts);
            }
            else
                SendInfo("Выполняю: " + alternation.Synonym + ".", casts);
        }

        private uint? GetNumber_After(string word)
        {
            if (uint.TryParse(word, out uint res))
                return res;
            else {
                switch (word)
                {
                    case "один": return 1;
                    case "два": return 2;
                    case "три": return 3;
                    case "четыре": return 4;
                    case "пять": return 5;
                    case "шесть": return 6;
                    case "семь": return 7;
                    case "восемь": return 8;
                    case "девять": return 9;
                    case "десять": return 10;
                }
            }
            return null;
        }

        public void HandleCancel(ScenarioCast[] casts)
        {
            SendInfo("Отмена.", casts);
        }

        public void HandleReady(ScenarioCast[] casts)
        {
            SendInfo("#Слушаю...", casts);
            SendSound(SoundNotification.OK, casts);
            LowDownVolume(casts);
        }

        public void HandleFinish(ScenarioCast[] casts)
        {
            ReturnVolume(casts);
        }

        private void SendInfo(string info, ScenarioCast[] casts)
        {
            var commandViewScenario = casts.FirstOrDefault(x => x.ID == VoicePluginData.Current.CommandsViewScenarioId);
            if (commandViewScenario != null && commandViewScenario.Enabled && commandViewScenario.CanSet && commandViewScenario.ValueType is InfoValueType)
                commandViewScenario.Value = info;
        }

        private void SendSound(SoundNotification n, ScenarioCast[] casts)
        {
            var scen = casts.FirstOrDefault(x => x.ID == VoicePluginData.Current.SoundNotifyViewScenarioId);
            if (scen != null && scen.Enabled && scen.CanSet && scen.ValueType is StateValueType && scen.ValueType.AcceptedValues.Length >= 3)
                scen.Value = scen.ValueType.AcceptedValues[(int)n];
        }

        private void LowDownVolume(ScenarioCast[] casts)
        {
            var volumeScenario = casts.FirstOrDefault(x => x.ID == VoicePluginData.Current.VolumeLevelScenarioId);
            if (volumeScenario != null && volumeScenario.Enabled && volumeScenario.CanSet && volumeScenario.ValueType is FloatValueType valueType)
            {
                _prevSoundVolume = volumeScenario.Value;
                var newVal = valueType.Min + (valueType.Max - valueType.Min) * 0.02;
                if (newVal <= double.Parse(volumeScenario.Value))
                    volumeScenario.Value = newVal.ToString();
            }
        }

        private void ReturnVolume(ScenarioCast[] casts)
        {
            if (_prevSoundVolume != null)
            {
                var volumeScenario = casts.FirstOrDefault(x => x.ID == VoicePluginData.Current.VolumeLevelScenarioId);
                if (volumeScenario != null && volumeScenario.Enabled && volumeScenario.CanSet && volumeScenario.ValueType is FloatValueType valueType)
                    volumeScenario.Value = _prevSoundVolume;
                _prevSoundVolume = null;
            }
        }

        private bool HasNumbersExt(string s)
        {
            foreach (char c in s)
                if (Char.IsDigit(c))
                    return true;
            return false;
        }

        private Alternation FindAlternation(ScenarioCast[] scenarios, string source)
        {
            bool containsNumbers = HasNumbersExt(source);
            var sourceForNumber = source;
            
            if (containsNumbers)
                for (int i = 100; i >= 0; i -= 5)
                    sourceForNumber = sourceForNumber.Replace(i.ToString(), Utils.ConvertToNormalString(i));

            var alternations = new List<Alternation>();
            foreach (var scenario in scenarios)
            {
                if (scenario.ValueType is FloatValueType)
                    alternations.AddRange(ForNumeric(WithSynonyms(scenario.Name), scenario, containsNumbers, sourceForNumber));
                else if (scenario.ValueType is ToggleValueType)
                    alternations.AddRange(ForToggle(WithSynonyms(scenario.Name), scenario, source));
                else if (scenario.ValueType is InfoValueType)
                    alternations.AddRange(ForInfo(WithSynonyms(scenario.Name), scenario, source));
                else if (scenario.ValueType is ButtonValueType)
                    alternations.AddRange(ForButton(WithSynonyms(scenario.Name), scenario));
                else if (scenario.ValueType is StateValueType)
                    alternations.AddRange(ForState(WithSynonyms(scenario.Name), scenario));
            }

            Alternation maxAlternation = null;
            double maxConfidence = 0;
            double maxWeight = 0;

            foreach (var alt in alternations)
            {
                var confidence =
                    _comparer.CalculateFuzzyEqualValue(
                        alt.Synonym,
                        alt.Scenario.ValueType is FloatValueType ? sourceForNumber : source);
                if (confidence > maxConfidence || (confidence == maxConfidence && alt.Weight >= maxWeight))
                {
                    maxAlternation = alt;
                    maxConfidence = confidence;
                    maxWeight = alt.Weight;
                }
            }

            if (maxConfidence > 0.7)
                return maxAlternation;
            return null;
        }

        private string[] WithSynonyms(string name)
        {
            name = PrepareString(name);

            var strings = new List<string>();
            strings.Add(name);
            foreach (var synonyms in VoicePluginData.Current.Synonyms)
            {
                if (name.Contains(synonyms.Name))
                {
                    foreach (var synonym in synonyms.Strings)
                        strings.Add(name.Replace(synonyms.Name, PrepareString(synonym)));
                }
            }

            return strings.ToArray();
        }

        private string PrepareString(string str)
        {
            str = str.ToLowerInvariant().Trim();
            foreach (var synonym in StandardSynonyms)
                if (str.Contains(synonym.Item1))
                    str = str.Replace(synonym.Item1, synonym.Item2);
            return str;
        }

        private Alternation[] ForButton(string[] synonyms, ScenarioCast scenario)
        {
            var alternates = new List<Alternation>();
            foreach (var synonym in synonyms)
            {
                alternates.Add(new Alternation(scenario, synonym));
                alternates.Add(new Alternation(scenario, "запустить " + synonym, weight: 90));
                alternates.Add(new Alternation(scenario, "запусти " + synonym, weight: 90));
                alternates.Add(new Alternation(scenario, "включить " + synonym, weight: 90));
                alternates.Add(new Alternation(scenario, "включи " + synonym, weight: 90));
                alternates.Add(new Alternation(scenario, "активировать " + synonym, weight: 90));
                alternates.Add(new Alternation(scenario, "активируй " + synonym, weight: 90));
            }
            return alternates.ToArray();
        }

        private Alternation[] ForToggle(string[] synonyms, ScenarioCast scenario, string source)
        {
            var alternates = new List<Alternation>();
            foreach (var synonym in synonyms)
            {
                alternates.Add(new Alternation(scenario, synonym, (a) => a.Scenario.Value == ToggleValueType.ValueOFF ? ToggleValueType.ValueON : ToggleValueType.ValueOFF));
                alternates.Add(new Alternation(scenario, "запустить " + synonym, (a) => ToggleValueType.ValueON, 90));
                alternates.Add(new Alternation(scenario, "запусти " + synonym, (a) => ToggleValueType.ValueON, 90));
                alternates.Add(new Alternation(scenario, "активировать " + synonym, (a) => ToggleValueType.ValueON, 90));
                alternates.Add(new Alternation(scenario, "активируй " + synonym, (a) => ToggleValueType.ValueON, 90));
                alternates.Add(new Alternation(scenario, "деактивировать " + synonym, (a) => ToggleValueType.ValueOFF, 90));

                // Распознаватель речи считает очень похожими эти фразы, поэтому разделяем их
                if (source.Contains("включ"))
                {
                    alternates.Add(new Alternation(scenario, "включить " + synonym, (a) => ToggleValueType.ValueON));
                    alternates.Add(new Alternation(scenario, "включай " + synonym, (a) => ToggleValueType.ValueON));
                    alternates.Add(new Alternation(scenario, "включи " + synonym, (a) => ToggleValueType.ValueON));
                }
                else if (source.Contains("отключ"))
                {
                    alternates.Add(new Alternation(scenario, "отключить " + synonym, (a) => ToggleValueType.ValueOFF));
                    alternates.Add(new Alternation(scenario, "отключи " + synonym, (a) => ToggleValueType.ValueOFF));
                }
                else if (source.Contains("выключ"))
                {
                    alternates.Add(new Alternation(scenario, "выключить " + synonym, (a) => ToggleValueType.ValueOFF));
                    alternates.Add(new Alternation(scenario, "выключи " + synonym, (a) => ToggleValueType.ValueOFF));
                }
            }
            return alternates.ToArray();
        }

        private Alternation[] ForInfo(string[] synonyms, ScenarioCast scenario, string source)
        {
            string[] prefixes = new[] { "отправить ", "отправь ", "вывести " , "выслать " , "вышли ", ""};

            var alternates = new List<Alternation>();
            foreach (var synonym in synonyms)
            {
                foreach (var prefix in prefixes)
                {
                    var alt = PrepareInfoAlternation(prefix + synonym, source, scenario, prefix == string.Empty ? 100 : 90);
                    if (alt != null)
                        alternates.Add(alt);
                }
            }

            return alternates.ToArray();
        }

        private Alternation PrepareInfoAlternation(string synonym, string source, ScenarioCast scenario, int weight)
        {
            if (synonym.Length < source.Length)
            {
                var wordsCount = synonym.Count(x => x == ' ') + 1;
                var wordsCountSource = source.Count(x => x == ' ') + 1;

                if (wordsCount >= wordsCountSource)
                    return null;

                var sourceWords = source.Split(' ');
                var prefixSource = sourceWords.Take(wordsCount).Aggregate((x1, x2) => x1 + ' ' + x2);

                var equalityNumber = _comparer.CalculateFuzzyEqualValue(synonym, prefixSource);

                if (equalityNumber > 0.6)
                {
                    var parameter =
                        sourceWords
                        .Skip(wordsCount)
                        .Take(wordsCountSource - wordsCount)
                        .Aggregate((x1, x2) => x1 + ' ' + x2);

                    return new Alternation(scenario, synonym + " " + parameter, (a) => parameter, weight);
                }
                else return null;
            }
            return null;
        }

        private Alternation[] ForState(string[] synonyms, ScenarioCast scenario)
        {
            var alternates = new List<Alternation>();
            foreach (var state in scenario.ValueType.AcceptedValues)
            {
                var stateCur = state;
                foreach (var stateSyn in WithSynonyms(stateCur))
                {
                    alternates.Add(new Alternation(scenario, stateSyn, (a) => stateCur, 85));
                    foreach (var synonym in synonyms)
                    {
                        alternates.Add(new Alternation(scenario, synonym + " " + stateSyn, (a) => stateCur));
                        alternates.Add(new Alternation(scenario, "сделать " + synonym + " " + stateSyn, (a) => stateCur, 90));
                    }
                }
            }
            return alternates.ToArray();
        }

        private Alternation[] ForNumeric(string[] synonyms, ScenarioCast scenario, bool containsNumbers, string source)
        {
            var actionOnPlus = new Func<Alternation, string>((a) => {
                var currentValue = a.Scenario.Value;
                if (scenario.ID == VoicePluginData.Current.VolumeLevelScenarioId && _prevSoundVolume != null)
                    currentValue = _prevSoundVolume;
                double.TryParse(currentValue, out double current);
                var valueType = a.Scenario.ValueType as FloatValueType;
                var min = valueType.Min;
                var max = valueType.Max;
                var plus = current + (max - min) * 0.1;
                if (plus > max)
                    plus = max;
                return plus.ToString();
            });

            var actionOnLow = new Func<Alternation, string>((a) => {
                var currentValue = a.Scenario.Value;
                if (scenario.ID == VoicePluginData.Current.VolumeLevelScenarioId && _prevSoundVolume != null)
                    currentValue = _prevSoundVolume;
                double.TryParse(currentValue, out double current);
                var valueType = a.Scenario.ValueType as FloatValueType;
                var min = valueType.Min;
                var max = valueType.Max;
                var low = min + (max - min) * 0.08;
                if (low > current)
                    low = 0;
                return low.ToString();
            });

            var actionOnMinus = new Func<Alternation, string>((a) => {
                var currentValue = a.Scenario.Value;
                if (scenario.ID == VoicePluginData.Current.VolumeLevelScenarioId && _prevSoundVolume != null)
                    currentValue = _prevSoundVolume;
                double.TryParse(currentValue, out double current);
                var valueType = scenario.ValueType as FloatValueType;
                var min = valueType.Min;
                var max = valueType.Max;
                var minus = current - (max - min) * 0.1;
                if (minus < min)
                    minus = min;
                return minus.ToString();
            });

            var actionOnMinimum = new Func<Alternation, string>((a) => {
                var valueType = a.Scenario.ValueType as FloatValueType;
                var min = valueType.Min;
                return min.ToString();
            });

            var actionOnMaximum = new Func<Alternation, string>((a) => {
                var valueType = a.Scenario.ValueType as FloatValueType;
                var max = valueType.Max;
                return max.ToString();
            });

            var alternates = new List<Alternation>();
            foreach (var synonym in synonyms)
            {
                alternates.Add(new Alternation(scenario, "выключить " + synonym, actionOnMinimum));
                alternates.Add(new Alternation(scenario, "отключить " + synonym, actionOnMinimum));
                alternates.Add(new Alternation(scenario, "максимум " + synonym, actionOnMaximum));
                alternates.Add(new Alternation(scenario, "минимум " + synonym, actionOnLow));
                alternates.Add(new Alternation(scenario, "тихо " + synonym, actionOnLow, 90));
                alternates.Add(new Alternation(scenario, "тише " + synonym, actionOnMinus, 90));
                alternates.Add(new Alternation(scenario, "сделай тише " + synonym, actionOnMinus, 90));
                alternates.Add(new Alternation(scenario, "убавь " + synonym, actionOnMinus, 90));
                alternates.Add(new Alternation(scenario, "громче " + synonym, actionOnPlus, 90));
                alternates.Add(new Alternation(scenario, "сделай громче " + synonym, actionOnPlus, 90));
                alternates.Add(new Alternation(scenario, "прибавь " + synonym, actionOnPlus, 90));
                alternates.Add(new Alternation(scenario, "сделай ярче " + synonym, actionOnPlus, 90));
                alternates.Add(new Alternation(scenario, "ярче " + synonym, actionOnPlus, 90));
                alternates.Add(new Alternation(scenario, "тусклее " + synonym, actionOnMinus, 90));

                alternates.Add(new Alternation(scenario, "сделай тихо " + synonym, actionOnLow, 90));
                alternates.Add(new Alternation(scenario, "сделай минимум " + synonym, actionOnLow, 90));
                alternates.Add(new Alternation(scenario, "сделай тише " + synonym, actionOnMinus, 90));
                alternates.Add(new Alternation(scenario, "сделай громче " + synonym, actionOnPlus, 90));
                alternates.Add(new Alternation(scenario, "сделай ярче " + synonym, actionOnPlus, 90));
                alternates.Add(new Alternation(scenario, "сделай тусклее " + synonym, actionOnMinus, 90));

                if (scenario.Name.ToLowerInvariant() == "звук")
                {
                    alternates.Add(new Alternation(scenario, "тихо", actionOnLow));
                    alternates.Add(new Alternation(scenario, "сделай тихо", actionOnLow));
                    alternates.Add(new Alternation(scenario, "тише", actionOnMinus));
                    alternates.Add(new Alternation(scenario, "сделай тише", actionOnMinus));
                    alternates.Add(new Alternation(scenario, "громче", actionOnPlus));
                    alternates.Add(new Alternation(scenario, "прибавь громкость", actionOnPlus));
                    alternates.Add(new Alternation(scenario, "прибавь музыку", actionOnPlus));
                    alternates.Add(new Alternation(scenario, "убавь громкость", actionOnMinus));
                    alternates.Add(new Alternation(scenario, "убавь музыку", actionOnMinus));
                    alternates.Add(new Alternation(scenario, "сделай громче", actionOnPlus));
                }

                // Два похожих слова, алгоритм может ошибиться в этом моменте
                if (source.Contains("прибавить"))
                    alternates.Add(new Alternation(scenario, "прибавить " + synonym, actionOnPlus));
                else
                    alternates.Add(new Alternation(scenario, "убавить " + synonym, actionOnMinus));

                alternates.Add(new Alternation(scenario, synonym + " ноль", (a) => "0"));
                alternates.Add(new Alternation(scenario, synonym + " один", (a) => "1"));
                alternates.Add(new Alternation(scenario, synonym + " два", (a) => "2"));
                alternates.Add(new Alternation(scenario, synonym + " три", (a) => "3"));
                alternates.Add(new Alternation(scenario, synonym + " четрые", (a) => "4"));
                alternates.Add(new Alternation(scenario, synonym + " пять", (a) => "5"));
                alternates.Add(new Alternation(scenario, synonym + " шесть", (a) => "6"));
                alternates.Add(new Alternation(scenario, synonym + " семь", (a) => "7"));
                alternates.Add(new Alternation(scenario, synonym + " восемь", (a) => "8"));
                alternates.Add(new Alternation(scenario, synonym + " девять", (a) => "9"));

                if (containsNumbers)
                    for (int i = 0; i <= 100; i += 5)
                    {
                        var iCur = i;
                        var actionOnPercent = new Func<Alternation, string>((a) => {
                            var valueType = a.Scenario.ValueType as FloatValueType;
                            var min = valueType.Min;
                            var max = valueType.Max;
                            var val = (min + (max - min) * iCur / 100.0).ToString();
                            return val;
                        });
                        var asString = Utils.ConvertToNormalString(i);
                        if (source.Contains(asString))
                        {
                            alternates.Add(new Alternation(scenario, "выставить " + synonym + " " + asString, actionOnPercent));
                            alternates.Add(new Alternation(scenario, "выставить " + synonym + " на " + asString, actionOnPercent));
                            alternates.Add(new Alternation(scenario, "яркость " + synonym + " " + asString, actionOnPercent, 90));
                            alternates.Add(new Alternation(scenario, "яркость " + synonym + " на " + asString, actionOnPercent, 90));
                            alternates.Add(new Alternation(scenario, "уровень " + synonym + " " + asString, actionOnPercent));
                            alternates.Add(new Alternation(scenario, "уровень " + synonym + " на " + asString, actionOnPercent));
                            alternates.Add(new Alternation(scenario, synonym + " на " + asString, actionOnPercent));
                        }
                    }
            }

            return alternates.ToArray();
        }

        public class Alternation
        {
            public Func<Alternation, string> GenerateValue { get; set; }

            public string Synonym { get; set; }

            public int Weight { get; set; }

            public ScenarioCast Scenario { get; private set; }

            public Alternation(ScenarioCast scenario, string syn, Func<Alternation, string> generateValue = null, int weight = 100)
            {
                Weight = weight;
                Scenario = scenario;
                Synonym = syn;
                GenerateValue = generateValue ?? new Func<Alternation, string>((a) => string.Empty);
            }
            
            public string Execute()
            {
                var result = GenerateValue(this);
                Scenario.Value = result;
                return result;
            }
        }

        public class After
        {
            private static List<Timer> _timersPool = new List<Timer>();

            public After(uint value, TimeUnit unit = TimeUnit.Second)
            {
                Value = value;
                Unit = unit;
            }

            public uint Value { get; set; } = 0;

            public TimeUnit Unit { get; set; }

            public uint GetAsUnit()
            {
                var seconds = Value;
                switch (Unit)
                {
                    case TimeUnit.Minute: seconds = Value * 60; break;
                    case TimeUnit.Hour: seconds = Value * 60 * 60; break;
                }
                return seconds;
            }

            public void Execute(Alternation alternation)
            {
                if (Value > 0)
                {
                    var seconds = GetAsUnit();
                    Timer timer = null;
                    timer = new Timer(
                        (s) =>
                        {
                            _timersPool.Remove(timer);
                            alternation.Execute();
                        },
                        null, seconds * 1000, Timeout.Infinite);
                    _timersPool.Add(timer);

                }
                else alternation.Execute();
            }
        }

        public enum TimeUnit
        {
            Second,
            Minute,
            Hour
        }

        public enum SoundNotification
        {
            OK = 0,
            Apply = 1,
            Cancel = 2
        }
    }
}
