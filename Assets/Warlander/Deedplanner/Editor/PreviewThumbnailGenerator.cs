using Warlander.Deedplanner.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Warlander.Deedplanner.Rendering.Assets;
using Warlander.Deedplanner.Logging;
using Object = UnityEngine.Object;

namespace Warlander.Deedplanner.Editor
{
    /// <summary>
    /// Renders floors, walls and decorations from objects.xml into 64x64 preview atlas
    /// textures with JSON manifests. Outputs live in generated Resources.
    /// </summary>
    public static class PreviewThumbnailGenerator
    {
        // Render-input changes require this bump plus a harmless objects.xml change to invalidate CI caches.
        public const int GeneratorVersion = 4;
        private const int CellSize = 64;
        private const int RenderResolution = 256;
        private const float FitMargin = 1.02f;
        private const float CameraElevationDeg = -30f;
        private const float CameraAzimuthDeg = -30f;
        public const string OutputFolder = "Assets/Generated/Resources/Previews";
        private const string ObjectsXmlLocation = "objects.xml";
        private static readonly TimeSpan ModelLoadTimeout = TimeSpan.FromSeconds(25);

        public static bool IsRunning { get; private set; }
        public static int CompletedCount { get; private set; }
        public static int TotalCount { get; private set; }
        public static string CurrentEntry { get; private set; }
        public static string LastRunStatus { get; private set; } = "Not run";

        public static readonly LogCategory Category = new LogCategory("Thumbnails");

        // Loader chatter (thousands of console entries per run) measurably slows generation down;
        // this private source silences it while warnings and errors still surface.
        private static readonly LogLevelFilter LoaderFilter;
        private static readonly LoggerSource LoaderSource;
        private static readonly ICategoryLogger Logger;

        static PreviewThumbnailGenerator()
        {
            LoaderFilter = new LogLevelFilter();
            LoaderFilter.SetMinimum(WurmAssetFacade.Category, LogType.Warning);
            LoaderSource = new LoggerSource(LoaderFilter);
            Logger = LoaderSource.Create(Category);
        }

        // Runtime-created Unity objects (loaded models, materials, textures) die when the editor
        // runs Resources.UnloadUnusedAssets under memory pressure, since nothing native references
        // them. Cell pixels are kept as managed arrays to survive that, and the asset facade is
        // recreated on failure so caches rebuild from scratch.
        private static Camera _camera;
        private static WurmAssetFacade _assetFacade;
        private static readonly List<string> FailedEntries = new List<string>();
        private static readonly List<long> LoadMilliseconds = new List<long>();
        private static readonly List<long> RenderMilliseconds = new List<long>();

        private sealed class PreviewEntry
        {
            public readonly string ShortName;
            public readonly XmlElement NormalModelElement;
            public readonly XmlElement BottomModelElement;

            public PreviewEntry(string shortName, XmlElement normalModelElement, XmlElement bottomModelElement)
            {
                ShortName = shortName;
                NormalModelElement = normalModelElement;
                BottomModelElement = bottomModelElement;
            }
        }

        private sealed class GroundPreviewEntry
        {
            public readonly string ShortName;
            public readonly string TextureLocation;

            public GroundPreviewEntry(string shortName, string textureLocation)
            {
                ShortName = shortName;
                TextureLocation = textureLocation;
            }
        }

        [MenuItem("DeedPlanner/Generate Preview Thumbnails")]
        public static void StartGeneration()
        {
            if (IsRunning)
            {
                Logger.Warning("Preview thumbnail generation is already running");
                return;
            }

            RunAllAsync();
        }

        public static async Task GenerateAllAsync()
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Preview thumbnail generation is already running");
            }

            IsRunning = true;
            CompletedCount = 0;
            TotalCount = 0;
            CurrentEntry = "";
            _assetFacade = null;
            FailedEntries.Clear();
            LoadMilliseconds.Clear();
            RenderMilliseconds.Clear();
            LastRunStatus = "Running";
            try
            {
                await GenerateAllCoreAsync();
                LastRunStatus = FailedEntries.Count == 0
                    ? "Success"
                    : $"Success with {FailedEntries.Count} failed entries: " + string.Join(", ", FailedEntries);
                Logger.Message("Preview thumbnail generation finished: " + LastRunStatus);
            }
            finally
            {
                IsRunning = false;
                _assetFacade = null;
                if (LoadMilliseconds.Count > 0)
                {
                    Logger.Message($"Preview thumbnails timing: {LoadMilliseconds.Count} entries, " +
                              $"load avg {LoadMilliseconds.Average():F0}ms max {LoadMilliseconds.Max():F0}ms, " +
                              $"render avg {RenderMilliseconds.Average():F0}ms max {RenderMilliseconds.Max():F0}ms");
                }
                EditorUtility.ClearProgressBar();
            }
        }

        private static async void RunAllAsync()
        {
            try
            {
                await GenerateAllAsync();
            }
            catch (OperationCanceledException)
            {
                LastRunStatus = "Cancelled";
                Logger.Message("Preview thumbnail generation cancelled");
            }
            catch (Exception exception)
            {
                LastRunStatus = "Failed: " + exception.Message;
                Logger.Exception(exception);
            }
            finally
            {
            }
        }

        private static async Task GenerateAllCoreAsync()
        {
            // without a graphics device (-nographics) camera.Render and Blit are silent no-ops
            // that produce all-white atlases; fail loudly instead of caching garbage
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                throw new InvalidOperationException(
                    "Preview thumbnail generation requires a graphics device (editor launched with -nographics?)");
            }

            byte[] xmlBytes = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, ObjectsXmlLocation));
            XmlDocument document = new XmlDocument();
            document.LoadXml(Encoding.UTF8.GetString(xmlBytes));
            string xmlSha256;
            using (SHA256 sha256 = SHA256.Create())
            {
                xmlSha256 = BitConverter.ToString(sha256.ComputeHash(xmlBytes)).Replace("-", string.Empty);
            }

            NewSceneMode sceneMode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
            Scene previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, sceneMode);
            try
            {
                SetupLighting();
                _camera = CreateThumbnailCamera();

                List<PreviewEntry> floors = CollectFloors(document);
                List<PreviewEntry> walls = CollectWalls(document);
                List<PreviewEntry> objects = CollectObjects(document);
                List<GroundPreviewEntry> grounds = CollectGrounds(document);
                TotalCount = floors.Count + walls.Count + objects.Count + grounds.Count;
                CompletedCount = 0;

                List<string> writtenPaths = new List<string>();
                if (floors.Count > 0)
                {
                    // first camera.Render() of a batch run samples freshly uploaded textures wrong on
                    // software rasterizers (llvmpipe/swrast CI) - discard that render as warm-up
                    await WarmUpRenderAsync(floors[0], LayerMasks.FloorRoofLayer);
                }
                writtenPaths.AddRange(await GenerateCategoryAsync("floors", floors, LayerMasks.FloorRoofLayer,
                    entry => entry.NormalModelElement, xmlSha256));
                writtenPaths.AddRange(await GenerateCategoryAsync("walls", walls, LayerMasks.WallLayer,
                    entry => entry.NormalModelElement ?? entry.BottomModelElement, xmlSha256));
                writtenPaths.AddRange(await GenerateCategoryAsync("objects", objects, LayerMasks.DecorationLayer,
                    entry => entry.NormalModelElement, xmlSha256));
                writtenPaths.AddRange(await GenerateGroundsAsync(grounds, xmlSha256));

                ImportWrittenAtlases(writtenPaths);
            }
            finally
            {
                EditorSceneManager.CloseScene(previewScene, true);
            }
        }

        private static WurmAssetFacade AssetFacade
        {
            get
            {
                _assetFacade ??= new WurmAssetFacade(LoaderSource);
                return _assetFacade;
            }
        }

        private static void SetupLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);

            Vector3 viewDirection = OffsetFromAngles().normalized;
            Quaternion keyRotation = Quaternion.LookRotation(viewDirection) * Quaternion.Euler(15f, 15f, 0f);
            Quaternion fillRotation = Quaternion.LookRotation(-viewDirection) * Quaternion.Euler(20f, 0f, 0f);
            CreateDirectionalLight("PreviewKeyLight", keyRotation, new Color(1f, 0.98f, 0.95f), 1.35f);
            CreateDirectionalLight("PreviewFillLight", fillRotation, new Color(0.85f, 0.9f, 1f), 0.4f);
        }

        private static void CreateDirectionalLight(string name, Quaternion rotation, Color color, float intensity)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.rotation = rotation;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        private static Camera CreateThumbnailCamera()
        {
            GameObject cameraObject = new GameObject("PreviewThumbnailCamera");
            cameraObject.hideFlags = HideFlags.HideInHierarchy;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.aspect = 1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.cullingMask = ~0;
            return camera;
        }

        private static List<PreviewEntry> CollectFloors(XmlDocument document)
        {
            List<PreviewEntry> entries = new List<PreviewEntry>();
            HashSet<string> seenShortNames = new HashSet<string>();

            foreach (XmlElement element in document.GetElementsByTagName("floor"))
            {
                if (!VerifyShortName(element, seenShortNames))
                {
                    continue;
                }

                entries.Add(new PreviewEntry(element.GetAttribute("shortname"), GetFirstChild(element, "model"), null));
            }

            return entries;
        }

        private static List<PreviewEntry> CollectWalls(XmlDocument document)
        {
            List<PreviewEntry> entries = new List<PreviewEntry>();
            HashSet<string> seenShortNames = new HashSet<string>();

            foreach (XmlElement element in document.GetElementsByTagName("wall"))
            {
                if (!VerifyShortName(element, seenShortNames))
                {
                    continue;
                }

                XmlElement normalModel = null;
                XmlElement bottomModel = null;
                foreach (XmlElement child in element.GetElementsByTagName("model"))
                {
                    if (child.GetAttribute("tag") == "bottom")
                    {
                        bottomModel = child;
                    }
                    else
                    {
                        normalModel = child;
                    }
                }

                entries.Add(new PreviewEntry(element.GetAttribute("shortname"), normalModel, bottomModel));
            }

            return entries;
        }

        private static List<PreviewEntry> CollectObjects(XmlDocument document)
        {
            List<PreviewEntry> entries = new List<PreviewEntry>();
            HashSet<string> seenShortNames = new HashSet<string>();

            foreach (XmlElement element in document.GetElementsByTagName("object"))
            {
                if (!VerifyShortName(element, seenShortNames))
                {
                    continue;
                }

                entries.Add(new PreviewEntry(element.GetAttribute("shortname"), GetFirstChild(element, "model"), null));
            }

            return entries;
        }

        private static List<GroundPreviewEntry> CollectGrounds(XmlDocument document)
        {
            List<GroundPreviewEntry> entries = new List<GroundPreviewEntry>();
            HashSet<string> seenShortNames = new HashSet<string>();
            foreach (XmlElement element in document.GetElementsByTagName("ground"))
            {
                if (!VerifyShortName(element, seenShortNames))
                {
                    continue;
                }

                string location = null;
                foreach (XmlElement textureElement in element.GetElementsByTagName("tex"))
                {
                    string target = textureElement.GetAttribute("target");
                    if (string.IsNullOrEmpty(target) || target == "editmode")
                    {
                        location = textureElement.GetAttribute("location");
                        break;
                    }
                }
                entries.Add(new GroundPreviewEntry(element.GetAttribute("shortname"), location));
            }
            return entries;
        }

        private static bool VerifyShortName(XmlElement element, HashSet<string> seenShortNames)
        {
            string shortName = element.GetAttribute("shortname");
            if (seenShortNames.Contains(shortName))
            {
                Logger.Warning($"Preview thumbnails: duplicate shortname {shortName}, skipping");
                return false;
            }

            seenShortNames.Add(shortName);
            return true;
        }

        private static XmlElement GetFirstChild(XmlElement element, string childName)
        {
            XmlNodeList children = element.GetElementsByTagName(childName);
            return children.Count > 0 ? (XmlElement) children[0] : null;
        }

        private static async Task<List<string>> GenerateCategoryAsync(string categoryName, List<PreviewEntry> entries,
            int layer, Func<PreviewEntry, XmlElement> modelSelector, string xmlSha256)
        {
            int columns = Mathf.CeilToInt(Mathf.Sqrt(entries.Count));
            int rows = Mathf.CeilToInt(entries.Count / (float) columns);

            Dictionary<string, int> remainingUses = new Dictionary<string, int>();
            foreach (PreviewEntry entry in entries)
            {
                XmlElement modelElement = modelSelector(entry);
                if (modelElement != null)
                {
                    string location = modelElement.GetAttribute("location");
                    remainingUses[location] = remainingUses.GetValueOrDefault(location) + 1;
                }
            }

            Color[][] cellPixels = new Color[entries.Count][];

            for (int index = 0; index < entries.Count; index++)
            {
                PreviewEntry entry = entries[index];
                CurrentEntry = categoryName + "/" + entry.ShortName;
                if (EditorUtility.DisplayCancelableProgressBar("Generating preview thumbnails",
                        $"{CurrentEntry} ({CompletedCount}/{TotalCount})", CompletedCount / (float) TotalCount))
                {
                    throw new OperationCanceledException();
                }

                cellPixels[index] = await RenderEntryCellAsync(entry, layer, modelSelector, remainingUses);
                CompletedCount++;

                // drop loader caches periodically so texture memory stays bounded; unreferenced
                // native memory gets reclaimed by the editor's own unused-asset unloads
                if (CompletedCount % 100 == 0)
                {
                    _assetFacade = null;
                }
            }

            PreviewAtlasManifest manifest = CreateManifest(categoryName, entries.Select(entry => entry.ShortName), columns,
                xmlSha256);
            return WriteAtlasOutputs(categoryName, cellPixels, columns, rows, manifest);
        }

        private static async Task<List<string>> GenerateGroundsAsync(List<GroundPreviewEntry> entries, string xmlSha256)
        {
            int columns = Mathf.CeilToInt(Mathf.Sqrt(entries.Count));
            int rows = Mathf.CeilToInt(entries.Count / (float) columns);
            Color[][] cellPixels = new Color[entries.Count][];
            AggregateTextureLoader textureLoader = new AggregateTextureLoader(Logger);

            for (int index = 0; index < entries.Count; index++)
            {
                GroundPreviewEntry entry = entries[index];
                CurrentEntry = "grounds/" + entry.ShortName;
                if (EditorUtility.DisplayCancelableProgressBar("Generating preview thumbnails",
                        $"{CurrentEntry} ({CompletedCount}/{TotalCount})", CompletedCount / (float) TotalCount))
                {
                    throw new OperationCanceledException();
                }

                cellPixels[index] = await RenderGroundCellAsync(entry, textureLoader);
                CompletedCount++;
            }

            PreviewAtlasManifest manifest = CreateManifest("grounds", entries.Select(entry => entry.ShortName), columns,
                xmlSha256);
            return WriteAtlasOutputs("grounds", cellPixels, columns, rows, manifest);
        }

        private static PreviewAtlasManifest CreateManifest(string categoryName, IEnumerable<string> shortNames,
            int columns, string inputsHash)
        {
            PreviewAtlasManifest manifest = new PreviewAtlasManifest
            {
                generatorVersion = GeneratorVersion,
                cellSize = CellSize,
                columns = columns,
                category = categoryName,
                inputsHash = inputsHash,
            };
            int index = 0;
            foreach (string shortName in shortNames)
            {
                manifest.entries.Add(new PreviewAtlasEntry { index = index, shortName = shortName });
                index++;
            }
            return manifest;
        }

        private static async Task<Color[]> RenderGroundCellAsync(GroundPreviewEntry entry,
            AggregateTextureLoader textureLoader)
        {
            if (string.IsNullOrEmpty(entry.TextureLocation))
            {
                Logger.Warning("Preview thumbnails: " + entry.ShortName + " has no edit-mode ground texture");
                return CreateMissingGroundCell();
            }

            string fullPath = Path.Combine(Application.streamingAssetsPath, entry.TextureLocation).Replace("\\", "/");
            if (!File.Exists(fullPath))
            {
                Logger.Warning("Preview thumbnails: missing ground texture " + entry.TextureLocation);
                return CreateMissingGroundCell();
            }

            Texture2D texture = null;
            RenderTexture target = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                texture = await textureLoader.LoadTextureAsync(fullPath, true);
                if (!texture)
                {
                    return CreateMissingGroundCell();
                }
                target = RenderTexture.GetTemporary(RenderResolution, RenderResolution, 0, RenderTextureFormat.ARGB32);
                UnityEngine.Graphics.Blit(texture, target);
                RenderTexture.active = target;
                Texture2D readable = new Texture2D(RenderResolution, RenderResolution, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, RenderResolution, RenderResolution), 0, 0);
                readable.Apply();
                Color[] pixels = readable.GetPixels();
                Object.DestroyImmediate(readable);
                return pixels;
            }
            catch (Exception exception)
            {
                Logger.Warning("Preview thumbnails: " + entry.ShortName + " ground texture failed: " + exception.Message);
                return CreateMissingGroundCell();
            }
            finally
            {
                RenderTexture.active = previous;
                if (target)
                {
                    RenderTexture.ReleaseTemporary(target);
                }
                if (texture && texture != Texture2D.whiteTexture)
                {
                    Object.DestroyImmediate(texture);
                }
            }
        }

        private static Color[] CreateMissingGroundCell()
        {
            Color[] pixels = new Color[RenderResolution * RenderResolution];
            Array.Fill(pixels, Color.magenta);
            return pixels;
        }

        /// <summary>Returns cell pixels, or null for a valid empty cell (entry without a model).</summary>
        private static async Task WarmUpRenderAsync(PreviewEntry entry, int layer)
        {
            XmlElement modelElement = entry.NormalModelElement;
            if (modelElement == null)
            {
                return;
            }

            Dictionary<string, int> remainingUses = new Dictionary<string, int>
            {
                [modelElement.GetAttribute("location")] = 1,
            };
            await TryRenderEntryCellAsync(entry, layer, e => e.NormalModelElement, remainingUses);
        }

        private static async Task<Color[]> RenderEntryCellAsync(PreviewEntry entry, int layer,
            Func<PreviewEntry, XmlElement> modelSelector, Dictionary<string, int> remainingUses)
        {
            Color[] pixels = await TryRenderEntryCellAsync(entry, layer, modelSelector, remainingUses);
            if (pixels != null)
            {
                return pixels;
            }

            _assetFacade = null;
            CurrentEntry += " (retrying)";
            return await TryRenderEntryCellAsync(entry, layer, modelSelector, remainingUses);
        }

        private static async Task<Color[]> TryRenderEntryCellAsync(PreviewEntry entry, int layer,
            Func<PreviewEntry, XmlElement> modelSelector, Dictionary<string, int> remainingUses)
        {
            XmlElement modelElement = modelSelector(entry);
            if (modelElement == null)
            {
                Logger.Warning($"Preview thumbnails: {entry.ShortName} has no model, leaving cell empty");
                return null;
            }

            string location = modelElement.GetAttribute("location");
            string fullPath = Application.streamingAssetsPath + "/" + location;
            if (string.IsNullOrEmpty(location) || !File.Exists(fullPath))
            {
                Logger.Warning($"Preview thumbnails: {entry.ShortName} model file missing: {location}, leaving cell empty");
                return null;
            }

            ModelHandle model = AssetFacade.GetModel(modelElement, layer);

            GameObject instance;
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                instance = await LoadModelInstanceAsync(model);
            }
            catch (Exception exception)
            {
                Logger.Warning($"Preview thumbnails: {entry.ShortName} load failed: {exception.Message}");
                return null;
            }

            if (instance == null)
            {
                Logger.Warning($"Preview thumbnails: {entry.ShortName} load timed out: {location}");
                return null;
            }

            // render far from the origin so any stray content in other loaded scenes
            // (leftover editor probes, hidden runtime objects) can never photobomb a cell
            instance.transform.position = new Vector3(0f, -10000f - CompletedCount % 1000, 0f);

            LoadMilliseconds.Add(stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();

            Color[] pixels;
            try
            {
                pixels = RenderInstanceToPixels(instance);
            }
            catch (Exception exception)
            {
                Logger.Warning($"Preview thumbnails: {entry.ShortName} render failed: {exception.Message}");
                return null;
            }
            finally
            {
                Object.DestroyImmediate(instance);
                // destroy the master copy once its last entry is done - shared models stay cached
                // until then, unique models are freed immediately instead of accumulating
                string usedLocation = modelElement.GetAttribute("location");
                if (remainingUses.TryGetValue(usedLocation, out int remaining) && remaining <= 1)
                {
                    remainingUses.Remove(usedLocation);
                    if (model.OriginalModel)
                    {
                        Object.DestroyImmediate(model.OriginalModel);
                    }
                }
                else if (remaining > 1)
                {
                    remainingUses[usedLocation] = remaining - 1;
                }
            }

            RenderMilliseconds.Add(stopwatch.ElapsedMilliseconds);
            return pixels;
        }

        private static async Task<GameObject> LoadModelInstanceAsync(ModelHandle model)
        {
            TaskCompletionSource<GameObject> completion = new TaskCompletionSource<GameObject>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            model.CreateOrGetModel(loaded => completion.TrySetResult(loaded));

            Task finishedTask = await Task.WhenAny(completion.Task, Task.Delay(ModelLoadTimeout));
            if (finishedTask != completion.Task)
            {
                return null;
            }

            return completion.Task.Result;
        }

        private static Color[] RenderInstanceToPixels(GameObject instance)
        {
            Bounds bounds = CalculateBounds(instance);
            if (bounds.extents.sqrMagnitude <= Mathf.Epsilon)
            {
                Logger.Warning($"Preview thumbnails: {instance.name} has empty bounds, leaving cell empty");
                return null;
            }

            Camera camera = _camera;

            float radius = bounds.extents.magnitude;
            Vector3 viewDirection = OffsetFromAngles().normalized;
            float distance = radius * 4f + 10f;
            Vector3 cameraPosition = bounds.center - viewDirection * distance;
            Transform cameraTransform = camera.transform;
            cameraTransform.position = cameraPosition;
            cameraTransform.rotation = Quaternion.LookRotation(viewDirection);
            camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2f);
            camera.farClipPlane = distance + radius * 2f + 1f;
            camera.aspect = 1f;
            camera.orthographicSize = ComputeFitOrthoSize(cameraTransform, bounds) * FitMargin;

            RenderTexture renderTexture = RenderTexture.GetTemporary(RenderResolution, RenderResolution, 24,
                RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            Texture2D rendered = new Texture2D(RenderResolution, RenderResolution, TextureFormat.RGBA32, false);
            rendered.ReadPixels(new Rect(0f, 0f, RenderResolution, RenderResolution), 0, 0);
            rendered.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(renderTexture);

            Color[] pixels = rendered.GetPixels();
            Object.DestroyImmediate(rendered);
            return pixels;
        }

        /// <summary>
        /// Camera offset direction from the model center: elevation degrees up, azimuth degrees to the
        /// side of straight-on front view (front assumed to face -Z).
        /// </summary>
        private static Vector3 OffsetFromAngles()
        {
            float elevationRadians = CameraElevationDeg * Mathf.Deg2Rad;
            float azimuthRadians = CameraAzimuthDeg * Mathf.Deg2Rad;
            return new Vector3(
                -Mathf.Sin(azimuthRadians) * Mathf.Cos(elevationRadians),
                Mathf.Sin(elevationRadians),
                -Mathf.Cos(azimuthRadians) * Mathf.Cos(elevationRadians));
        }

        private static float ComputeFitOrthoSize(Transform cameraTransform, Bounds bounds)
        {
            Vector3[] corners = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                corners[i] = bounds.min + new Vector3(
                    (i & 1) != 0 ? bounds.size.x : 0f,
                    (i & 2) != 0 ? bounds.size.y : 0f,
                    (i & 4) != 0 ? bounds.size.z : 0f);
            }

            float maxAbsX = 0f;
            float maxAbsY = 0f;
            foreach (Vector3 corner in corners)
            {
                Vector3 localCorner = cameraTransform.InverseTransformPoint(corner);
                maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(localCorner.x));
                maxAbsY = Mathf.Max(maxAbsY, Mathf.Abs(localCorner.y));
            }

            // square aspect: orthographic size is the half-height, half-width equals it at aspect 1
            return Mathf.Max(maxAbsY, maxAbsX);
        }

        private static Bounds CalculateBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return default;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void BlitCell(Color[] source, Texture2D atlas, int column, int row)
        {
            Color[] cell = new Color[CellSize * CellSize];
            int ratio = RenderResolution / CellSize;

            for (int y = 0; y < CellSize; y++)
            {
                for (int x = 0; x < CellSize; x++)
                {
                    float redSum = 0f;
                    float greenSum = 0f;
                    float blueSum = 0f;
                    float alphaSum = 0f;

                    for (int subY = 0; subY < ratio; subY++)
                    {
                        for (int subX = 0; subX < ratio; subX++)
                        {
                            Color sourcePixel = source[(y * ratio + subY) * RenderResolution + x * ratio + subX];
                            float weight = sourcePixel.a;
                            redSum += sourcePixel.r * weight;
                            greenSum += sourcePixel.g * weight;
                            blueSum += sourcePixel.b * weight;
                            alphaSum += weight;
                        }
                    }

                    int outputIndex = y * CellSize + x;
                    if (alphaSum > 0f)
                    {
                        float blockArea = ratio * ratio;
                        cell[outputIndex] = new Color(redSum / alphaSum, greenSum / alphaSum, blueSum / alphaSum,
                            alphaSum / blockArea);
                    }
                    else
                    {
                        cell[outputIndex] = Color.clear;
                    }
                }
            }

            atlas.SetPixels(column * CellSize, row * CellSize, CellSize, CellSize, cell);
        }

        private static List<string> WriteAtlasOutputs(string categoryName, Color[][] cellPixels, int columns, int rows,
            PreviewAtlasManifest manifest)
        {
            Texture2D atlas = new Texture2D(columns * CellSize, rows * CellSize, TextureFormat.RGBA32, false);
            Color[] clearPixels = new Color[atlas.width * atlas.height];
            atlas.SetPixels(clearPixels);
            for (int index = 0; index < cellPixels.Length; index++)
            {
                if (cellPixels[index] != null)
                {
                    BlitCell(cellPixels[index], atlas, index % columns, index / columns);
                }
            }
            atlas.Apply();

            Directory.CreateDirectory(OutputFolder);
            string pngPath = Path.Combine(OutputFolder, categoryName + ".png").Replace("\\", "/");
            File.WriteAllBytes(pngPath, atlas.EncodeToPNG());
            File.WriteAllText(Path.Combine(OutputFolder, categoryName + ".json"), JsonUtility.ToJson(manifest, true));
            Object.DestroyImmediate(atlas);

            Logger.Message($"Preview thumbnails: wrote {pngPath} ({manifest.entries.Count} cells)");
            return new List<string> { pngPath };
        }

        private static void ImportWrittenAtlases(List<string> pngPaths)
        {
            AssetDatabase.Refresh();
            foreach (string pngPath in pngPaths)
            {
                AssetDatabase.ImportAsset(pngPath);
                if (AssetImporter.GetAtPath(pngPath) is TextureImporter importer)
                {
                    importer.mipmapEnabled = false;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
