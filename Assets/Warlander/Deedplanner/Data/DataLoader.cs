using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;
using Warlander.Deedplanner.Data.Caves;
using Warlander.Deedplanner.Data.Decorations;
using Warlander.Deedplanner.Data.Floors;
using Warlander.Deedplanner.Data.Grounds;
using Warlander.Deedplanner.Data.Roofs;
using Warlander.Deedplanner.Data.Walls;
using Warlander.Deedplanner.Graphics;
using Warlander.Deedplanner.Gui;
using Warlander.Deedplanner.Gui.Widgets;
using Warlander.Deedplanner.Logic;

namespace Warlander.Deedplanner.Data
{
    public static class DataLoader
    {

        private static readonly List<string> shortNames = new List<string>();

        public static IEnumerator LoadData()
        {
            if (Properties.Mobile || Properties.Web)
            {
                return LoadData(Application.streamingAssetsPath + "/objects.xml");
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
                objectFiles[i] = Application.streamingAssetsPath + "/" + oldFile;
            }
            
            return LoadData(objectFiles);
        }

        private static IEnumerator LoadData(params string[] locations)
        {
            XmlDocument[] documents = new XmlDocument[locations.Length];

            for (int i = 0; i < documents.Length; i++)
            {
                Debug.Log("Parsing " + locations[i]);
                UnityWebRequest request = UnityWebRequest.Get(locations[i]);
                yield return request.SendWebRequest();
                documents[i] = new XmlDocument();
                documents[i].LoadXml(request.downloadHandler.text);
                Debug.Log("Parsed " + locations[i]);
            }

            foreach (XmlDocument document in documents)
            {
                Debug.Log("Loading grounds");
                LoadGrounds(document);
                shortNames.Clear();
                Debug.Log("Loading caves");
                LoadCaves(document);
                shortNames.Clear();
                Debug.Log("Loading floors");
                LoadFloors(document);
                shortNames.Clear();
                Debug.Log("Loading walls");
                LoadWalls(document);
                shortNames.Clear();
                Debug.Log("Loading roofs");
                LoadRoofs(document);
                shortNames.Clear();
                Debug.Log("Loading objects");
                LoadObjects(document);
                shortNames.Clear();
                Debug.Log("XML file loading complete");
            }
        }

        private static void LoadGrounds(XmlDocument document)
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
                                tex2d = TextureReference.GetTextureReference(child);
                            }
                            else if (target == "previewmode")
                            {
                                tex3d = TextureReference.GetTextureReference(child);
                            }
                            else
                            {
                                tex2d = TextureReference.GetTextureReference(child);
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

                GroundData data = ScriptableObject.CreateInstance<GroundData>();
                data.Initialize(name, shortName, tex2d, tex3d, diagonal);
                Database.Grounds[shortName] = data;
                foreach (string[] category in categories)
                {
                    IconUnityListElement iconListElement = (IconUnityListElement) GuiManager.Instance.GroundsTree.Add(data, category);
                    iconListElement.TextureReference = tex2d;
                }
                Debug.Log("Ground data " + name + " loaded and ready to use!");
            }
        }

        private static void LoadCaves(XmlDocument document)
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

                TextureReference texture = TextureReference.GetTextureReference(element.GetAttribute("tex"));
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

                CaveData data = ScriptableObject.CreateInstance<CaveData>();
                data.Initialize(texture, name, shortName, wall, show, entrance);
                Database.Caves[shortName] = data;
                foreach (string[] category in categories)
                {
                    GuiManager.Instance.CavesTree.Add(data, category);
                }
            }
        }

        private static void LoadFloors(XmlDocument document)
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
                            model = new Model(child, LayerMasks.FloorRoofLayer);
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

                FloorData data = ScriptableObject.CreateInstance<FloorData>();
                data.Initialize(model, name, shortName, opening, materials);
                Database.Floors[shortName] = data;
                foreach (string[] category in categories)
                {
                    GuiManager.Instance.FloorsTree.Add(data, category);
                }
            }
        }

        private static void LoadWalls(XmlDocument document)
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
                            Model model = new Model(child, LayerMasks.WallLayer);
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
                            icon = TextureReference.GetTextureReference(child.GetAttribute("location"));
                            break;
                    }
                }

                if (bottomModel == null)
                {
                    bottomModel = normalModel;
                }

                WallData data = ScriptableObject.CreateInstance<WallData>();
                data.Initialize(bottomModel, normalModel, name, shortName, color, scale, houseWall, arch, archBuildable, materials, icon);
                Database.Walls[shortName] = data;
                foreach (string[] category in categories)
                {
                    IconUnityListElement iconListElement = (IconUnityListElement)GuiManager.Instance.WallsTree.Add(data, category);
                    iconListElement.TextureReference = icon;
                }
            }
        }

        private static void LoadRoofs(XmlDocument document)
        {
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

                TextureReference texture = TextureReference.GetTextureReference(element.GetAttribute("tex"));
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

                RoofData data = ScriptableObject.CreateInstance<RoofData>();
                data.Initialize(texture, name, shortName, materials);
                Database.Roofs[shortName] = data;

                GuiManager.Instance.RoofsList.Add(data);
            }
        }

        private static void LoadObjects(XmlDocument document)
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
                            model = new Model(child, LayerMasks.DecorationLayer);
                            break;
                        case "materials":
                            materials = new Materials(child);
                            break;
                        case "category":
                            categories.Add(child.InnerText.Split('/'));
                            break;
                    }
                }

                DecorationData data = ScriptableObject.CreateInstance<DecorationData>();
                data.Initialize(model, name, shortName, type, centerOnly, cornerOnly, floating, tree, bush, materials);
                Database.Decorations[shortName] = data;

                foreach (string[] category in categories)
                {
                    GuiManager.Instance.ObjectsTree.Add(data, category);
                }
            }
        }

        private static bool VerifyShortName(string shortName)
        {
            if (shortNames.Contains(shortName))
            {
                return false;
            }

            shortNames.Add(shortName);
            return true;
        }

    }
}
