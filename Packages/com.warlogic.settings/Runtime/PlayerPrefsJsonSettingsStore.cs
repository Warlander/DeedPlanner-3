using System;
using System.Collections.Generic;
using UnityEngine;

namespace Warlogic.Settings
{
    public sealed class PlayerPrefsJsonSettingsStore : ISettingsStore
    {
        public const string DefaultPlayerPrefsKey = "warlogic.settings";

        private readonly string _playerPrefsKey;
        private readonly Dictionary<string, string> _values;

        public PlayerPrefsJsonSettingsStore(string playerPrefsKey = DefaultPlayerPrefsKey)
        {
            _playerPrefsKey = playerPrefsKey;
            _values = LoadDocument(playerPrefsKey);
        }

        public bool TryLoad(string key, out string value)
        {
            return _values.TryGetValue(key, out value);
        }

        public void Save(string key, string value)
        {
            _values[key] = value;
            PlayerPrefs.SetString(_playerPrefsKey, SerializeDocument());
            PlayerPrefs.Save();
        }

        private static Dictionary<string, string> LoadDocument(string playerPrefsKey)
        {
            var values = new Dictionary<string, string>();
            string json = PlayerPrefs.GetString(playerPrefsKey, null);
            if (string.IsNullOrEmpty(json))
            {
                return values;
            }
            Document document;
            try
            {
                document = JsonUtility.FromJson<Document>(json);
            }
            catch (Exception)
            {
                return values;
            }
            if (document?.Entries == null)
            {
                return values;
            }
            foreach (Entry entry in document.Entries)
            {
                values[entry.Key] = entry.Value;
            }
            return values;
        }

        private string SerializeDocument()
        {
            var document = new Document();
            foreach (KeyValuePair<string, string> pair in _values)
            {
                document.Entries.Add(new Entry { Key = pair.Key, Value = pair.Value });
            }
            return JsonUtility.ToJson(document);
        }

        [Serializable]
        private class Entry
        {
            public string Key;
            public string Value;
        }

        [Serializable]
        private class Document
        {
            public List<Entry> Entries = new List<Entry>();
        }
    }
}
