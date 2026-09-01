namespace Warlander.Deedplanner.Ui.Tooltips
{
    /// <summary>
    /// Absolute corner heights in row-major order (top row first), Size*Size entries.
    /// Size 3 renders as differences from the center cell; size 2 renders absolute values.
    /// </summary>
    public readonly struct SlopeGridData
    {
        public int Size { get; }
        public int[] Heights { get; }

        public SlopeGridData(int size, int[] heights)
        {
            Size = size;
            Heights = heights;
        }
    }
}
