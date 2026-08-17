namespace Warlander.Deedplanner.Logic.Saving
{
    public readonly struct SaveFeasibility
    {
        public static readonly SaveFeasibility Ok = new SaveFeasibility(true, 0, null);

        public readonly bool Possible;
        public readonly long LimitBytes;
        public readonly string Reason;

        public SaveFeasibility(bool possible, long limitBytes, string reason)
        {
            Possible = possible;
            LimitBytes = limitBytes;
            Reason = reason;
        }
    }
}
