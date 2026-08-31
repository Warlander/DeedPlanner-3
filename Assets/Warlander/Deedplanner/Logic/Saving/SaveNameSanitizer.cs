using System.Text;

namespace Warlander.Deedplanner.Logic.Saving
{
    public class SaveNameSanitizer : ISaveNameSanitizer
    {
        public string Sanitize(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Untitled";
            }

            var builder = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                builder.Append(char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' ? c : '_');
            }

            string sanitized = builder.ToString().Trim();
            if (sanitized.Length > 64)
            {
                sanitized = sanitized.Substring(0, 64);
            }

            return sanitized.Length > 0 ? sanitized : "Untitled";
        }
    }
}
