using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Screenshots
{
    public interface IScreenshotCapture
    {
        Task<Texture2D> CaptureAsync();
    }
}
