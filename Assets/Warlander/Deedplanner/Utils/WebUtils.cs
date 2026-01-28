using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Warlander.Deedplanner.Utils
{
    public static class WebUtils
    {
        /*public static byte[] ReadUrlToByteArray(string location)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // native JavaScript loading
            return JavaScriptUtils.LoadUrlToBytes(location);
#else

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
#endif
        }*/

        public static async Task<byte[]> ReadUrlToByteArray(string location)
        {
            location = location.Replace('\\', '/'); // making sure all paths have uniform format
            location = FixLocalPath(location);

            UnityWebRequest www = UnityWebRequest.Get(location);
            await www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error + "\nLocation: " + location);
                return null;
            }
            else
            {
                return www.downloadHandler.data;
            }
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