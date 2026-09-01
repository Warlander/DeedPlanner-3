using System.Threading.Tasks;
using Warlander.Deedplanner.Domain;

namespace Warlander.Deedplanner.Screenshots
{
    public interface IScreenshotFacade
    {
        Task CaptureAndSaveCurrentViewAsync();
        Task CaptureAndSaveWithUIAsync();
        byte[] CaptureThumbnailJpeg(Map map);
    }
}
