using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warlander.Deedplanner.Gui.Tooltips
{
    public class SlopeGridView : TooltipContentBlock
    {
        private const int MaxCells = 9;
        private const int StrongSlopeThreshold = 15;

        // Grid keeps the same total size for 2x2 and 3x3; cells scale to fill it.
        private static readonly Vector2 CellSize3 = new Vector2(44, 24);
        private static readonly Vector2 CellSize2 = new Vector2(67, 37);

        private static readonly Color NeutralCell = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color CenterCell = new Color(1f, 1f, 1f, 0.14f);
        private static readonly Color ClimbLight = new Color(0.86f, 0.24f, 0.2f, 0.28f);
        private static readonly Color ClimbStrong = new Color(0.86f, 0.24f, 0.2f, 0.5f);
        private static readonly Color DropLight = new Color(0.24f, 0.71f, 0.35f, 0.28f);
        private static readonly Color DropStrong = new Color(0.24f, 0.71f, 0.35f, 0.5f);

        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private Image[] cellBackgrounds;
        [SerializeField] private TMP_Text[] cellLabels;

        public void SetData(SlopeGridData data)
        {
            int cellCount = data.Size * data.Size;
            gridLayout.constraintCount = data.Size;
            gridLayout.cellSize = data.Size == 3 ? CellSize3 : CellSize2;

            for (int i = 0; i < MaxCells; i++)
            {
                bool used = i < cellCount;
                cellBackgrounds[i].gameObject.SetActive(used);
            }

            if (data.Size == 3)
            {
                SetCenteredCells(data.Heights);
            }
            else
            {
                SetAbsoluteCells(data.Heights, cellCount);
            }
        }

        private void SetCenteredCells(int[] heights)
        {
            const int centerIndex = 4;
            int centerHeight = heights[centerIndex];

            for (int i = 0; i < MaxCells; i++)
            {
                if (i == centerIndex)
                {
                    cellBackgrounds[i].color = CenterCell;
                    cellLabels[i].text = centerHeight.ToString();
                    cellLabels[i].fontStyle = FontStyles.Bold;
                    continue;
                }

                int diff = heights[i] - centerHeight;
                cellBackgrounds[i].color = ColorForDiff(diff);
                cellLabels[i].text = diff > 0 ? "+" + diff : diff.ToString();
                cellLabels[i].fontStyle = FontStyles.Normal;
            }
        }

        private void SetAbsoluteCells(int[] heights, int cellCount)
        {
            int min = int.MaxValue;
            int max = int.MinValue;
            for (int i = 0; i < cellCount; i++)
            {
                min = Mathf.Min(min, heights[i]);
                max = Mathf.Max(max, heights[i]);
            }

            for (int i = 0; i < cellCount; i++)
            {
                float t = max > min ? (float)(heights[i] - min) / (max - min) : 0f;
                cellBackgrounds[i].color = t < 0.5f
                    ? Color.Lerp(DropStrong, NeutralCell, t * 2f)
                    : Color.Lerp(NeutralCell, ClimbStrong, (t - 0.5f) * 2f);
                cellLabels[i].text = heights[i].ToString();
                cellLabels[i].fontStyle = FontStyles.Normal;
            }
        }

        private static Color ColorForDiff(int diff)
        {
            if (diff == 0)
            {
                return NeutralCell;
            }
            bool strong = Mathf.Abs(diff) >= StrongSlopeThreshold;
            return diff > 0 ? (strong ? ClimbStrong : ClimbLight) : (strong ? DropStrong : DropLight);
        }
    }
}
