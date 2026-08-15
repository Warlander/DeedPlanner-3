using System;

namespace Warlander.Deedplanner.Logic.Saving
{
    public readonly struct TrackResult
    {
        public readonly bool Exists;
        public readonly DateTime WriteTimeUtc;
        public readonly long SizeBytes;

        public TrackResult(bool exists, DateTime writeTimeUtc, long sizeBytes)
        {
            Exists = exists;
            WriteTimeUtc = writeTimeUtc;
            SizeBytes = sizeBytes;
        }
    }
}
