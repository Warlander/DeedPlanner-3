using System;
using System.Runtime.InteropServices;

namespace Warlander.Deedplanner.Utils
{
    public static class JavaScriptUtils
    {
        [DllImport("__Internal")] private static extern IntPtr LoadResourceNative(string location);
        [DllImport("__Internal")] private static extern int GetLastLoadedResourceLengthNative();
        
        [DllImport("__Internal")] public static extern string GetMapLocationString();
        
        [DllImport("__Internal")] public static extern void DownloadNative(string name, string content);

        [DllImport("__Internal")] public static extern void UploadNative(string objectCallbackName, string methodCallbackName);

        [DllImport("__Internal")] public static extern string PromptNative(string message, string defaultInput);

        public static byte[] LoadUrlToBytes(string url)
        {
            IntPtr pointer = LoadResourceNative(url);
            int length = GetLastLoadedResourceLengthNative();
            byte[] data = new byte[length];
            Marshal.Copy(pointer, data, 0, length);

            return data;
        }
    }
}