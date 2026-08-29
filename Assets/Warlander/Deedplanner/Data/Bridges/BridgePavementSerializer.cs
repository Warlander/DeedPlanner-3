using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class BridgePavementSerializer
    {
        private readonly IDataCatalog _dataCatalog;
        private readonly ICategoryLogger _logger;

        public const string NoneToken = "none";

        public BridgePavementSerializer(IDataCatalog dataCatalog, ICategoryLogger logger)
        {
            _dataCatalog = dataCatalog;
            _logger = logger;
        }

        // Segments are comma-separated, lanes within a segment slash-separated.
        // Single-lane bridges produce the same flat "pcs,none,..." shape as a result.
        // Returns null when nothing is paved, so the attribute is only written when needed.
        public string Encode(IEnumerable<BridgePart> parts)
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

        public BridgePavementData[,] Decode(string serialized, int segmentCount, int laneCount)
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

                    BridgePavementData pavement = _dataCatalog.GetBridgePavement(token);
                    if (pavement == null)
                    {
                        _logger.Warning("Unknown bridge pavement token: " + token);
                    }

                    pavements[segment, lane] = pavement;
                }
            }

            return pavements;
        }
    }
}
