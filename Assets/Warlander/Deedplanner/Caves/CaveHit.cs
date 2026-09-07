namespace Warlander.Deedplanner.Caves
{
    public readonly struct CaveHit
    {
        public CaveChunk Chunk { get; }
        public int TriangleIndex { get; }
        public int ColliderRevision { get; }
        public CaveFaceKind Kind { get; }
        public int CellX { get; }
        public int CellY { get; }
        public int OpenCellX { get; }
        public int OpenCellY { get; }
        public CaveEdge Edge { get; }
        public bool HasEdge => Kind == CaveFaceKind.Wall;

        internal CaveHit(CaveChunk chunk, int triangleIndex, CaveFace face, int cellX, int cellY)
        {
            Chunk = chunk;
            TriangleIndex = triangleIndex;
            ColliderRevision = face.Revision;
            Kind = face.Kind;
            CellX = cellX;
            CellY = cellY;
            OpenCellX = face.OpenCellX;
            OpenCellY = face.OpenCellY;
            Edge = face.Edge;
        }
    }
}
