using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Warlander.Deedplanner.Utils
{
    public static class WebUtils
    {
        public static byte[] ReadUrlToByteArray(string location)
        {
#if UNITY_WEBGL
            // native JavaScript loading
            if (Properties.Web)
            {
                return JavaScriptUtils.LoadUrlToBytes(location);
            }
#endif

            location = FixLocalPath(location);
            
            using (UnityWebRequest request = UnityWebRequest.Get(location))
            {
                request.SendWebRequest();
                while (!request.isDone && request.result != UnityWebRequest.Result.Success)
                {
                    Thread.Sleep(1);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(request.error + "\nLocation: " + location);
                    return null;
                }

                byte[] data = request.downloadHandler.data;
                return data;
            }
        }

        public static void ReadUrlToByteArray(string location, Action<byte[]> onLoaded)
        {
            location = location.Replace('\\', '/'); // making sure all paths have uniform format
            location = FixLocalPath(location);

            UnityWebRequest www = UnityWebRequest.Get(location);
            www.SendWebRequest().completed += operation =>
            {
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(www.error + "\nLocation: " + location);
                    onLoaded.Invoke(null);
                }
                else
                {
                    onLoaded.Invoke(www.downloadHandler.data);
                }
            };
        }

        private static string FixLocalPath(string path)
        {
            Uri uri;
            Uri.TryCreate(path, UriKind.Absolute, out uri);
            bool isWebUri = uri != null && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            
            // required for Linux and Mac compatibility, have no effect on Windows
            if (!isWebUri && SystemInfo.deviceType == DeviceType.Desktop)
            {
                return "file://" + path;
            }

            return path;
        }
    }
}