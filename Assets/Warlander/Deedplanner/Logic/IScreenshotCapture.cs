using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Logic
{
    public interface IScreenshotCapture
    {
        Task<Texture2D> CaptureAsync();
    }
}
