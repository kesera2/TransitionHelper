using System.Collections;
using System.Collections.Generic;

namespace dev.kesera2.transition_helper
{
    [System.Serializable]
    internal class LocalizationData
    {
        public List<LocalizedEntry> entries;

        public Dictionary<string, string> ToDictionary()
        {
            if (entries == null)
            {
                entries = new List<LocalizedEntry>();
            }
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (var entry in entries)
            {
                dict[entry.key] = entry.value;
            }

            return dict;
        }
    }
    [System.Serializable]
    internal class LocalizedEntry
    {
        public string key;
        public string value;
    }
}