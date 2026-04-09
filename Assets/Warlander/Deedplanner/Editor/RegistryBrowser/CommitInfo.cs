using System;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public readonly struct CommitInfo
    {
        public string Sha { get; }
        public string ShortSha { get; }
        public string Message { get; }
        public string[] Tags { get; }

        public CommitInfo(string sha, string message, string[] tags)
        {
            Sha = sha;
            ShortSha = sha != null && sha.Length >= 7 ? sha.Substring(0, 7) : sha ?? "";
            Message = message ?? "";
            Tags = tags ?? Array.Empty<string>();
        }

        public string DisplayLabel
        {
            get
            {
                string tagSuffix = Tags.Length > 0
                    ? " [" + string.Join(", ", Tags) + "]"
                    : "";
                return $"{ShortSha} {Message}{tagSuffix}";
            }
        }
    }
}
