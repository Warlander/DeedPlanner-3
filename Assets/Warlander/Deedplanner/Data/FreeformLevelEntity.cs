using UnityEngine;

namespace Warlander.Deedplanner.Data
{
    public abstract class FreeformLevelEntity : LevelEntity
    {
        public abstract Vector2 Position { get; }
        public abstract bool AlignToSlope { get; }
    }
}