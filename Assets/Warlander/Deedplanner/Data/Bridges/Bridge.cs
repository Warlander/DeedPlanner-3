using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class Bridge : IXmlSerializable
    {
        public BridgeData Data { get; }

        public int LowerFloor => Mathf.Min(firstFloor, secondFloor);
        public int HigherFloor => Mathf.Max(firstFloor, secondFloor);

        private readonly OutlineCoordinator _outlineCoordinator;
        
        private readonly BridgePartType[] segments;
        private readonly int firstFloor;
        private readonly int firstX;
        private readonly int firstY;
        private readonly int secondFloor;
        private readonly int secondX;
        private readonly int secondY;
        private readonly int additionalData;
        private readonly bool verticalOrientation;
        private readonly bool surfaced;
        private readonly BridgeType bridgeType;

        private List<BridgePart> bridgeParts = new List<BridgePart>();
        
        public Bridge(Map map, XmlElement element, OutlineCoordinator outlineCoordinator)
        {
            _outlineCoordinator = outlineCoordinator;
            
            string dataString = element.GetAttribute("data");
            Data = Database.Bridges[dataString];

            segments = BridgePartTypeUtils.DecodeSegments(element.InnerText);
            firstFloor = int.Parse(element.GetAttribute("firstFloor"));
            firstX = int.Parse(element.GetAttribute("firstX"));
            firstY = int.Parse(element.GetAttribute("firstY"));
            secondFloor = int.Parse(element.GetAttribute("secondFloor"));
            secondX = int.Parse(element.GetAttribute("secondX"));
            secondY = int.Parse(element.GetAttribute("secondY"));
            additionalData = int.Parse(element.GetAttribute("sag"));
            verticalOrientation = bool.Parse(element.GetAttribute("orientation"));
            if (element.HasAttribute("surfaced"))
            {
                surfaced = bool.Parse(element.GetAttribute("surfaced"));
            }
            else
            {
                surfaced = true;
            }
            
            string typeString = element.GetAttribute("type");
            bool typeParseSuccess = Enum.TryParse(typeString, true, out BridgeType type);
                            
            if (typeParseSuccess)
            {
                bridgeType = type;
            }
            else
            {
                Debug.LogError($"Bridge type enum parsing fail, type: {typeString}");
            }

            ConstructBridge(map);
        }

        private void ConstructBridge(Map map)
        {
            if (bridgeParts.Count != 0)
            {
                Debug.LogError("Bridge already exists, aborting construction");
                return;
            }
            
            int startX = Mathf.Min(firstX, secondX);
            int endX = Mathf.Max(firstX, secondX);
            int startY = Mathf.Min(firstY, secondY);
            int endY = Mathf.Max(firstY, secondY);
            
            // if (verticalOrientation) {
            //     startY += 1;
            //     endY -= 1;
            // }
            // else {
            //     startX += 1;
            //     endX -= 1;
            // }
            
            int maxWidth = Data.MaxWidth;
            int bridgeWidth = Mathf.Min(endX - startX, endY - startY) + 1;
            if (maxWidth < bridgeWidth) {
                Debug.LogError($"Impossible bridge: requested width {bridgeWidth}, max possible: {maxWidth}");
                return;
            }

            IBridgeType bridgeTypeCalc = GetTypeForBridge(bridgeType);

            int bridgeLength = Mathf.Max(endX - startX, endY - startY) + 2;
            int startHeight = GetAbsoluteHeight(map[startX, startY], firstFloor);
            int endHeight = GetAbsoluteHeight(map[endX + 1, endY + 1], secondFloor);
            float heightStep = (float)(endHeight - startHeight) / bridgeWidth;
        
            for (int x = startX; x <= endX; x++) {
                for (int y = startY; y <= endY; y++) {
                    int currentSegment = verticalOrientation ? y - startY : x - startX;
                    float totalHeight = CalculateHeightAtPoint(currentSegment, bridgeTypeCalc, bridgeLength,
                        startHeight, endHeight, heightStep);
                    float totalHeightAfter = CalculateHeightAtPoint(currentSegment + 1, bridgeTypeCalc, bridgeLength,
                        startHeight, endHeight, heightStep);
                    int delta = Mathf.RoundToInt(totalHeightAfter - totalHeight);
                    BridgePartType segment = segments[currentSegment];
                    BridgePartSide side = GetPartSide(startX, startY, endX, endY, x, y, verticalOrientation);
                    EntityOrientation orientation = GetPartOrientation(verticalOrientation, currentSegment);

                    GameObject bridgePartObject = new GameObject("Bridge Part " + Data.Name, typeof(BridgePart));
                    BridgePart bridgePart = bridgePartObject.GetComponent<BridgePart>();
                    bridgePart.Initialise(this, segment, side, orientation, x, y, totalHeight, delta);
                    
                    bridgeParts.Add(bridgePart);
                    map[x, y].RegisterBridgePart(bridgePart);
                }
            }
        }

        private float CalculateHeightAtPoint(int segment, IBridgeType bridgeTypeCalc, int bridgeLength,
            int startHeight, int endHeight, float heightStep)
        {
            if (segment < 0)
            {
                return startHeight;
            }
            if (segment >= bridgeLength)
            {
                return endHeight;
            }
            
            float currentHeight = startHeight + heightStep * segment;
            float currentExtraData = bridgeTypeCalc.CalculateAddedHeight(segment, bridgeLength,
                startHeight, endHeight, additionalData);
            float totalHeight = currentHeight + currentExtraData;
            return totalHeight;
        }
        
        private int GetAbsoluteHeight(Tile tile, int floor) {
            int calculationsFloor = floor;
            if (calculationsFloor < 0) {
                calculationsFloor = -calculationsFloor - 1;
            }
        
            if (calculationsFloor == 0) {
                if (floor < 0)
                {
                    return tile.CaveHeight;
                }
                else
                {
                    return tile.SurfaceHeight;
                }
            }
            else {
                int tileHeight;
                if (floor < 0)
                {
                    tileHeight = tile.CaveHeight;
                }
                else
                {
                    tileHeight = tile.SurfaceHeight;
                }
            
                return tileHeight + calculationsFloor * 30 + 3;
            }
        }
        
        private BridgePartSide GetPartSide(int startX, int startY, int endX, int endY, int x, int y, bool isVertical) {
            if (startX == endX || startY == endY) {
                return BridgePartSide.NARROW;
            }

            if ((startX == x && isVertical) || (startY == y && !isVertical)) {
                return BridgePartSide.RIGHT;
            }
            else if ((endX == x && isVertical) || (endY == y && !isVertical)) {
                return BridgePartSide.LEFT;
            }
            else {
                return BridgePartSide.CENTER;
            }
        }
    
        private EntityOrientation GetPartOrientation(bool isVertical, int segment) {
            int dist = 1;
            while (true) {
                BridgePartType previousSegment = segment - dist < 0 ? BridgePartType.Support : segments[segment - dist];
                BridgePartType nextSegment = segment + dist >= segments.Length ? BridgePartType.Support : segments[segment + dist];

                if (isVertical) {
                    if (nextSegment == BridgePartType.Support) {
                        return EntityOrientation.Up;
                    }
                    else if (previousSegment == BridgePartType.Support) {
                        return EntityOrientation.Down;
                    }
                }
                else {
                    if (nextSegment == BridgePartType.Support) {
                        return EntityOrientation.Right;
                    }
                    else if (previousSegment == BridgePartType.Support) {
                        return EntityOrientation.Left;
                    }
                }
            
                dist++;
            }
        }

        private IBridgeType GetTypeForBridge(BridgeType type)
        {
            switch (type)
            {
                case BridgeType.Rope:
                    return new RopeBridgeType();
                case BridgeType.Flat:
                    return new FlatBridgeType();
                case BridgeType.Arched:
                    return new ArchedBridgeType();
                default:
                    throw new ArgumentException("Unknown bridge type: " + type);
            }
        }

        public void SetVisible(bool state)
        {
            foreach (BridgePart part in bridgeParts)
            {
                part.gameObject.SetActive(state);
            }
        }

        public void EnableHighlighting(OutlineType type)
        {
            foreach (BridgePart part in bridgeParts)
            {
                _outlineCoordinator.AddObject(part, type);
            }
        }

        public void DisableHighlighting()
        {
            foreach (BridgePart part in bridgeParts)
            {
                _outlineCoordinator.RemoveObject(part);
            }
        }

        public void SetPropertyBlock(MaterialPropertyBlock propertyBlock)
        {
            foreach (BridgePart part in bridgeParts)
            {
                Renderer[] renderers = part.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.SetPropertyBlock(propertyBlock);
                }
            }
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            
        }
    }
}