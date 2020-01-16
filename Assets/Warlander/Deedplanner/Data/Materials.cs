using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Warlander.Deedplanner.Data
{
    public sealed class Materials : Dictionary<string, int>
    {
        public Materials()
        { }

        public Materials(XmlNode node)
        {
            string content = node.InnerText;
            string[] materials = content.Split(',');
            foreach (string material in materials)
            {
                string[] parts = material.Split('=');
                string name = parts[0].Trim();
                int count = int.Parse(parts[1].Trim());
                this[name] = count;
            }
        }

        public void Add(Materials materials)
        {
            if (materials == null)
            {
                return;
            }

            foreach (KeyValuePair<string, int> entry in materials)
            {
                Add(entry.Key, entry.Value);
            }
        }

        public new void Add(string name, int count)
        {
            if (ContainsKey(name))
            {
                int newCount = this[name] + count;
                Remove(name);
                base.Add(name, newCount);
            }
            else
            {
                base.Add(name, count);
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Materials needed:");
            builder.AppendLine();

            foreach (KeyValuePair<string, int> entry in this)
            {
                builder.Append(entry.Key).Append(" = ").AppendLine(entry.Value.ToString());
            }
            if (Count == 0)
            {
                builder.AppendLine("None");
            }

            return builder.ToString();
        }
    }
}
