using System;

namespace Warlogic.Settings
{
    public interface ISetting
    {
        string Key { get; }
        string Label { get; }
        string Description { get; }
        ApplyMode ApplyMode { get; }
        bool IsDirty { get; }
        event Action Changed;
        void Commit();
        void Revert();
        bool IsEnabled();
    }

    public interface ISetting<T> : ISetting
    {
        T Value { get; set; }
        T StagedValue { get; set; }
        T DefaultValue { get; }
    }
}
