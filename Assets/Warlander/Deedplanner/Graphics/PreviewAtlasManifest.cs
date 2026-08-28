using System;
using System.Collections.Generic;

namespace Warlander.Deedplanner.Graphics
{
    [Serializable]
    public class PreviewAtlasManifest
    {
        public int generatorVersion;
        public int cellSize;
        public int columns;
        public string category;
        public string inputsHash;
        public List<PreviewAtlasEntry> entries = new List<PreviewAtlasEntry>();
    }
}
