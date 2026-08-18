using System.Linq;
using UnityEngine;

namespace Warlander.Deedplanner.Data.Bridges
{
    public static class BridgePavementSerializer
    {
        public const string NoneToken = "none";

        // Returns null when nothing is paved, so the attribute is only written when needed.
        public static string Encode(BridgePavementData[] pavements)
        {
            if (pavements.All(pavement => pavement == null))
            {
                return null;
            }

            return string.Join(",", pavements.Select(pavement => pavement?.Token ?? NoneToken));
        }

        public static BridgePavementData[] Decode(string serialized, int segmentCount)
        {
            BridgePavementData[] pavements = new BridgePavementData[segmentCount];
            string[] tokens = serialized.Split(',');
            for (int i = 0; i < segmentCount && i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                if (token == NoneToken)
                {
                    continue;
                }

                BridgePavementData pavement = null;
                if (!Database.BridgePavements.TryGetValue(token, out pavement))
                {
                    Debug.LogWarning("Unknown bridge pavement token: " + token);
                }

                pavements[i] = pavement;
            }

            return pavements;
        }
    }
}
