using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Plugins.Warlander.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Warlander.Deedplanner.Data.Bridges;
using Warlander.Deedplanner.Data.Caves;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Data.Docks;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Data.Roofs;
using Warlander.Deedplanner.Data.Walls;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Logic;
using Warlander.Deedplanner.Logging;
using VContainer.Unity;

namespace Warlander.Deedplanner.Data
{
    public class DataLoader : IInitializable
    {
        public static readonly LogCategory Category = new LogCategory("DataLoad");

        private readonly UnityThreadRunner _unityThreadRunner;
        private readonly IWurmAssetFacade _assetFacade;
        private readonly BridgePartDataFactory _bridgePartDataFactory;
        private readonly ICategoryLogger _logger;

        public DataLoader(UnityThreadRunner unityThreadRunner, IWurmAssetFacade assetFacade,
            ILoggerSource loggerSource)
        {
            _unityThreadRunner = unityThreadRunner;
            _assetFacade = assetFacade;
            _logger = loggerSource.Create(Category);
            _bridgePartDataFactory = new BridgePartDataFactory(assetFacade);
        }

        public delegate void LoadingStepStartedDelegate(int stepNumber, string stepDescription);

        public event LoadingStepStartedDelegate LoadingStepStarted;
        public event Action LoadingComplete;
        
        public const int TotalSteps = 9;
        
        public bool Completed { get; private set; }
        
        private readonly List<string> _shortNames = new List<string>();
        private int _stepsCompleted = 0;

        void IInitializable.Initialize()
        {
            string[] locations = GetDataLocations();

            int completedLoadings = 0;
            XmlDocument[] documents = new XmlDocument[locations.Length];

            for (int i = 0; i < documents.Length; i++)
            {
                int index = i;
                _logger.Message("Parsing " + locations[i]);
                UnityWebRequest request = UnityWebRequest.Get(locations[i]);
                request.SendWebRequest().completed += operation =>
                {
                    completedLoadings++;
                    documents[index] = new XmlDocument();
                    documents[index].LoadXml(request.downloadHandler.text);
                    
                    _logger.Message("Parsed " + locations[index]);

                    if (completedLoadings == documents.Length)
                    {
                        if (Application.platform != RuntimePlatform.WebGLPlayer)
                        {
                            Task.Run(() => PerformLoading(documents));
                        }
                        else
                        {
                            PerformLoading(documents);
                        }
                    }
                };
            }
        }

        private string[] GetDataLocations()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer || SystemInfo.deviceType == DeviceType.Handheld)
            {
                return new string[] { Application.streamingAssetsPath + "/objects.xml" };
            }
            
            string[] objectFiles = Directory.GetFiles(Application.streamingAssetsPath);
            objectFiles = objectFiles
                .Where(name => Path.GetFileName(name).StartsWith("objects"))
                .Where(name => name.EndsWith("xml"))
                .ToArray();

            for (int i = 0; i < objectFiles.Length; i++)
            {
                string oldFile = objectFiles[i];
                oldFile = Path.GetFileName(oldFile);
                objectFiles[i] = "file://" + Application.streamingAssetsPath + "/" + oldFile;
            }
            
            return objectFiles;
        }

        private void PerformLoading(XmlDocument[] documents)
        {
            IncrementStep(documents, "Loading grounds", LoadGrounds);
            IncrementStep(documents, "Loading caves", LoadCaves);
            IncrementStep(documents, "Loading floors", LoadFloors);
            IncrementStep(documents, "Loading walls", LoadWalls);
            IncrementStep(documents, "Loading roofs", LoadRoofs);
            IncrementStep(documents, "Loading objects", LoadObjects);
            IncrementStep(documents, "Loading bridges", LoadBridges);
            IncrementStep(documents, "Loading bridge pavements", LoadBridgePavements);
            IncrementStep(documents, "Loading dock supports", LoadDockSupports);
            
            Completed = true;
            
            _unityThreadRunner.RunOnUnityThread(() =>
            {
                _logger.Message("XML file loading complete");
                LoadingComplete?.Invoke();
            });
        }

        private void IncrementStep(XmlDocument[] documents, string description, Action<XmlDocument> loadingAction)
        {
            _stepsCompleted++;
            int capturedStepNumber = _stepsCompleted;
            
            _unityThreadRunner.RunOnUnityThread(() =>
            {
                _logger.Message(description);
                LoadingStepStarted?.Invoke(capturedStepNumber, description);
            });
            
            Array.ForEach(documents, loadingAction);
            _shortNames.Clear();
        }

        private void LoadGrounds(XmlDocument document)
        {
            XmlNodeList entities = document.GetElementsByTagName("ground");

            foreach (XmlElement element in entities)
            {
                string name = element.GetAttribute("name");
                string shortName = element.GetAttribute("shortname");

                _logger.Message("Loading ground " + name);

                bool unique = VerifyShortName(shortName);
                if (!unique)
                {
                    _logger.Warning("Shortname " + shortName + " already exists, aborting");
                    continue;
                }

                TextureReference tex2d = null;
                TextureReference tex3d = null;
                List<string[]> categories = new List<string[]>();
                bool diagonal = false;
                bool caveDoor = false;

                foreach (XmlElement child in element)
                {
                    switch (child.LocalName)
                    {
                        case "tex":
                            string target = child.GetAttribute("target");
                            if (target == "editmode")
                            {
                                tex2d = _assetFacade.GetTextureReference(child);
                            }
                            else if (target == "previewmode")
                            {
                                tex3d = _assetFacade.GetTextureReference(child);
                            }
                            else
                            {
                                tex2d = _assetFacade.GetTextureReference(child);
                                tex3d = tex2d;
                            }
                            break;
                        case "category":
                            categories.Add(child.InnerText.Split('/'));
                            caveDoor = (child.InnerText == "Cave doors") ? true : false;
                            break;
                        case "diagonal":
                            diagonal = true;
                            break;
                    }
                }

                if (tex2d == null || tex3d == null)
                {
                    _logger.Warning("No textures loaded, aborting");
                }

                GroundData data = new GroundData(name, shortName, categories.ToArray(), tex2d, tex3d, diagonal, caveDoor);
                Database.Grounds[shortName] = data;
                _logger.Message("Ground data " + name + " loaded and ready to use!");
            }
        }

        private void LoadBridgePavements(XmlDocument document)
        {
            XmlNodeList entities = document.GetElementsByTagName("bridgepavement");

            foreach (XmlElement element in entities)
            {
                string name = element.GetAttribute("name");
                string shortName = element.GetAttribute("shortname");
                TextureReference tex = null;
                Materials materials = null;

                foreach (XmlElement child in element)
                {
                    if (child.LocalName == "tex")
                    {
                        tex = _assetFacade.GetTextureReference(child);
                    }
                    else if (child.LocalName == "materials")
                    {
                        materials = new Materials(child);
                    }
                }

                if (tex == null)
                {
                    _logger.Warning("Bridge pavement " + name + " has no texture, skipping");
                    continue;
                }

                bool unique = VerifyShortName(shortName);
                if (!unique)
                {
                    _logger.Warning("Bridge pavement shortname " + shortName + " conflicts with an existing one, skipping");
                    continue;
                }

                Database.BridgePavements[shortName] = new BridgePavementData(name, shortName, tex, materials);
                _logger.Message("Bridge pavement " + name + " loaded and ready to use!");
            }
        }

        private void LoadDockSupports(XmlDocument document)
        {
            XmlNodeList entities = document.GetElementsByTagName("docksupport");

            foreach (XmlElement element in entities)
            {
                string name = element.GetAttribute("name");
                string shortName = element.GetAttribute("shortname");
                DockSupportType supportType = ParseDockSupportType(element.GetAttribute("type"));
                ModelHandle baseModel = null;
                ModelHandle extensionModel = null;
                Materials materials = null;

                foreach (XmlElement child in element)
                {
                    if (child.LocalName == "materials")
                    {
                        materials = new Materials(child);
                        continue;
                    }

                    if (child.LocalName != "model")
                    {
                        continue;
                    }

                    if (child.GetAttribute("tag") == "extension")
                    {
                        extensionModel = _assetFacade.GetModel(child, LayerMasks.FloorRoofLayer);
                    }
                    else
                    {
                        baseModel = _assetFacade.GetModel(child, LayerMasks.FloorRoofLayer);
                    }
                }

                if (baseModel == null)
                {
                    _logger.Warning("Dock support " + name + " has no base model, skipping");
                    continue;
                }

                bool unique = VerifyShortName(shortName);
                if (!unique)
                {
                    _logger.Warning("Dock support shortname " + shortName + " conflicts with an existing one, skipping");
                    continue;
                }

                Database.DockSupports[shortName] = new DockSupportData(name, shortName, supportType, baseModel, extensionModel, materials);
                _logger.Message("Dock support " + name + " loaded and ready to use!");
            }
        }

        private DockSupportType ParseDockSupportType(string type)
        {
            switch (type)
            {
                case "wood":
                    return DockSupportType.WoodPillar;
                case "stone":
                    return DockSupportType.StonePillar;
                case "brace":
                    return DockSupportType.Brace;
                default:
                    _logger.Warning("Unknown dock support type: " + type);
                    return DockSupportType.None;
            }
        }

        private void LoadCaves(XmlDocument document)
        {
            XmlNodeList entities = document.GetElementsByTagName("rock");

            foreach (XmlElement element in entities)
            {
                string name = element.GetAttribute("name");
                string shortName = element.GetAttribute("shortname");

                _logger.Message("Loading cave data " + name);

                bool unique = VerifyShortName(shortName);
                if (!unique)
                {
                    _logger.Warning("Shortname " + shortName + " already exists, aborting");
                    continue;
                }

                TextureReference texture = _assetFacade.GetTextureReference(element.GetAttribute("tex"));
                string type = element.GetAttribute("type");
                bool wall = type == "wall";
                bool entrance = type == "entrance";

                List<string[]> categories = new List<string[]>();
                bool show = true;

                foreach (XmlElement child in element)
                {
                    switch (child.LocalName)
                    {
                        case "category":
                            categories.Add(child.InnerText.Split('/'));
                            break;
                        case "hidden":
                            show = false;
                            break;
                    }
                }

                CaveData data = new CaveData(texture, name, shortName, categories.ToArray(), wall, show, entrance);
                Database.Caves[shortName] = data;
            }
        }

        private void LoadFloors(XmlDocument document)
        {
            XmlNodeList entities = document.GetElementsByTagName("floor");

            foreach (XmlElement element in entities)
            {
                string name = element.GetAttribute("name");
                string shortName = element.GetAttribute("shortname");

                _logger.Message("Loading floor " + name);

                bool unique = VerifyShortName(shortName);
                if (!unique)
                {
                    _logger.Warning("Shortname " + shortName + " already exists, aborting");
                    continue;
                }

                ModelHandle model = null;
                List<string[]> categories = new List<string[]>();
                bool opening = false;
                bool supportsDock = element.GetAttribute("dockable") == "true";
                Materials materials = null;

                foreach (XmlElement child in element)
                {
                    switch (child.LocalName)
                    {
                        case "model":
                            model = _assetFacade.GetModel(child, LayerMasks.FloorRoofLayer);
                            break;
                        case "category":
                            categories.Add(child.InnerText.Split('/'));
                            break;
                        case "opening":
                            opening = true;
                            break;
                        case "materials":
                            materials = new Materials(child);
                            break;
                    }
                }

                if (model == null)
                {
                    _logger.Warning("No model loaded, aborting");
                }

                FloorData data = new FloorData(model, name, shortName, categories.ToArray(), opening, supportsDock, materials);
                Database.Floors[shortName] = data;
            }
        }

        private void LoadWalls(XmlDocument document)
        {
            XmlNodeList entities = document.GetElementsByTagName("wall");

            foreach (XmlElement element in entities)
            {
                string name = element.GetAttribute("name");
                string shortName = element.GetAttribute("shortname");
                float scale = float.Parse(element.GetAttribute("scale"));

                bool unique = VerifyShortName(shortName);
                if (!unique)
                {
                    _logger.Warning("Shortname " + shortName + " already exists, aborting");
                    continue;
                }

                string type = element.GetAttribute("type");
                bool houseWall = type == "house" || type == "arch";
                bool arch = type == "arch";
                bool archBuildable = type == "lowfence";

                ModelHandle bottomModel = null;
                ModelHandle normalModel = null;
                TextureReference icon = null;
                Color color = Color.white;

                List<string[]> categories = new List<string[]>();
                Materials materials = null;

                foreach (XmlElement child in element)
                {
                    switch (child.LocalName)
                    {
                        case "model":
                            ModelHandle model = _assetFacade.GetModel(child, LayerMasks.WallLayer);
                            if (model.Tag == "bottom")
                            {
                                bottomModel = model;
                            }
                            else
                            {
                                normalModel = model;
                            }
                            break;
                        case "category":
                            categories.Add(child.InnerText.Split('/'));
                            break;
                        case "color":
                            float r = float.Parse(child.GetAttribute("r"), CultureInfo.InvariantCulture);
                            float g = float.Parse(child.GetAttribute("g"), CultureInfo.InvariantCulture);
                            float b = float.Parse(child.GetAttribute("b"), CultureInfo.InvariantCulture);
                            color = new Color(r, g, b);
                            break;
                        case "materials":
                            materials = new Materials(child);
                            break;
                        case "icon":
                            icon = _assetFacade.GetTextureReference(child.GetAttribute("location"));
                            break;
                    }
                }

                if (bottomModel == null)
                {
                    bottomModel = normalModel;
                }

                WallData data = new WallData(bottomModel, normalModel, name, shortName, categories.ToArray(), color,
                    scale, houseWall, arch, archBuildable, materials, icon);
                Database.Walls[shortName] = data;
            }
        }

        private void LoadRoofs(XmlDocument document)
        {
            XmlNodeList entities = document.GetElementsByTagName("roof");

            foreach (XmlElement element in entities)
            {
                string name = element.GetAttribute("name");
                string shortName = element.GetAttribute("shortname");

                _logger.Message("Loading roof " + name);

                bool unique = VerifyShortName(shortName);
                if (!unique)
                {
                    _logger.Warning("Shortname " + shortName + " already exists, aborting");
                    continue;
                }

                TextureReference texture = _assetFacade.GetTextureReference(element.GetAttribute("tex"));
                Materials materials = null;

                foreach (XmlElement child in element)
                {
                    switch (child.LocalName)
                    {
                        case "materials":
                            materials = new Materials(child);
                            break;
                    }
                }

                RoofData data = new RoofData(texture, name, shortName, materials);
                Database.Roofs[shortName] = data;
            }
            
            RoofType.Initialize(_assetFacade);
        }

        private void LoadObjects(XmlDocument document)
        {
            XmlNodeList entities = document.GetElementsByTagName("object");

            foreach (XmlElement element in entities)
            {
                string name = element.GetAttribute("name");
                string shortName = element.GetAttribute("shortname");
                string type = element.GetAttribute("type");
                bool centerOnly = type == "centered";
                bool cornerOnly = type == "corner";
                bool floating = type == "floating";
                bool tree = type == "tree";
                bool bush = type == "bush";

                _logger.Message("Loading object " + name);

                bool unique = VerifyShortName(shortName);
                if (!unique)
                {
                    _logger.Warning("Shortname " + shortName + " already exists, aborting");
                    continue;
                }

                ModelHandle model = null;
                List<string[]> categories = new List<string[]>();
                Materials materials = null;

                foreach (XmlElement child in element)
                {
                    switch (child.LocalName)
                    {
                        case "model":
                            model = _assetFacade.GetModel(child, LayerMasks.DecorationLayer);
                            break;
                        case "materials":
                            materials = new Materials(child);
                            break;
                        case "category":
                            categories.Add(child.InnerText.Split('/'));
                            break;
                    }
                }

                DecorationData data = new DecorationData(model, name, shortName, categories.ToArray(),
                    type, centerOnly, cornerOnly, floating,
                    tree, bush, materials);
                Database.Decorations[shortName] = data;
            }
        }

        private void LoadBridges(XmlDocument document)
        {
            XmlNodeList entities = document.GetElementsByTagName("bridge");
            
            foreach (XmlElement element in entities)
            {
                string name = element.GetAttribute("name");
                int supportHeight = int.Parse(element.GetAttribute("supportheight"));
                int maxWidth = int.Parse(element.GetAttribute("maxwidth"));
                bool canBePaved = element.GetAttribute("paveable") == "true";

                _logger.Message("Loading object " + name);

                bool unique = VerifyShortName(name);
                if (!unique)
                {
                    _logger.Warning("Name " + name + " already exists, aborting");
                    continue;
                }

                List<BridgeType> allowedTypes = new List<BridgeType>();
                Dictionary<BridgePartSide, Materials> sidesCost = new Dictionary<BridgePartSide, Materials>();
                List<BridgePartData> partsData = new List<BridgePartData>();

                foreach (XmlElement child in element)
                {
                    switch (child.LocalName)
                    {
                        case "type":
                            string typeString = child.GetAttribute("name");
                            bool typeParseSuccess = Enum.TryParse(typeString, true, out BridgeType type);
                            
                            if (typeParseSuccess)
                            {
                                allowedTypes.Add(type);
                            }
                            else
                            {
                                _logger.Error($"Bridge type enum parsing fail for bridge {name}, type: {typeString}");
                            }
                            break;
                        case "lane":
                            string sideString = child.GetAttribute("type");
                            bool sideParseSuccess = Enum.TryParse(sideString, true, out BridgePartSide side);
                            Materials sideMaterials;
                            if (child.HasChildNodes)
                            {
                                sideMaterials = new Materials(child.GetElementsByTagName("materials")[0]);
                            }
                            else
                            {
                                sideMaterials = new Materials();
                            }

                            if (sideString.Equals("side", StringComparison.OrdinalIgnoreCase))
                            {
                                sidesCost.Add(BridgePartSide.LEFT, sideMaterials);
                                sidesCost.Add(BridgePartSide.RIGHT, sideMaterials);
                            }
                            else if (sideParseSuccess)
                            {
                                sidesCost.Add(side, sideMaterials);
                            }
                            else
                            {
                                _logger.Error($"Bridge side enum parsing fail for bridge {name}, type: {sideString}");
                            }
                            break;
                        case "part":
                            partsData.Add(_bridgePartDataFactory.Create(child));
                            break;
                    }
                }

                BridgeData data = new BridgeData(name, maxWidth, supportHeight, partsData.ToArray(),
                    allowedTypes.ToArray(), sidesCost, canBePaved);
                Database.Bridges[name] = data;
            }
        }

        private bool VerifyShortName(string shortName)
        {
            if (_shortNames.Contains(shortName))
            {
                return false;
            }

            _shortNames.Add(shortName);
            return true;
        }
    }
}
