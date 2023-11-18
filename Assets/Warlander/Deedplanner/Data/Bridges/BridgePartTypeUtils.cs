using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlander.Deedplanner.Data.Bridges
{
    public static class BridgePartTypeUtils
    {
        private static Dictionary<char, BridgePartType> charToBridgePartTypes = new Dictionary<char, BridgePartType>();

        static BridgePartTypeUtils()
        {
            BridgePartType[] types = Enum.GetValues(typeof(BridgePartType)).Cast<BridgePartType>().ToArray();

            foreach (BridgePartType bridgePartType in types)
            {
                charToBridgePartTypes[bridgePartType.ToChar()] = bridgePartType;
            }
        }

        public static BridgePartType FromChar(char c)
        {
            return charToBridgePartTypes[c];
        }
        
        public static char ToChar(this BridgePartType type)
        {
            switch (type)
            {
                case BridgePartType.Floating:
                    return 'f';
                case BridgePartType.Abutment:
                    return 'a';
                case BridgePartType.Bracing:
                    return 'b';
                case BridgePartType.Crown:
                    return 'c';
                case BridgePartType.DoubleBracing:
                    return 'd';
                case BridgePartType.DoubleAbutment:
                    return 'e';
                case BridgePartType.Support:
                    return 's';
                case BridgePartType.Extension:
                    return '\n'; // It should never appear in actual save files
            }
            
            throw new ArgumentException("No letter for bridge part type: " + type);
        }

        public static string ToHumanFriendlyName(this BridgePartType type)
        {
            switch (type)
            {
                case BridgePartType.Floating:
                    return "floating";
                case BridgePartType.Abutment:
                    return "abutment";
                case BridgePartType.Bracing:
                    return "bracing";
                case BridgePartType.Crown:
                    return "crown";
                case BridgePartType.DoubleBracing:
                    return "double bracing";
                case BridgePartType.DoubleAbutment:
                    return "double abutment";
                case BridgePartType.Support:
                    return "support";
                case BridgePartType.Extension:
                    return "extension";
            }
            
            throw new ArgumentException("No name for bridge part type: " + type);
        }

        public static int GetSupportedDistanceFromSupport(this BridgePartType type)
        {
            switch (type)
            {
                case BridgePartType.Floating:
                    return 1;
                case BridgePartType.Abutment: case BridgePartType.DoubleAbutment:
                    return 1;
                case BridgePartType.Bracing: case BridgePartType.DoubleBracing:
                    return 2;
                case BridgePartType.Crown:
                    return 3;
                case BridgePartType.Support:
                    return 0;
                case BridgePartType.Extension:
                    throw new ArgumentException("Extension cannot support distance from support. If it's included in calculations somewhere, that's a bug.");
                default:
                    throw new ArgumentException($"Unknown distance from support for part: {type}.");
            }
        }

        public static BridgePartType[] DecodeSegments(string segmentsString)
        {
            return segmentsString.ToCharArray().Select(FromChar).ToArray();
        }

        public static string EncodeSegments(BridgePartType[] segments)
        {
            return new string(segments.Select(ToChar).ToArray());
        }
    }
}