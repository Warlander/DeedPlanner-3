using System;

namespace Warlander.Deedplanner.Logic.Saving
{
    public readonly struct SaveLocationStatus
    {
        public readonly bool Exists;
        public readonly DateTime WriteTimeUtc;
        public readonly long SizeBytes;

        public SaveLocationStatus(bool exists, DateTime writeTimeUtc, long sizeBytes)
        {
            Exists = exists;
            WriteTimeUtc = writeTimeUtc;
            SizeBytes = sizeBytes;
        }
    }
}
