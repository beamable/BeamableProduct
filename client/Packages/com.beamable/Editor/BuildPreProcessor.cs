using Beamable.Content;
using Beamable.Editor.Content;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Beamable.Editor
{
    public class BuildPreProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; }

        public async void OnPreprocessBuild(BuildReport report)
        {
            if (ContentConfiguration.Instance.BakeContentOnBuild)
            {
                await ContentIO.BakeContent();
            }

            if (CoreConfiguration.Instance.PreventCodeStripping)
            {
				BeamableLinker.GenerateLinkFile();
            }
        }
    }
}
