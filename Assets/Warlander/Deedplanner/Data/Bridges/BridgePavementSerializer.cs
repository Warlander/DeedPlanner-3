using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Warlander.Deedplanner.Data.Bridges
{
    public static class BridgePavementSerializer
    {
        public const string NoneToken = "none";

        // Segments are comma-separated, lanes within a segment slash-separated.
        // Single-lane bridges produce the same flat "pcs,none,..." shape as a result.
        // Returns null when nothing is paved, so the attribute is only written when needed.
        public static string Encode(IEnumerable<BridgePart> parts)
        {
            IGrouping<int, BridgePart>[] segments = parts
                .OrderBy(part => part.SegmentIndex)
                .ThenBy(part => part.LaneIndex)
                .GroupBy(part => part.SegmentIndex)
                .ToArray();

            if (segments.All(segment => segment.All(part => part.Pavement == null)))
            {
                return null;
            }

            return string.Join(",", segments.Select(segment =>
                string.Join("/", segment.Select(part => part.Pavement?.Token ?? NoneToken))));
        }

        public static BridgePavementData[,] Decode(string serialized, int segmentCount, int laneCount)
        {
            BridgePavementData[,] pavements = new BridgePavementData[segmentCount, laneCount];
            string[] segmentTokens = serialized.Split(',');
            for (int segment = 0; segment < segmentCount && segment < segmentTokens.Length; segment++)
            {
                string[] laneTokens = segmentTokens[segment].Split('/');
                for (int lane = 0; lane < laneCount && lane < laneTokens.Length; lane++)
                {
                    string token = laneTokens[lane].Trim();
                    if (token == NoneToken)
                    {
                        continue;
                    }

                    if (!Database.BridgePavements.TryGetValue(token, out BridgePavementData pavement))
                    {
                        Debug.LogWarning("Unknown bridge pavement token: " + token);
                    }

                    pavements[segment, lane] = pavement;
                }
            }

            return pavements;
        }
    }
}
