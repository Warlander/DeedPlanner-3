using System;
using System.Linq;

namespace Warlander.Deedplanner.Data.Bridges
{
    public static class BridgeStructureUtils
    {
        /// <summary>
        /// Tries to generate bridge segments based on provided supports.
        /// Bridge might not be valid and should be checked using <see cref="IsBridgeValid"/>.
        /// </summary>
        /// <param name="supports">Supports of a desired bridge.</param>
        /// <returns>Segments of a final bridge. If sections of the bridge are impossible they will be null instead.</returns>
        public static bool ConstructFlatBridge(bool[] supports, out BridgePartType?[] finalSegments)
        {
            finalSegments = new BridgePartType?[supports.Length];

            int[] distances = CalculateDistancesFromSupports(supports);
            
            for (int i = 0; i < supports.Length; i++)
            {
                int leftIndex = i - 1;
                int leftDistanceToSupport = leftIndex >= 0 ? distances[leftIndex] : 0;

                int selfDistanceToSupport = distances[i];

                int rightIndex = 1 + 1;
                int rightDistanceToSupport = rightIndex < supports.Length ? distances[rightIndex] : 0;

                BridgePartType? segment = GetPartTypeForDistanceFromSupport(
                    leftDistanceToSupport, selfDistanceToSupport, rightDistanceToSupport);
                finalSegments[i] = segment;
            }

            return IsConstructedBridgeValid(finalSegments);
        }

        /// <summary>
        /// This method assumes bridge segments were constructed using <see cref="ConstructFlatBridge"/>.
        /// </summary>
        private static bool IsConstructedBridgeValid(BridgePartType?[] segments)
        {
            // Anchor point counts as support.
            bool wasPreviousPartSupport = true;
            
            foreach (BridgePartType? segment in segments)
            {
                if (segment == BridgePartType.Support)
                {
                    // Bridge cannot have two support segments in a row.
                    if (wasPreviousPartSupport)
                    {
                        return false;
                    }

                    wasPreviousPartSupport = true;
                }
                else
                {
                    wasPreviousPartSupport = false;
                }

                if (segment == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static int[] CalculateDistancesFromSupports(bool[] supports)
        {
            int[] distances = new int[supports.Length];

            for (int i = 0; i < supports.Length; i++)
            {
                // This segment is a support.
                if (supports[i])
                {
                    distances[i] = 0;
                    continue;
                }

                int checkDistance = 1;
                while (true)
                {
                    int leftCheckIndex = i - checkDistance;
                    bool isLeftWithinBounds = leftCheckIndex >= 0;
                    bool isLeftSupport = isLeftWithinBounds == false || supports[leftCheckIndex];
                    if (isLeftSupport)
                    {
                        break;
                    }

                    int rightCheckIndex = i + checkDistance;
                    bool isRightWithinBounds = rightCheckIndex < supports.Length;
                    bool isRightSupport = isRightWithinBounds == false || supports[rightCheckIndex];
                    if (isRightSupport)
                    {
                        break;
                    }
                    
                    checkDistance++;
                }

                distances[i] = checkDistance;
            }

            return distances;
        }
        
        /// <summary>
        /// This method assumes distances are valid and calculated using <see cref="CalculateDistancesFromSupports"/>.
        /// </summary>
        /// <param name="leftDistance">Distance to nearest support for segment to the left.</param>
        /// <param name="selfDistance">Own distance to nearest support.</param>
        /// <param name="rightDistance">Distance to nearest support for segment to the right.</param>
        /// <returns>Bridge part for use in a bridge, or null if there's no valid bridge part.</returns>
        private static BridgePartType? GetPartTypeForDistanceFromSupport(int leftDistance, int selfDistance, int rightDistance)
        {
            if (selfDistance == 0)
            {
                return BridgePartType.Support;
            }

            bool isPeak = leftDistance == rightDistance;
            if (isPeak)
            {
                switch (selfDistance)
                {
                    case 1:
                        return BridgePartType.DoubleAbutment;
                    case 2:
                        return BridgePartType.DoubleBracing;
                    case 3:
                        return BridgePartType.Crown;
                }
            }
            
            bool isSameLevelAsNeighbor = leftDistance - selfDistance == 0 || rightDistance - selfDistance == 0;
            if (isSameLevelAsNeighbor)
            {
                switch (selfDistance)
                {
                    case 1:
                        return BridgePartType.Abutment;
                    case 2:
                        return BridgePartType.Bracing;
                }
            }
            
            return null;
        }
    }
}