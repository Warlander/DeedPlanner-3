using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Warlander.Deedplanner.Editor
{
    public sealed class PreviewAtlasBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => int.MinValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!PreviewAtlasFreshness.IsFresh(out string reason))
            {
                throw new BuildFailedException("Preview atlases are not fresh: " + reason);
            }
        }
    }
}
