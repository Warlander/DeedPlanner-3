#if UNITY_WEBGL
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Warlander.Deedplanner.Platform.Web
{
    public static class JavaScriptUtils
    {
        [DllImport("__Internal")] private static extern IntPtr LoadResourceNative(string location);
        [DllImport("__Internal")] private static extern int GetLastLoadedResourceLengthNative();
        
        [DllImport("__Internal")] public static extern string GetMapLocationString();
        
        [DllImport("__Internal")] public static extern void DownloadNative(string name, string content);

        [DllImport("__Internal")] private static extern void DownloadBinaryNative(string name, byte[] data, int length);

        public static void DownloadBinary(string name, byte[] data)
        {
            DownloadBinaryNative(name, data, data.Length);
        }

        [DllImport("__Internal")] public static extern void UploadNative(string objectCallbackName, string methodCallbackName);

        [DllImport("__Internal")] public static extern string PromptNative(string message, string defaultInput);

        [DllImport("__Internal")] private static extern int LocalStorageSetItemNative(string key, string value);
        [DllImport("__Internal")] private static extern string LocalStorageGetItemNative(string key);
        [DllImport("__Internal")] private static extern int LocalStorageHasItemNative(string key);
        [DllImport("__Internal")] private static extern void LocalStorageRemoveItemNative(string key);
        [DllImport("__Internal")] private static extern int LocalStorageTotalSizeNative();
        [DllImport("__Internal")] private static extern string LocalStorageGetKeysNative();

        public static bool LocalStorageSetItem(string key, string value) => LocalStorageSetItemNative(key, value) == 1;
        public static string LocalStorageGetItem(string key) => LocalStorageGetItemNative(key);
        public static bool LocalStorageHasItem(string key) => LocalStorageHasItemNative(key) == 1;
        public static void LocalStorageRemoveItem(string key) => LocalStorageRemoveItemNative(key);
        public static int LocalStorageTotalSize() => LocalStorageTotalSizeNative();

        public static string[] LocalStorageGetKeys()
        {
            KeyList parsed = JsonUtility.FromJson<KeyList>(LocalStorageGetKeysNative());
            return parsed?.keys ?? Array.Empty<string>();
        }

        [Serializable]
        private class KeyList
        {
            public string[] keys;
        }

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
#endif
