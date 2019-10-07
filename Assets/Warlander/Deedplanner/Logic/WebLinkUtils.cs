using System;

namespace Warlander.Deedplanner.Logic
{
    public static class WebLinkUtils
    {

        /// <summary>
        /// Turn link supplied by user into downloadable link. Supplied formats: Pastebin, Google Drive, Dropbox
        /// </summary>
        /// <param name="originalLink">link to turn into downloadable link</param>
        /// <returns>direct download link for supported formats, or original link</returns>
        public static string ParseToDirectDownloadLink(string originalLink)
        {
            string newLink;
            if (originalLink.Contains("pastebin"))
            {
                newLink = AsDirectPastebinLink(originalLink);
            }
            else if (originalLink.Contains("drive.google"))
            {
                newLink = AsDirectGoogleDriveLink(originalLink);
            }
            else if (originalLink.Contains("dropbox"))
            {
                newLink = AsDirectDropboxLink(originalLink);
            }
            else
            {
                return originalLink;
            }

            return AsCrossOriginLink(newLink);
        }
        
        private static string AsDirectPastebinLink(string originalLink)
        {
            if (originalLink.Contains("raw"))
            {
                return originalLink;
            }
            
            string pasteId = originalLink.Substring(originalLink.LastIndexOf("/", StringComparison.Ordinal) + 1);
            return "https://pastebin.com/raw.php?i=" + pasteId;
        }

        private static string AsDirectGoogleDriveLink(string originalLink)
        {
            const string googleDriveDownloadUrl = "https://drive.google.com/uc?export=download&id=";
            
            if (originalLink.Contains(googleDriveDownloadUrl))
            {
                return originalLink;
            }

            const string primaryKeyword = "drive.google.com";
            string[] splitOriginalLink = originalLink.Split('/');
            int startIndex = Array.FindIndex(splitOriginalLink, part => part == primaryKeyword);

            const int scanSize = 3;
            
            bool canContainKeywords = startIndex != -1 && startIndex + scanSize <= splitOriginalLink.Length;
            bool containsKeywords = canContainKeywords && splitOriginalLink[startIndex + 1] == "file" && splitOriginalLink[startIndex + 2] == "d";
            
            if (!containsKeywords)
            {
                return originalLink;
            }
            
            string fileId = splitOriginalLink[startIndex + 3];
            return googleDriveDownloadUrl + fileId;
        }

        private static string AsDirectDropboxLink(string originalLink)
        {
            string[] querySplit = originalLink.Split('?');
            if (querySplit.Length == 1)
            {
                return originalLink + "?dl=1";
            }
            
            int dlIndex = Array.FindIndex(querySplit, part => part.Contains("dl="));
            if (dlIndex == -1)
            {
                // link contains other query parameters, add new one
                return originalLink + "&dl=1";
            }
            
            // link already have download in its query, make sure it's set to download
            querySplit[dlIndex] = "dl=1";
            string newLink = querySplit[0] + "?" + querySplit[1];
            for (int i = 2; i < querySplit.Length; i++)
            {
                newLink = newLink + "&" + querySplit[i];
            }

            return newLink;
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