using System.IO;
using Beamable.Editor.Content;
using Modules.Content;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Beamable.Editor
{
    public class BuildPreProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; }

        public async void OnPreprocessBuild(BuildReport report)
        {
            // remove old baked files and bake again
            if (Directory.Exists(ContentConfiguration.Instance.DecompressedContentPath))
            {
                var directoryInfo = new DirectoryInfo(ContentConfiguration.Instance.DecompressedContentPath);
                foreach (var file in directoryInfo.GetFiles())
                {
                    file.Delete();
                }
            }
            if (File.Exists(ContentConfiguration.Instance.CompressedContentPath))
            {
                File.Delete(ContentConfiguration.Instance.CompressedContentPath);
            }
            
            await ContentIO.BakeContent();
        }
    }
}
