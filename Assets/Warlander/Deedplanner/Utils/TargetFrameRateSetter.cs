using UnityEngine;
using Zenject;

namespace Warlander.Deedplanner.Utils
{
    public class TargetFrameRateSetter : IInitializable
    {
        private const int DefaultFrameRate = 60;

        void IInitializable.Initialize()
        {
            SetNewFrameRate(DefaultFrameRate);
        }
        
        public void SetNewFrameRate(int frameRate)
        {
            Application.targetFrameRate = frameRate;
        }
    }
}