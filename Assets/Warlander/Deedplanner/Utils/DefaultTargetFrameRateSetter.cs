using UnityEngine;
using Zenject;

namespace Warlander.Deedplanner.Utils
{
    public class DefaultTargetFrameRateSetter : IInitializable
    {
        void IInitializable.Initialize()
        {
            if (SystemInfo.deviceType == DeviceType.Desktop)
            {
                QualitySettings.vSyncCount = 1;
            }
            else
            {
                Application.targetFrameRate = 60;
            }
        }
    }
}