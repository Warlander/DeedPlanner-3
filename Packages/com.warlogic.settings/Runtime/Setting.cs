using System;
using System.Collections.Generic;

namespace Warlogic.Settings
{
    internal interface IStoreAttachable
    {
        void AttachStore(ISettingsStore store);
    }

    public abstract class Setting<T> : ISetting<T>, IStoreAttachable
    {
        private T _value;
        private T _stagedValue;
        private ISettingsStore _store;

        public string Key { get; }
        public string Label { get; }
        public string Description { get; }
        public ApplyMode ApplyMode { get; }
        public T DefaultValue { get; }
        public Func<T, bool> Validator { get; set; }
        public Func<bool> EnableCondition { get; set; }

        public event Action Changed;

        protected Setting(string key, string label, T defaultValue, string description = null, ApplyMode applyMode = ApplyMode.Immediate)
        {
            Key = key;
            Label = label;
            DefaultValue = defaultValue;
            Description = description;
            ApplyMode = applyMode;
            _value = defaultValue;
            _stagedValue = defaultValue;
        }

        public T Value
        {
            get => _value;
            set
            {
                if (ApplyMode == ApplyMode.OnSave)
                {
                    StagedValue = value;
                    return;
                }
                ApplyValue(value);
            }
        }

        public T StagedValue
        {
            get => _stagedValue;
            set
            {
                if (ApplyMode == ApplyMode.Immediate)
                {
                    ApplyValue(value);
                    return;
                }
                T prepared = PrepareValue(value);
                if (Validator != null && !Validator(prepared))
                {
                    return;
                }
                _stagedValue = prepared;
            }
        }

        public bool IsDirty => ApplyMode == ApplyMode.OnSave && !EqualityComparer<T>.Default.Equals(_stagedValue, _value);

        public void Commit()
        {
            if (!IsDirty)
            {
                return;
            }
            ApplyValue(_stagedValue);
        }

        public void Revert()
        {
            _stagedValue = _value;
        }

        public bool IsEnabled()
        {
            return EnableCondition == null || EnableCondition();
        }

        void IStoreAttachable.AttachStore(ISettingsStore store)
        {
            _store = store;
            if (store.TryLoad(Key, out string raw))
            {
                T parsed;
                try
                {
                    parsed = PrepareValue(Deserialize(raw));
                }
                catch (Exception)
                {
                    return;
                }
                if (Validator != null && !Validator(parsed))
                {
                    return;
                }
                _value = parsed;
                _stagedValue = parsed;
            }
        }

        protected virtual T PrepareValue(T value)
        {
            return value;
        }

        protected abstract string Serialize(T value);
        protected abstract T Deserialize(string raw);

        private void ApplyValue(T value)
        {
            T prepared = PrepareValue(value);
            if (Validator != null && !Validator(prepared))
            {
                return;
            }
            if (EqualityComparer<T>.Default.Equals(_value, prepared))
            {
                _stagedValue = _value;
                return;
            }
            _value = prepared;
            _stagedValue = prepared;
            _store?.Save(Key, Serialize(prepared));
            Changed?.Invoke();
        }
    }
}
