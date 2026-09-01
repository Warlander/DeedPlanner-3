using Warlander.Deedplanner.Data;
using System;
using UnityEngine;

namespace Warlander.Deedplanner.Bridges
{
    public class RopeBridgeType : IBridgeType
    {
        public string Name => "rope";
        public BridgeType Type => BridgeType.Rope;
        public int[] ExtraArguments { get; } = {3, 4, 5, 6, 7, 8, 9, 10, 11, 12};

        public int CalculateAddedHeight(int currentSegment, int bridgeLength, int startHeight, int endHeight, int extraArgument)
        {
            // no sag at start and end of the bridge
            if (currentSegment == 0 || currentSegment == bridgeLength - 1)
            {
                return 0;
            }

            int halfLength = bridgeLength / 2;
            float totalSag = extraArgument / 100f;
            float sagDistance = (bridgeLength + 1) * 4f * totalSag;
            float scaleCosh = 5E-05f; //Math.cosh(1 / 100) - 1;
            float scaleFactor = sagDistance / scaleCosh;

            int centerRelativeSegment = currentSegment - halfLength;
            // if bridge length is even, make sure bridge center is 2 tiles long
            if (centerRelativeSegment < 0 && bridgeLength % 2 == 0)
            {
                centerRelativeSegment++;
            }

            float scale = (float)centerRelativeSegment / halfLength;

            // Pure sag below the chord - slope between ends is already handled by the caller's
            // linear interpolation, so no height-difference adjustment belongs here.
            float sag = scaleFactor * (float)Math.Cosh(scale / 100) - scaleFactor - sagDistance;
            return (int)(sag * 10);
        }
    }
}
