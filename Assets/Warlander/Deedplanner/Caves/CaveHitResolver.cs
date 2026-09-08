namespace Warlander.Deedplanner.Caves
{
    public sealed class CaveHitResolver : ICaveHitResolver
    {
        public bool TryResolve(CaveChunk chunk, int triangleIndex, out CaveHit hit)
        {
            if (chunk == null || !chunk.TryGetFace(triangleIndex, out CaveFace face))
            {
                hit = default;
                return false;
            }

            switch (face.Kind)
            {
                case CaveFaceKind.Floor:
                case CaveFaceKind.Ceiling:
                case CaveFaceKind.SolidSurface:
                    hit = new CaveHit(chunk, triangleIndex, face, face.OpenCellX, face.OpenCellY);
                    return true;
                case CaveFaceKind.Wall when face.HasSolidOwner:
                    hit = new CaveHit(chunk, triangleIndex, face, face.SolidOwnerX, face.SolidOwnerY);
                    return true;
                default:
                    hit = default;
                    return false;
            }
        }

        public bool IsCurrent(CaveHit hit)
        {
            return hit.Chunk != null
                   && hit.Chunk.ColliderRevision == hit.ColliderRevision
                   && hit.Chunk.TryGetFace(hit.TriangleIndex, out CaveFace face)
                   && face.Revision == hit.ColliderRevision;
        }
    }
}
