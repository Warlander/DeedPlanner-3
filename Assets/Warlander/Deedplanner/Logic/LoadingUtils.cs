using System;

namespace Warlander.Deedplanner.Logic
{
    public static class LoadingUtils
    {

        public static string CreateDirectPastebinLink(string originalLink)
        {
            string requestLink;
            if (originalLink.Contains("raw"))
            {
                requestLink = originalLink;
            }
            else
            {
                string pasteId = originalLink.Substring(originalLink.LastIndexOf("/", StringComparison.Ordinal) + 1);
                requestLink = "https://pastebin.com/raw.php?i=" + pasteId;
            }

            if (Properties.Web)
            {
                requestLink = "https://cors-anywhere.herokuapp.com/" + requestLink;
            }

            return requestLink;
        }
        
    }
}