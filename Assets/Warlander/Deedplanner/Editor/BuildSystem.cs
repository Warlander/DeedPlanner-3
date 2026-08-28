using System.IO;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Warlander.Deedplanner.Logging;

namespace Warlander.Deedplanner.Editor
{
    public static class BuildSystem
    {
        public static readonly LogCategory Category = new LogCategory("Builds");

        private static readonly ICategoryLogger Logger = new LoggerSource(new LogLevelFilter()).Create(Category);
        [MenuItem("Build/All Platforms", false, 0)]
        public static void BuildAllPlatforms()
        {
            RunBuildAsync(async () => await BuildAllStandaloneCoreAsync() && BuildWebCore());
        }
        
        [MenuItem("Build/All Standalone", false, 1)]
        public static void BuildAllStandalone()
        {
            RunBuildAsync(BuildAllStandaloneCoreAsync);
        }
        
        [MenuItem("Build/Windows", false, 50)]
        public static void BuildWindows64()
        {
            RunBuildAsync(() => Task.FromResult(BuildWindows64Core()));
        }

        private static bool BuildWindows64Core()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            }
            else
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            }
            
            BuildPlayerOptions buildOptions = CreateUniversalBuildOptions();
            buildOptions.targetGroup = BuildTargetGroup.Standalone;
            buildOptions.target = BuildTarget.StandaloneWindows64;
            buildOptions.locationPathName = "Build/"+ Constants.SimpleTitleString + " Windows/DeedPlanner.exe";
            buildOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                CreateSteamAppId("Build/"+ Constants.SimpleTitleString + " Windows/");
                Logger.Message("SUCCESS BUILD Windows");
                return true;
            } 
            else
            {
                Logger.Message("FAILED BUILD Windows");
                ExitBatchWithError();
                return false;
            }
        }
        
        [MenuItem("Build/Linux", false, 51)]
        public static void BuildLinux()
        {
            RunBuildAsync(() => Task.FromResult(BuildLinuxCore()));
        }

        private static bool BuildLinuxCore()
        {
            if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            }
            else
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            }

            BuildPlayerOptions buildOptions = CreateUniversalBuildOptions();
            buildOptions.targetGroup = BuildTargetGroup.Standalone;
            buildOptions.target = BuildTarget.StandaloneLinux64;
            buildOptions.locationPathName = "Build/"+ Constants.SimpleTitleString + " Linux/DeedPlanner.x86_64";
            buildOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                CreateSteamAppId("Build/"+ Constants.SimpleTitleString + " Linux/");
                Logger.Message("SUCCESS BUILD Linux");
                return true;
            } 
            else
            {
                Logger.Message("FAILED BUILD Linux");
                ExitBatchWithError();
                return false;
            }
        }
        
        [MenuItem("Build/Mac", false, 52)]
        public static void BuildMac()
        {
            RunBuildAsync(() => Task.FromResult(BuildMacCore()));
        }

        private static bool BuildMacCore()
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            }
            else
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            }

#if UNITY_EDITOR_OSX
            // OSXStandalone only exists in editors with the Mac build support module;
            // Linux/Windows CI editor images lack it, so this must be compile-guarded
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64ARM64;
#endif

            BuildPlayerOptions buildOptions = CreateUniversalBuildOptions();
            buildOptions.targetGroup = BuildTargetGroup.Standalone;
            buildOptions.target = BuildTarget.StandaloneOSX;
            buildOptions.locationPathName = "Build/"+ Constants.SimpleTitleString + ".app";
            buildOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                // no steam_appid.txt on Mac: unsealed files in the bundle root make
                // codesign refuse to sign, and Finder launches never read it (CWD=/)
                Logger.Message("SUCCESS BUILD Mac");
                return true;
            }
            else
            {
                Logger.Message("FAILED BUILD Mac");
                ExitBatchWithError();
                return false;
            }
        }

        [MenuItem("Build/WebGL", false, 100)]
        public static void BuildWeb()
        {
            RunBuildAsync(() => Task.FromResult(BuildWebCore()));
        }

        private static bool BuildWebCore()
        {
            // GitHub Pages serves compressed builds without Content-Encoding headers,
            // so the loader cannot boot them. Ship uncompressed instead.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.template = "PROJECT:DeedPlanner";

            BuildPlayerOptions buildOptions = CreateUniversalBuildOptions();
            buildOptions.targetGroup = BuildTargetGroup.WebGL;
            buildOptions.target = BuildTarget.WebGL;
            buildOptions.locationPathName = "Build/DeedPlanner 3 WebGL";
            buildOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Logger.Message("SUCCESS BUILD WebGL");
                return true;
            } 
            else
            {
                Logger.Message("FAILED BUILD WebGL");
                ExitBatchWithError();
                return false;
            }
        }

        private static BuildPlayerOptions CreateUniversalBuildOptions()
        {
            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = new[] { "Assets/Scenes/LoadingScene.unity", "Assets/Scenes/MainScene.unity" };

            return options;
        }

        private static Task<bool> BuildAllStandaloneCoreAsync()
        {
            return Task.FromResult(BuildWindows64Core() && BuildLinuxCore() && BuildMacCore());
        }

        private static async void RunBuildAsync(Func<Task<bool>> build)
        {
            bool success = false;
            try
            {
                if (!PreviewAtlasFreshness.IsFresh(out string reason))
                {
                    Logger.Message("Generating preview atlases before build: " + reason);
                    await PreviewThumbnailGenerator.GenerateAllAsync();
                }
                success = await build();
            }
            catch (Exception exception)
            {
                Logger.Exception(exception);
            }
            finally
            {
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(success ? 0 : 1);
                }
            }
        }

        // -executeMethod ignores return values, so a failed build must exit the
        // batch process explicitly or CI sees exit code 0
        private static void ExitBatchWithError()
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void CreateSteamAppId(string path)
        {
            if (!Directory.Exists(path))
            {
                Logger.Error("Invalid directory for Steam app ID: " + path);
                return;
            }
            
            File.WriteAllText(Path.Combine(path, "steam_appid.txt"), Constants.SteamAppId.ToString());
        }
    }
}
