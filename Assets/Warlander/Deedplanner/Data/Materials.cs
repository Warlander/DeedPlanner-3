using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Warlander.Deedplanner.Data
{
    public sealed class Materials
    {
        private readonly Dictionary<string, int> _entries;

        public Materials()
        {
            _entries = new Dictionary<string, int>();
        }

        public Materials(XmlNode node)
        {
            string content = node.InnerText;
            string[] materials = content.Split(',');
            foreach (string material in materials)
            {
                string[] parts = material.Split('=');
                string name = parts[0].Trim();
                int count = int.Parse(parts[1].Trim());
                _entries[name] = count;
            }
        }

        public void Add(Materials materials)
        {
            if (materials == null)
            {
                return;
            }

            foreach (KeyValuePair<string, int> entry in materials._entries)
            {
                Add(entry.Key, entry.Value);
            }
        }

        public void Add(string name, int count)
        {
            _entries.TryGetValue(name, out int existing);
            _entries[name] = existing + count;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Materials needed:");
            builder.AppendLine();

            foreach (KeyValuePair<string, int> entry in _entries)
            {
                builder.Append(entry.Key).Append(" = ").AppendLine(entry.Value.ToString());
            }
            if (_entries.Count == 0)
            {
                builder.AppendLine("None");
            }

            return builder.ToString();
        }
    }
}
