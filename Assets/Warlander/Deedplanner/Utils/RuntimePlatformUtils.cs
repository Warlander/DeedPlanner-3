using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    public static class RuntimePlatformUtils
    {
        public static bool IsDesktopPlatform(this RuntimePlatform platform)
        {
            bool isWindows = platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.WindowsEditor;
            bool isLinux = platform == RuntimePlatform.LinuxPlayer || platform == RuntimePlatform.LinuxEditor;
            bool isMac = platform == RuntimePlatform.OSXPlayer || platform == RuntimePlatform.OSXEditor;

            return isWindows || isLinux || isMac;
        }
    }
}