using System;
using Warlander.Deedplanner.Persistence;
using VContainer.Unity;

namespace Warlander.Deedplanner.Editing
{
    public sealed class SymmetrySession : IInitializable, IDisposable
    {
        private readonly MapHandler _mapHandler;
        private int? _verticalAxisCoordinate2;
        private int? _horizontalAxisCoordinate2;
        private bool _verticalEnabled;
        private bool _horizontalEnabled;

        public event Action Changed = delegate { };

        public bool HasVerticalAxis => _verticalAxisCoordinate2.HasValue;
        public bool HasHorizontalAxis => _horizontalAxisCoordinate2.HasValue;
        public bool IsVerticalEnabled => _verticalEnabled;
        public bool IsHorizontalEnabled => _horizontalEnabled;

        public SymmetrySession(MapHandler mapHandler)
        {
            _mapHandler = mapHandler;
        }

        void IInitializable.Initialize()
        {
            _mapHandler.MapInitialized += Clear;
        }

        void IDisposable.Dispose()
        {
            _mapHandler.MapInitialized -= Clear;
        }

        public bool TryGetVerticalAxis(out int axisCoordinate2)
        {
            axisCoordinate2 = _verticalAxisCoordinate2.GetValueOrDefault();
            return _verticalAxisCoordinate2.HasValue;
        }

        public bool TryGetHorizontalAxis(out int axisCoordinate2)
        {
            axisCoordinate2 = _horizontalAxisCoordinate2.GetValueOrDefault();
            return _horizontalAxisCoordinate2.HasValue;
        }

        public void PlaceVerticalAxis(int axisCoordinate2)
        {
            _verticalAxisCoordinate2 = axisCoordinate2;
            _verticalEnabled = true;
            Changed();
        }

        public void PlaceHorizontalAxis(int axisCoordinate2)
        {
            _horizontalAxisCoordinate2 = axisCoordinate2;
            _horizontalEnabled = true;
            Changed();
        }

        public bool SetVerticalEnabled(bool enabled)
        {
            if (!_verticalAxisCoordinate2.HasValue || _verticalEnabled == enabled)
            {
                return false;
            }

            _verticalEnabled = enabled;
            Changed();
            return true;
        }

        public bool SetHorizontalEnabled(bool enabled)
        {
            if (!_horizontalAxisCoordinate2.HasValue || _horizontalEnabled == enabled)
            {
                return false;
            }

            _horizontalEnabled = enabled;
            Changed();
            return true;
        }

        public bool ToggleVertical() => SetVerticalEnabled(!_verticalEnabled);
        public bool ToggleHorizontal() => SetHorizontalEnabled(!_horizontalEnabled);

        public SymmetrySnapshot Capture()
        {
            return new SymmetrySnapshot(_verticalEnabled && _verticalAxisCoordinate2.HasValue,
                _verticalAxisCoordinate2.GetValueOrDefault(),
                _horizontalEnabled && _horizontalAxisCoordinate2.HasValue,
                _horizontalAxisCoordinate2.GetValueOrDefault());
        }

        public void Clear()
        {
            bool changed = _verticalAxisCoordinate2.HasValue || _horizontalAxisCoordinate2.HasValue;
            _verticalAxisCoordinate2 = null;
            _horizontalAxisCoordinate2 = null;
            _verticalEnabled = false;
            _horizontalEnabled = false;
            if (changed)
            {
                Changed();
            }
        }
    }
}
