using System;
using UnityEngine;

namespace Warlogic.Settings.Ugui
{
    public interface ISettingsWindowView
    {
        event Action SaveClicked;
        event Action DiscardClicked;
        RectTransform TabButtonRoot { get; }
        RectTransform ContentRoot { get; }
        void SetSaveInteractable(bool interactable);
        void SetDiscardInteractable(bool interactable);
    }
}
