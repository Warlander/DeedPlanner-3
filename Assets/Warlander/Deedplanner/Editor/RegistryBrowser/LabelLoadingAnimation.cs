using UnityEditor;
using UnityEngine.UIElements;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public class LabelLoadingAnimation
    {
        private readonly Label _label;
        private readonly string _baseText;
        private bool _running;
        private double _lastTick;
        private int _dotCount;

        public LabelLoadingAnimation(Label label, string baseText)
        {
            _label = label;
            _baseText = baseText;
        }

        public void Start()
        {
            if (_running)
                EditorApplication.update -= Tick;
            _running = true;
            _dotCount = 0;
            _lastTick = EditorApplication.timeSinceStartup;
            _label.text = _baseText;
            EditorApplication.update += Tick;
        }

        public void Stop()
        {
            _running = false;
            EditorApplication.update -= Tick;
        }

        private void Tick()
        {
            if (!_running)
                return;
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastTick < 0.5)
                return;
            _lastTick = now;
            _dotCount = (_dotCount + 1) % 4;
            _label.text = _baseText + new string('.', _dotCount);
        }
    }
}
