using System.IO;
using Beamable.Common.Content;
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
            RemoveOldBakedContent();
            
            await ContentIO.BakeContent();
        }

        private void RemoveOldBakedContent()
        {
            if (Directory.Exists(ContentConstants.DecompressedContentPath))
            {
                var directoryInfo = new DirectoryInfo(ContentConstants.DecompressedContentPath);
                foreach (var file in directoryInfo.GetFiles())
                {
                    file.Delete();
                }
            }
            if (File.Exists(ContentConstants.CompressedContentPath))
            {
                File.Delete(ContentConstants.CompressedContentPath);
            }
        }
    }
}
