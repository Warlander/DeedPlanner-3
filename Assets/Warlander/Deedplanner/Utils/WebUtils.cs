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
            // native JavaScript loading
            if (Properties.Web)
            {
                return JavaScriptUtils.LoadUrlToBytes(location);
            }

            location = FixLocalPath(location);
            
            using (UnityWebRequest request = UnityWebRequest.Get(location))
            {
                request.SendWebRequest();
                while (!request.isDone && !request.isHttpError && !request.isNetworkError)
                {
                    Thread.Sleep(1);
                }

                if (request.isHttpError || request.isNetworkError)
                {
                    Debug.LogError(request.error + "\nLocation: " + location);
                    return null;
                }

                byte[] data = request.downloadHandler.data;
                return data;
            }
        }

        public static IEnumerator ReadUrlToByteArray(string location, Action<byte[]> callback)
        {
            location = location.Replace('\\', '/'); // making sure all paths have uniform format
            location = FixLocalPath(location);

            UnityWebRequest www = UnityWebRequest.Get(location);
            yield return www.SendWebRequest();
            
            if (www.isHttpError || www.isNetworkError)
            {
                Debug.LogError(www.error + "\nLocation: " + location);
                callback.Invoke(null);
                yield break;
            }
            
            callback.Invoke(www.downloadHandler.data);
        }

        private static string FixLocalPath(string path)
        {
            Uri uri;
            Uri.TryCreate(path, UriKind.Absolute, out uri);
            bool isWebUri = uri != null && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            
            // required for Linux and Mac compatibility, have no effect on Windows
            if (!isWebUri && Application.platform.IsDesktopPlatform())
            {
                return "file://" + path;
            }

            return path;
        }
    }
}