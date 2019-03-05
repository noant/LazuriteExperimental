using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LazuriteVoicePlugin
{
    public class ScenarioSynonyms
    {
        public ScenarioSynonyms(string name, List<string> strings)
        {
            Name = name;
            Strings = strings;
        }

        public string Name { get; private set; }

        public List<string> Strings { get; private set; } = new List<string>();
    }
}
