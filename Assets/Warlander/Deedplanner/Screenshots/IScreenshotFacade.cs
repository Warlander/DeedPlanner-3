using System.Threading.Tasks;
using Warlander.Deedplanner.Data;

namespace Warlander.Deedplanner.Screenshots
{
    public interface IScreenshotFacade
    {
        Task CaptureAndSaveCurrentViewAsync();
        Task CaptureAndSaveWithUIAsync();
        byte[] CaptureThumbnailJpeg(Map map);
    }
}
