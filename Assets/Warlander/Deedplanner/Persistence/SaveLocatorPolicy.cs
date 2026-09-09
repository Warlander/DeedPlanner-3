using System;

namespace Warlander.Deedplanner.Persistence
{
    public static class SaveLocatorPolicy
    {
        public static string GetAvailable(
            string suggestedName, ISaveNameSanitizer nameSanitizer, Func<string, bool> locationExists)
        {
            string locator = nameSanitizer.Sanitize(suggestedName) + ".MAP";
            if (locationExists(locator))
            {
                throw new InvalidOperationException(
                    $"A save named '{suggestedName}' already exists. Choose a different name or delete the existing save.");
            }

            return locator;
        }
    }
}
