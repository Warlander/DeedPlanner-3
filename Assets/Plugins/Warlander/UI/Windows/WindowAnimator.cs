using System;
using UnityEngine;
using Warlander.UI.Windows;

namespace Warlander.UI.Windows
{
    public abstract class WindowAnimator : MonoBehaviour
    {
        public abstract bool ShowingSupported { get; }
        public abstract bool ClosingSupported { get; }

        public abstract void ApplyStartingState();
        public abstract void AnimateShowingWindow(Action onDone);
        public abstract void AnimateClosingWindow(Action onDone);
    }
}