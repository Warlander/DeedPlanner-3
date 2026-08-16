using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using Warlander.Deedplanner.Graphics.Outline;
using Warlander.Deedplanner.Logic.Outlines;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Data.Bridges
{
    public class Bridge : IXmlSerializable
    {
        public BridgeData Data { get; private set; }

        public BridgeType Type => bridgeType;
        public int AdditionalData => additionalData;
        public int LowerLevel => Mathf.Min(firstLevel, secondLevel);
        public int HigherLevel => Mathf.Max(firstLevel, secondLevel);
        public Vector2Int FirstTile => new Vector2Int(firstX, firstY);
        public Vector2Int SecondTile => new Vector2Int(secondX, secondY);

        public event Action Rebuilt;

        private readonly IOutlineCoordinator _outlineCoordinator;

        private BridgePartType[] segments;
        private readonly int firstLevel;
        private readonly int firstX;
        private readonly int firstY;
        private readonly int secondLevel;
        private readonly int secondX;
        private readonly int secondY;
        private int additionalData;
        private readonly bool verticalOrientation;
        private readonly bool surfaced;
        private readonly BridgeType bridgeType;

        private List<BridgePart> bridgeParts = new List<BridgePart>();
        private readonly List<BridgePart> segmentParts = new List<BridgePart>();
        private readonly MaterialPropertyBlock _opacityMergeBlock = new MaterialPropertyBlock();
        private bool _attached;
        
        public Bridge(Map map, XmlElement element, IOutlineCoordinator outlineCoordinator)
        {
            _outlineCoordinator = outlineCoordinator;
            
            string dataString = element.GetAttribute("data");
            Data = Database.Bridges[dataString];

            segments = BridgePartTypeUtils.DecodeSegments(element.InnerText);
            firstLevel = int.Parse(element.GetAttribute("firstFloor"));
            firstX = int.Parse(element.GetAttribute("firstX"));
            firstY = int.Parse(element.GetAttribute("firstY"));
            secondLevel = int.Parse(element.GetAttribute("secondFloor"));
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

        /// <summary>
        /// Constructor used for moving (previously) existing bridges around the map.
        /// </summary>
        public Bridge(Map map, Bridge originalBridge, Vector2Int tileShift,
            IOutlineCoordinator outlineCoordinator)
        {
            _outlineCoordinator = outlineCoordinator;

            Data = originalBridge.Data;

            segments = (BridgePartType[])originalBridge.segments.Clone();
            firstLevel = originalBridge.firstLevel;
            firstX = originalBridge.firstX + tileShift.x;
            firstY = originalBridge.firstY + tileShift.y;
            secondLevel = originalBridge.secondLevel;
            secondX = originalBridge.secondX + tileShift.x;
            secondY = originalBridge.secondY + tileShift.y;
            additionalData = originalBridge.additionalData;
            verticalOrientation = originalBridge.verticalOrientation;
            surfaced = originalBridge.surfaced;
            bridgeType = originalBridge.bridgeType;
            
            ConstructBridge(map);
        }

        /// <summary>
        /// Constructor used for runtime bridge creation from the Bridges tab.
        /// </summary>
        public Bridge(Map map, TileCoords start, TileCoords end, BridgeData data,
            BridgeType type, int additionalData, string segments,
            IOutlineCoordinator outlineCoordinator)
        {
            _outlineCoordinator = outlineCoordinator;

            Data = data;
            this.segments = BridgePartTypeUtils.DecodeSegments(segments);
            this.additionalData = additionalData;
            bridgeType = type;
            surfaced = start.Level >= 0;

            int minX = Mathf.Min(start.X, end.X);
            int maxX = Mathf.Max(start.X, end.X);
            int minY = Mathf.Min(start.Y, end.Y);
            int maxY = Mathf.Max(start.Y, end.Y);

            if (maxY - minY > maxX - minX)
            {
                verticalOrientation = true;
                minY += 1;
                maxY -= 1;
            }
            else
            {
                verticalOrientation = false;
                minX += 1;
                maxX -= 1;
            }

            firstLevel = start.Level;
            firstX = minX;
            firstY = minY;
            secondLevel = end.Level;
            secondX = maxX;
            secondY = maxY;

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
            int startHeight = GetAbsoluteHeight(map[startX, startY], firstLevel);
            int endHeight = GetAbsoluteHeight(map[endX + 1, endY + 1], secondLevel);
            float heightStep = (float)(endHeight - startHeight) / (bridgeLength - 1);
        
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
                    map[x, y].RegisterBridgePart(bridgePart);
                    bridgePart.Initialise(this, segment, side, orientation, x, y, totalHeight, delta);

                    bridgeParts.Add(bridgePart);
                    if (segmentParts.Count == currentSegment)
                    {
                        segmentParts.Add(bridgePart);
                    }
                }
            }

            _attached = true;
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
        
        private int GetAbsoluteHeight(Tile tile, int level) 
        {
            int baseHeight;
            if (level < 0)
            {
                baseHeight = tile.CaveHeight;
            }
            else
            {
                baseHeight = tile.SurfaceHeight;
            }

            int buildingLevel = level >= 0 ? level : -level - 1;
            if (buildingLevel > 0)
            {
                // Tiny bit of extra height for levels above ground level to account for height of the level.
                baseHeight += 3;
            }
            
            return baseHeight + buildingLevel * 30;
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

        internal static IBridgeType GetTypeForBridge(BridgeType type)
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

        public void AttachToMap()
        {
            if (_attached)
            {
                return;
            }

            foreach (BridgePart part in bridgeParts)
            {
                part.Tile.RegisterBridgePart(part);
            }

            SetVisible(true);
            _attached = true;
        }

        public void DetachFromMap()
        {
            if (!_attached)
            {
                return;
            }

            DisableHighlighting();

            foreach (BridgePart part in bridgeParts)
            {
                if (part.Tile != null)
                {
                    part.Tile.UnregisterBridgePart();
                }
            }

            SetVisible(false);
            _attached = false;
        }

        public void Destroy()
        {
            DisableHighlighting();

            foreach (BridgePart part in bridgeParts)
            {
                if (part.Tile != null)
                {
                    part.Tile.UnregisterBridgePart();
                }

                UnityEngine.Object.Destroy(part.gameObject);
            }

            bridgeParts.Clear();
            segmentParts.Clear();
            _attached = false;
        }

        public void EnableHighlighting(OutlineType type)
        {
            foreach (BridgePart part in bridgeParts)
            {
                _outlineCoordinator.AddObject(part, type, 1);
            }
        }

        public void DisableHighlighting()
        {
            foreach (BridgePart part in bridgeParts)
            {
                _outlineCoordinator.RemoveObject(part, 1);
            }
        }

        public void SetPropertyBlock(MaterialPropertyBlock propertyBlock)
        {
            Color opacityColor = propertyBlock.GetColor(ShaderPropertyIds.BaseColor);
            foreach (BridgePart part in bridgeParts)
            {
                Renderer[] renderers = part.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    // Merge instead of replacing - the block also carries the slope shear
                    // (_ShearY), and wholesale replacement wipes it (sloped deck renders as staircase).
                    renderer.GetPropertyBlock(_opacityMergeBlock);
                    _opacityMergeBlock.SetColor(ShaderPropertyIds.BaseColor, opacityColor);
                    renderer.SetPropertyBlock(_opacityMergeBlock);
                }
            }
        }

        /// <summary>
        /// Bridge is longitudinal if going south-north instead of west-east.
        /// </summary>
        public bool IsLongitudinal()
        {
            return verticalOrientation;
        }

        public ReadOnlyCollection<BridgePart> GetBridgeParts()
        {
            return bridgeParts.AsReadOnly();
        }

        public BridgePart GetBridgePart(int index)
        {
            return bridgeParts[index];
        }

        public int SegmentCount => segments.Length;

        public BridgePart GetSegmentPart(int index)
        {
            return segmentParts[index];
        }

        public string GetSegmentsString()
        {
            return BridgePartTypeUtils.EncodeSegments(segments);
        }

        public bool[] GetSupportPositions()
        {
            bool[] supports = new bool[segments.Length];
            for (int i = 0; i < segments.Length; i++)
            {
                supports[i] = segments[i] == BridgePartType.Support;
            }

            return supports;
        }

        public void Rebuild(Map map, BridgeData newData, string newSegments, int newAdditionalData)
        {
            DisableHighlighting();

            foreach (BridgePart part in bridgeParts)
            {
                if (part.Tile != null)
                {
                    part.Tile.UnregisterBridgePart();
                }

                UnityEngine.Object.Destroy(part.gameObject);
            }

            bridgeParts.Clear();
            segmentParts.Clear();
            _attached = false;

            Data = newData;
            segments = BridgePartTypeUtils.DecodeSegments(newSegments);
            additionalData = newAdditionalData;

            ConstructBridge(map);
            Rebuilt?.Invoke();
        }

        public void Serialize(XmlDocument document, XmlElement localRoot)
        {
            localRoot.SetAttribute("data", Data.Name);
            localRoot.SetAttribute("type", bridgeType.ToString().ToUpperInvariant());
            localRoot.SetAttribute("firstFloor", firstLevel.ToString());
            localRoot.SetAttribute("firstX", firstX.ToString());
            localRoot.SetAttribute("firstY", firstY.ToString());
            localRoot.SetAttribute("secondFloor", secondLevel.ToString());
            localRoot.SetAttribute("secondX", secondX.ToString());
            localRoot.SetAttribute("secondY", secondY.ToString());
            localRoot.SetAttribute("sag", additionalData.ToString());
            localRoot.SetAttribute("orientation", verticalOrientation ? "true" : "false");
            localRoot.SetAttribute("surfaced", surfaced ? "true" : "false");
            localRoot.InnerText = BridgePartTypeUtils.EncodeSegments(segments);
        }
    }
}