using System;
using UnityEngine;
using VContainer.Unity;

namespace Warlander.Deedplanner.Settings
{
    /// <summary>
    /// Applies the quality level setting to Unity's QualitySettings, on startup and on change.
    /// </summary>
    public sealed class QualityLevelApplier : IInitializable, IDisposable
    {
        private readonly GraphicsOptions _graphics;

        public QualityLevelApplier(GraphicsOptions graphics)
        {
            _graphics = graphics;
        }

        public void Initialize()
        {
            Apply(_graphics.QualityLevel);
            _graphics.QualityLevelChanged += OnQualityLevelChanged;
        }

        private void OnQualityLevelChanged()
        {
            Apply(_graphics.QualityLevel);
        }

        private static void Apply(QualityLevel level)
        {
            QualitySettings.SetQualityLevel((int) level, true);
        }

        public void Dispose()
        {
            _graphics.QualityLevelChanged -= OnQualityLevelChanged;
        }
    }
}
