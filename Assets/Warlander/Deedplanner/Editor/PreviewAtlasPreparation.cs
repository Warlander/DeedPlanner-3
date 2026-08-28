using System;
using System.Threading.Tasks;
using UnityEditor;
using Warlander.Deedplanner.Logging;
using Warlogic.LaunchIntercept;

namespace Warlander.Deedplanner.Editor
{
    public sealed class PreviewAtlasPreparation : ILaunchPreparation
    {
        private const string RegistrationId = "deedplanner.preview-thumbnails";
        private static readonly ICategoryLogger Logger = new LoggerSource(new LogLevelFilter())
            .Create(new LogCategory("PreviewPlayGate"));

        public int Priority => 1000;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            LaunchInterceptRegistration.RegisterPreparation(RegistrationId, new PreviewAtlasPreparation());
        }

        public bool IsRequired() => !PreviewAtlasFreshness.IsFresh(out _);

        public async Task PrepareAsync()
        {
            try
            {
                await PreviewThumbnailGenerator.GenerateAllAsync();
            }
            catch (OperationCanceledException exception)
            {
                const string message = "Preview generation was cancelled. Unity remains in Edit mode.";
                Logger.Warning(message);
                throw new OperationCanceledException(message, exception);
            }
            catch (Exception exception)
            {
                Logger.Exception(exception);
                throw;
            }
        }
    }
}
