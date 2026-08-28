namespace Warlander.Deedplanner.Logging
{
    public sealed class LogCategory
    {
        public string Name { get; }

        public LogCategory(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
