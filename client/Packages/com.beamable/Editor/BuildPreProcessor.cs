using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Editor.Content;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Beamable.Editor
{
	public class BuildPreProcessor : IPreprocessBuildWithReport
	{
		public int callbackOrder { get; }

#if !UNITY_STANDALONE
		public void OnPreprocessBuild(BuildReport report) { }
#else
        public async void OnPreprocessBuild(BuildReport report)
        {
            if (ContentConfiguration.Instance.BakeContentOnBuild)
            {
                await ContentIO.BakeContent();    
            }
        }
#endif
	}
}
