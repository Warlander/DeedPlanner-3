using System;

namespace Warlander.Deedplanner.Data.Bridges
{
    public static class BridgeDefaults
    {
        public const int MinLength = 1;
        public const int MaxLength = 38;

        private static readonly string[] WoodenArched = new string[]
        {
            "aa",
            "aca",
            "acca",
            "accca",
            "acccca",
            "accccca",
            "acccccca",
            "accccccca",
            "acccccccca",
            "accccccccca",
            "aasaccccasaa",
            "aasacccccasaa",
            "aasaccccccasaa",
            "aasacccccccasaa",
            "acasaccccccasaca",
            "acasacccccccasaca",
            "acasaccccccccasaca",
            "aasaasacccccasaasaa",
            "aasaasaccccccasaasaa",
            "aasaasacccccccasaasaa",
            "aasaasaccccccccasaasaa",
            "aasaasacccccccccasaasaa",
            "aasaasaccccccccccasaasaa",
            "aasaasacccccccccccasaasaa",
            "aasaasaccccccccccccasaasaa",
            "aasaasacccccccccccccasaasaa",
            "acasaasaccccccccccccasaasaca",
            "acasacasacccccccccccasacasaca",
            "acasacasaccccccccccccasacasaca",
            "acasacasacccccccccccccasacasaca",
            "aasaasaasaccccccccccccasaasaasaa",
            "aasaasaasacccccccccccccasaasaasaa",
            "aasaasaasaccccccccccccccasaasaasaa",
            "acasaasaasacccccccccccccasaasaasaca",
            "acasaasaasaccccccccccccccasaasaasaca",
            "acasacasaasacccccccccccccasaasacasaca",
            "acasacasaasaccccccccccccccasaasacasaca",
        };

        private static readonly string[] BrickArched = new string[]
        {
            "aa",
            "ada",
            "abba",
            "abfba",
            "abffba",
            "abfffba",
            "abffffba",
            "esabfbase",
            "esabffbase",
            "esabfffbase",
            "esabffffbase",
            "esabfffffbase",
            "esabffffffbase",
            "esabfffffffbase",
            "esabffffffffbase",
            "esabfffffffffbase",
            "esabffffffffffbase",
            "esabfffffffffffbase",
            "esabffffffffffffbase",
            "esabfffffffffffffbase",
            "esabffffffffffffffbase",
            "esabfffffffffffffffbase",
            "esabffffffffffffffffbase",
            "esabfffffffffffffffffbase",
            "esabffffffffffffffffffbase",
            "esabfffffffffffffffffffbase",
            "esabffffffffffffffffffffbase",
            "esabfffffffffffffffffffffbase",
            "esabffffffffffffffffffffffbase",
            "esabfffffffffffffffffffffffbase",
            "esabffffffffffffffffffffffffbase",
            "esabfffffffffffffffffffffffffbase",
            "esabffffffffffffffffffffffffffbase",
            "esabfffffffffffffffffffffffffffbase",
            "esabffffffffffffffffffffffffffffbase",
            "esabfffffffffffffffffffffffffffffbase",
            "esabffffffffffffffffffffffffffffffbase",
        };

        private static readonly string[] FlatBridges = new string[]
        {
            "e",
            "aa",
            "ada",
            "abba",
            "abcba",
            "esaase",
            "esadase",
            "esabbase",
            "esabcbase",
            "aasabbasaa",
            "aasabcbasaa",
            "adasabbasada",
            "adasabcbasada",
            "abbasabbasabba",
            "abbasabcbasabba",
            "abcbasabbasabcba",
            "abcbasabcbasabcba",
            "esabcbasaasabcaase",
            "esabcbasadasabcaase",
            "esabcbasabbasabcaase",
            "esabcbasabcbasabcaase",
            "aasabcbasabbasabcaasaa",
            "aasabcbasabcbasabcaasaa",
            "adasabcbasabbasabcaasada",
            "adasabcbasabcbasabcaasada",
            "abbasabcbasabbasabcaasabba",
            "abbasabcbasabcbasabcaasabba",
            "abcbasabcbasabbasabcaasabcba",
            "abcbasabcbasabcbasabcaasabcba",
            "esabcbasabcbasaasabcbasabcbase",
            "esabcbasabcbasadasabcbasabcbase",
            "esabcbasabcbasabbasabcbasabcbase",
            "esabcbasabcbasabcbasabcbasabcbase",
            "aasabcbasabcbasabcbasabbasabcbasaa",
            "aasabcbasabcbasabcbasabcbasabcbasaa",
            "adasabcbasabcbasabcbasabbasabcbasada",
            "adasabcbasabcbasabcbasabcbasabcbasada",
            "abbasabcbasabcbasabcbasabbasabcbasabba",
        };

        public static string GetDefaultSegments(BridgeType type, BridgeData material, int length)
        {
            if (length < MinLength || length > MaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(length),
                    $"Bridge length must be between {MinLength} and {MaxLength}.");
            }

            switch (type)
            {
                case BridgeType.Rope:
                    return GetRopeSegments(length);
                case BridgeType.Flat:
                    return GetFlatSegments(material, length);
                case BridgeType.Arched:
                    return GetArchedSegments(material, length);
                default:
                    throw new ArgumentException("Unknown bridge type: " + type);
            }
        }

        private static string GetRopeSegments(int length)
        {
            if (length == 1)
            {
                return "e";
            }

            return "a" + new string('c', length - 2) + "a";
        }

        private static string GetFlatSegments(BridgeData material, int length)
        {
            string segments = FlatBridges[length - 1];

            if (material.Name == "wood")
            {
                segments = segments
                    .Replace('b', 'c')
                    .Replace('d', 'c')
                    .Replace('e', 'c');
            }

            return segments;
        }

        private static string GetArchedSegments(BridgeData material, int length)
        {
            if (length < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(length),
                    "Arched bridge must be at least 2 tiles long.");
            }

            string[] source = material.Name == "wood" ? WoodenArched : BrickArched;
            return source[length - 2];
        }
    }
}
