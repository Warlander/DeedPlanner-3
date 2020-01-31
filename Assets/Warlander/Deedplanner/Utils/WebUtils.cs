using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Warlander.Deedplanner.Utils
{
    public static class WebUtils
    {
        public static byte[] ReadUrlToByteArray(string location)
        {
            if (Properties.Web)
            {
                return JavaScriptUtils.LoadUrlToBytes(location);
            }

            Uri uri;
            Uri.TryCreate(location, UriKind.Absolute, out uri);
            bool isWebUri = uri != null && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            
            // required for Linux and Mac compatibility, have no effect on Windows
            if (!isWebUri && Application.platform.IsDesktopPlatform())
            {
                location = "file://" + location;
            }
            
            using (UnityWebRequest request = UnityWebRequest.Get(location))
            {
                request.SendWebRequest();
                while (!request.isDone && !request.isHttpError && !request.isNetworkError)
                {
                    Thread.Sleep(1);
                }

                if (request.isHttpError || request.isNetworkError)
                {
                    Debug.LogError(request.error);
                    return null;
                }

                byte[] data = request.downloadHandler.data;
                return data;
            }
        }
    }
}