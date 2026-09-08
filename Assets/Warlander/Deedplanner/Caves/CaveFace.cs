namespace Warlander.Deedplanner.Caves
{
    public enum CaveFaceKind
    {
        Floor,
        Ceiling,
        Wall,
        MapEdgeCap,
        SolidSurface
    }

    public readonly struct CaveFace
    {
        public CaveFaceKind Kind { get; }
        public int OpenCellX { get; }
        public int OpenCellY { get; }
        public int SolidOwnerX { get; }
        public int SolidOwnerY { get; }
        public CaveEdge Edge { get; }
        public bool HasSolidOwner { get; }
        public int Revision { get; }

        public CaveFace(CaveFaceKind kind, int openCellX, int openCellY, int revision)
        {
            Kind = kind;
            OpenCellX = openCellX;
            OpenCellY = openCellY;
            SolidOwnerX = 0;
            SolidOwnerY = 0;
            Edge = CaveEdge.South;
            HasSolidOwner = false;
            Revision = revision;
        }

        public CaveFace(CaveFaceKind kind, int openCellX, int openCellY, int solidOwnerX, int solidOwnerY,
            CaveEdge edge, int revision)
        {
            Kind = kind;
            OpenCellX = openCellX;
            OpenCellY = openCellY;
            SolidOwnerX = solidOwnerX;
            SolidOwnerY = solidOwnerY;
            Edge = edge;
            HasSolidOwner = true;
            Revision = revision;
        }

        public bool IsSameLogicalFace(CaveFace other)
        {
            return Kind == other.Kind
                   && OpenCellX == other.OpenCellX
                   && OpenCellY == other.OpenCellY
                   && SolidOwnerX == other.SolidOwnerX
                   && SolidOwnerY == other.SolidOwnerY
                   && Edge == other.Edge
                   && HasSolidOwner == other.HasSolidOwner
                   && Revision == other.Revision;
        }
    }
}
