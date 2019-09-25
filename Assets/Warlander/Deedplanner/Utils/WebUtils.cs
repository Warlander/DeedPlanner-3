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
            else
            {
                UnityWebRequest request = UnityWebRequest.Get(location);
                request.SendWebRequest();
                while (!request.isDone && !request.isHttpError && !request.isNetworkError)
                {
                    Thread.Sleep(1);
                }

                if (request.isHttpError || request.isNetworkError)
                {
                    Debug.LogError(request.error);
                    request.Dispose();
                    return null;
                }

                byte[] data = request.downloadHandler.data;
                request.Dispose();
                return data;
            }
        }
        
    }
}