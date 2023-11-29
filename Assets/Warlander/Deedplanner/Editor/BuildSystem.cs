using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Warlander.Deedplanner.Editor
{
    public static class BuildSystem
    {
        [MenuItem("Build/All Platforms", false, 0)]
        public static bool BuildAllPlatforms()
        {
            bool standaloneSuccess = BuildAllStandalone();
            bool webSuccess = BuildWeb();

            bool totalSuccess = standaloneSuccess && webSuccess;
            return totalSuccess;
        }
        
        [MenuItem("Build/All Standalone", false, 1)]
        public static bool BuildAllStandalone()
        {
            bool windowsSuccess = BuildWindows64();
            bool linuxSuccess = BuildLinux();
            bool macSuccess = BuildMac();

            bool totalSuccess = windowsSuccess && linuxSuccess && macSuccess;
            return totalSuccess;
        }
        
        [MenuItem("Build/Windows", false, 50)]
        public static bool BuildWindows64()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
            }
            else
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
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
                Debug.Log("SUCCESS BUILD Windows");
                return true;
            } 
            else
            {
                Debug.Log("FAILED BUILD Windows");
                return false;
            }
        }
        
        [MenuItem("Build/Linux", false, 51)]
        public static bool BuildLinux()
        {
            if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
            }
            else
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
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
                Debug.Log("SUCCESS BUILD Linux");
                return true;
            } 
            else
            {
                Debug.Log("FAILED BUILD Linux");
                return false;
            }
        }
        
        [MenuItem("Build/Mac", false, 52)]
        public static bool BuildMac()
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
            }
            else
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            }
            
            BuildPlayerOptions buildOptions = CreateUniversalBuildOptions();
            buildOptions.targetGroup = BuildTargetGroup.Standalone;
            buildOptions.target = BuildTarget.StandaloneOSX;
            buildOptions.locationPathName = "Build/"+ Constants.SimpleTitleString + " Mac.app";
            buildOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                CreateSteamAppId("Build/"+ Constants.SimpleTitleString + " Mac.app/");
                Debug.Log("SUCCESS BUILD Mac");
                return true;
            } 
            else
            {
                Debug.Log("FAILED BUILD Mac");
                return false;
            }
        }
        
        [MenuItem("Build/WebGL", false, 100)]
        public static bool BuildWeb()
        {
            BuildPlayerOptions buildOptions = CreateUniversalBuildOptions();
            buildOptions.targetGroup = BuildTargetGroup.WebGL;
            buildOptions.target = BuildTarget.WebGL;
            buildOptions.locationPathName = "Build/DeedPlanner 3 WebGL";
            buildOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("SUCCESS BUILD WebGL");
                return true;
            } 
            else
            {
                Debug.Log("FAILED BUILD WebGL");
                return false;
            }
        }

        private static BuildPlayerOptions CreateUniversalBuildOptions()
        {
            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = new[] { "Assets/Scenes/LoadingScene.unity", "Assets/Scenes/MainScene.unity" };

            return options;
        }

        private static void CreateSteamAppId(string path)
        {
            if (!Directory.Exists(path))
            {
                Debug.LogError("Invalid directory for Steam app ID: " + path);
                return;
            }
            
            File.WriteAllText(Path.Combine(path, "steam_appid.txt"), Constants.SteamAppId.ToString());
        }
    }
}