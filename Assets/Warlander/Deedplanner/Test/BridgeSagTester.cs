using System.Text;
using UnityEngine;
using Warlander.Deedplanner.Data.Bridges;

namespace Warlander.Deedplanner.Test
{
    public class BridgeSagTester : MonoBehaviour
    {
        [SerializeField] private int bridgeLength = 10;
        [SerializeField] private int startHeight = 40;
        [SerializeField] private int endHeight = 40;
        
        private void Awake()
        {
            StringBuilder build = new StringBuilder();
            build.AppendLine("<b>Bridge Sag Tester</b>");
            build.AppendLine();

            build.AppendLine("Rope Bridge Sag 3:");
            RopeBridgeType ropeBridgeType = new RopeBridgeType();
            for (int i = 0; i < bridgeLength; i++)
            {
                float currentPartPercent = (float) i / (bridgeLength - 1);
                int baseHeight = (int) (startHeight * (1 - currentPartPercent) + endHeight * currentPartPercent);
                int addedHeight = ropeBridgeType.CalculateAddedHeight(i, bridgeLength, startHeight, endHeight, 3);
                int totalHeight = baseHeight + addedHeight;
                build.Append("Base: ").Append(baseHeight);
                build.Append("\tSag: ").Append(addedHeight);
                build.Append("\tTotal: ").Append(totalHeight).AppendLine();
            }
            
            Debug.Log(build.ToString());
        }
    }
}