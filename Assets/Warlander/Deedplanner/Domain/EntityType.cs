namespace Warlander.Deedplanner.Domain
{
    public enum EntityType
    {
        Ground, Floorroof, Hwall, Hfence, Hborder, Vwall, Vfence, Vborder, Object, Cave, Label, BridgePart, Dock
    }

    public static class EntityTypeUtils
    {
        public static bool IsVerticalTileBorder(this EntityType type)
        {
            return type == EntityType.Vwall || type == EntityType.Vfence || type == EntityType.Vborder;
        }
        
        public static bool IsHorizontalTileBorder(this EntityType type)
        {
            return type == EntityType.Hwall || type == EntityType.Hfence || type == EntityType.Hborder;
        }
    }
}
