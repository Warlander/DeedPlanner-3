using System.Threading.Tasks;
using UnityEngine;
using Warlander.Deedplanner.Domain;

namespace Warlander.Deedplanner.Screenshots
{
    public class ScreenshotFacade : IScreenshotFacade
    {
        private readonly CurrentViewScreenshotCapture _currentViewCapture;
        private readonly BackbufferScreenshotCapture _backbufferCapture;
        private readonly ScreenshotSaver _saver;
        private readonly DeedThumbnailCapture _thumbnailCapture;

        public ScreenshotFacade(CurrentViewScreenshotCapture currentViewCapture,
            BackbufferScreenshotCapture backbufferCapture, ScreenshotSaver saver,
            DeedThumbnailCapture thumbnailCapture)
        {
            _currentViewCapture = currentViewCapture;
            _backbufferCapture = backbufferCapture;
            _saver = saver;
            _thumbnailCapture = thumbnailCapture;
        }

        public Task CaptureAndSaveCurrentViewAsync()
        {
            return CaptureAndSaveAsync(_currentViewCapture);
        }

        public Task CaptureAndSaveWithUIAsync()
        {
            return CaptureAndSaveAsync(_backbufferCapture);
        }

        public byte[] CaptureThumbnailJpeg(Map map)
        {
            return _thumbnailCapture.CaptureJpeg(map);
        }

        private async Task CaptureAndSaveAsync(IScreenshotCapture capture)
        {
            Texture2D texture = await capture.CaptureAsync();
            if (texture != null)
            {
                _saver.Save(texture);
            }
        }
    }
}
