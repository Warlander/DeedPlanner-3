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
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Data.Roofs;
using Warlander.Deedplanner.Data.Walls;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Logic;
using Zenject;

namespace Warlander.Deedplanner.Data
{
    public class DataLoader : IInitializable
    {
        private readonly UnityThreadRunner _unityThreadRunner;
        private readonly ITextureReferenceFactory _textureReferenceFactory;
        private readonly IWurmModelFactory _modelFactory;
        private readonly BridgePartDataFactory _bridgePartDataFactory;

        [Inject]
        public DataLoader(UnityThreadRunner unityThreadRunner, ITextureReferenceFactory textureReferenceFactory,
            IWurmModelFactory modelFactory, BridgePartDataFactory bridgePartDataFactory)
        {
            _unityThreadRunner = unityThreadRunner;
            _textureReferenceFactory = textureReferenceFactory;
            _modelFactory = modelFactory;
            _bridgePartDataFactory = bridgePartDataFactory;
        }

        public delegate void LoadingStepStartedDelegate(int stepNumber, string stepDescription);

        public event LoadingStepStartedDelegate LoadingStepStarted;
        public event Action LoadingComplete;
        
        public const int TotalSteps = 7;
        
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
                Debug.Log("Parsing " + locations[i]);
                UnityWebRequest request = UnityWebRequest.Get(locations[i]);
                request.SendWebRequest().completed += operation =>
                {
                    completedLoadings++;
                    documents[index] = new XmlDocument();
                    documents[index].LoadXml(request.downloadHandler.text);
                    
                    Debug.Log("Parsed " + locations[index]);

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
            
            Completed = true;
            
            _unityThreadRunner.RunOnUnityThread(() =>
            {
                Debug.Log("XML file loading complete");
                LoadingComplete?.Invoke();
            });
        }

        private void IncrementStep(XmlDocument[] documents, string description, Action<XmlDocument> loadingAction)
        {
            _stepsCompleted++;
            int capturedStepNumber = _stepsCompleted;
            
            _unityThreadRunner.RunOnUnityThread(() =>
            {
                Debug.Log(description);
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

                Debug.Log("Loading ground " + name);

                bool unique = VerifyShortName(shortName);
                if (!unique)
                {
                    Debug.LogWarning("Shortname " + shortName + " already exists, aborting");
                    continue;
                }

                TextureReference tex2d = null;
                TextureReference tex3d = null;
                List<string[]> categories = new List<string[]>();
                bool diagonal = false;

                foreach (XmlElement child in element)
                {
                    switch (child.LocalName)
                    {
                        case "tex":
                            string target = child.GetAttribute("target");
                            if (target == "editmode")
                            {
                                tex2d = _textureReferenceFactory.GetTextureReference(child);
                            }
                            else if (target == "previewmode")
                            {
                                tex3d = _textureReferenceFactory.GetTextureReference(child);
                            }
                            else
                            {
                                tex2d = _textureReferenceFactory.GetTextureReference(child);
                                tex3d = tex2d;
                            }
                            break;
                        case "category":
                            categories.Add(child.InnerText.Split('/'));
                            break;
                        case "diagonal":
                            diagonal = true;
                            break;
                    }
                }

                if (tex2d == null || tex3d == null)
                {
                    Debug.LogWarning("No textures loaded, aborting");
                }

                GroundData data = new GroundData(name, shortName, categories.ToArray(), tex2d, tex3d, diagonal);
                Database.Grounds[shortName] = data;
                Debug.Log("Ground data " + name + " loaded and ready to use!");
            }
        }

        private void LoadCaves(XmlDocument document)
        {
            XmlNodeList entities = document.GetElementsByTagName("rock");

            foreach (XmlElement element in entities)
            {
                string name = element.GetAttribute("name");
                string shortName = element.GetAttribute("shortname");

                Debug.Log("Loading cave data " + name);

                bool unique = VerifyShortName(shortName);
                if (!unique)
                {
                    Debug.LogWarning("Shortname " + shortName + " already exists, aborting");
                    continue;
                }

                TextureReference texture = _textureReferenceFactory.GetTextureReference(element.GetAttribute("tex"));
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

                Debug.Log("Loading floor " + name);

                bool unique = VerifyShortName(shortName);
                if (!unique)
                {
                    Debug.LogWarning("Shortname " + shortName + " already exists, aborting");
                    continue;
                }

                Model model = null;
                List<string[]> categories = new List<string[]>();
                bool opening = false;
                Materials materials = null;

                foreach (XmlElement child in element)
                {
                    switch (child.LocalName)
                    {
                        case "model":
                            model = _modelFactory.CreateModel(child, LayerMasks.FloorRoofLayer);
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
                    Debug.LogWarning("No model loaded, aborting");
                }

                FloorData data = new FloorData(model, name, shortName, categories.ToArray(), opening, materials);
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
                    Debug.LogWarning("Shortname " + shortName + " already exists, aborting");
                    continue;
                }

                string type = element.GetAttribute("type");
                bool houseWall = type == "house" || type == "arch";
                bool arch = type == "arch";
                bool archBuildable = type == "lowfence";

                Model bottomModel = null;
                Model normalModel = null;
                TextureReference icon = null;
                Color color = Color.white;

                List<string[]> categories = new List<string[]>();
                Materials materials = null;

                foreach (XmlElement child in element)
                {
                    switch (child.LocalName)
                    {
                        case "model":
                            Model model = _modelFactory.CreateModel(child, LayerMasks.WallLayer);
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
                            icon = _textureReferenceFactory.GetTextureReference(child.GetAttribute("location"));
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
            RoofType.Initialize(_modelFactory);
            
            XmlNodeList entities = document.GetElementsByTagName("roof");

            foreach (XmlElement element in entities)
            {
                string name = element.GetAttribute("name");
                string shortName = element.GetAttribute("shortname");

                Debug.Log("Loading roof " + name);

                bool unique = VerifyShortName(shortName);
                if (!unique)
                {
                    Debug.LogWarning("Shortname " + shortName + " already exists, aborting");
                    continue;
                }

                TextureReference texture = _textureReferenceFactory.GetTextureReference(element.GetAttribute("tex"));
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

                Debug.Log("Loading object " + name);

                bool unique = VerifyShortName(shortName);
                if (!unique)
                {
                    Debug.LogWarning("Shortname " + shortName + " already exists, aborting");
                    continue;
                }

                Model model = null;
                List<string[]> categories = new List<string[]>();
                Materials materials = null;

                foreach (XmlElement child in element)
                {
                    switch (child.LocalName)
                    {
                        case "model":
                            model = _modelFactory.CreateModel(child, LayerMasks.DecorationLayer);
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

                Debug.Log("Loading object " + name);

                bool unique = VerifyShortName(name);
                if (!unique)
                {
                    Debug.LogWarning("Name " + name + " already exists, aborting");
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
                                Debug.LogError($"Bridge type enum parsing fail for bridge {name}, type: {typeString}");
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
                                Debug.LogError($"Bridge side enum parsing fail for bridge {name}, type: {sideString}");
                            }
                            break;
                        case "part":
                            partsData.Add(_bridgePartDataFactory.Create(child));
                            break;
                    }
                }

                BridgeData data = new BridgeData(name, maxWidth, supportHeight, partsData.ToArray(),
                    allowedTypes.ToArray(), sidesCost);
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
