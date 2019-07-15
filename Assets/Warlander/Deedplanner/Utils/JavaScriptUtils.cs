using System;
using System.Runtime.InteropServices;

namespace Warlander.Deedplanner.Utils
{
    public class JavaScriptUtils
    {
        
        [DllImport("__Internal")]
        private static extern IntPtr LoadResourceNative(string location);
        [DllImport("__Internal")]
        private static extern int GetLastLoadedResourceLengthNative();

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