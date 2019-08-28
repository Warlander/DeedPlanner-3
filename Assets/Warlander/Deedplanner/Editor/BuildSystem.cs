using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Warlander.Deedplanner.Editor
{
    public static class BuildSystem
    {
        
        [MenuItem("Build/All Platforms")]
        public static bool BuildAllPlatforms()
        {
            bool windowsSuccess = BuildWindows64();
            bool linuxSuccess = BuildLinux();
            bool macSuccess = BuildMac();
            bool webSuccess = BuildWeb();

            bool totalSuccess = windowsSuccess && linuxSuccess && macSuccess && webSuccess;
            return totalSuccess;
        }
        
        [MenuItem("Build/Windows")]
        public static bool BuildWindows64()
        {
            BuildPlayerOptions buildOptions = CreateUniversalBuildOptions();
            buildOptions.targetGroup = BuildTargetGroup.Standalone;
            buildOptions.target = BuildTarget.StandaloneWindows64;
            buildOptions.locationPathName = "Build/"+ Constants.TitleString + " Windows/DeedPlanner.exe";
            buildOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("SUCCESS BUILD Windows");
                return true;
            } 
            else
            {
                Debug.Log("FAILED BUILD Windows");
                return false;
            }
        }
        
        [MenuItem("Build/Linux")]
        public static bool BuildLinux()
        {
            BuildPlayerOptions buildOptions = CreateUniversalBuildOptions();
            buildOptions.targetGroup = BuildTargetGroup.Standalone;
            buildOptions.target = BuildTarget.StandaloneLinux64;
            buildOptions.locationPathName = "Build/"+ Constants.TitleString + " Linux/DeedPlanner.x86_64";
            buildOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("SUCCESS BUILD Linux");
                return true;
            } 
            else
            {
                Debug.Log("FAILED BUILD Linux");
                return false;
            }
        }
        
        [MenuItem("Build/Mac")]
        public static bool BuildMac()
        {
            BuildPlayerOptions buildOptions = CreateUniversalBuildOptions();
            buildOptions.targetGroup = BuildTargetGroup.Standalone;
            buildOptions.target = BuildTarget.StandaloneOSX;
            buildOptions.locationPathName = "Build/"+ Constants.TitleString + " Mac.app";
            buildOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("SUCCESS BUILD Mac");
                return true;
            } 
            else
            {
                Debug.Log("FAILED BUILD Mac");
                return false;
            }
        }
        
        [MenuItem("Build/WebGL")]
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
            options.scenes = new[] {"Assets/Scenes/MainScene.unity" };

            return options;
        }

    }
}