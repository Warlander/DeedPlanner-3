using System;

namespace Warlander.Deedplanner.Logic
{
    public static class WebLinkUtils
    {
        
        public static string AsDirectPastebinLink(string originalLink)
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

            requestLink = AsCrossOriginLink(requestLink);

            return requestLink;
        }

        public static string AsCrossOriginLink(string originalLink)
        {
            if (Properties.Web)
            {
                return "https://cors-anywhere.herokuapp.com/" + originalLink;
            }

            return originalLink;
        }

    }
}