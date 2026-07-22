using System.IO;
using Beamable.Notifications.Web;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Beamable.Notifications.Web.Editor
{
    /// <summary>
    /// Before a player build, stage this Unity project's <c>.beamable/config.beam.json</c> into
    /// <c>StreamingAssets/beam-config.json</c> so the packaged app carries it and
    /// <see cref="StreamingAssetsServer"/> can serve it to the hosted web app (which resolves its
    /// Beamable realm at runtime and overrides the config baked into the bundle). Cleaned up after the
    /// build so it doesn't linger in the project or shadow the Editor's live read.
    ///
    /// No <c>.beamable</c> in the project → nothing is staged, and the web app uses the config baked
    /// into the bundle. This needs no Node, no RN project, and no host-script code — just a normal
    /// Unity build. In the Editor (Play), <see cref="StreamingAssetsServer"/> reads the live
    /// <c>.beamable</c> directly, so this preprocessor is only for player builds.
    /// </summary>
    public class BeamConfigBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private const string StagedFileName = "beam-config.json";
        private static string StagedPath => Path.Combine(Application.streamingAssetsPath, StagedFileName);

        // Whether THIS processor created the StreamingAssets folder, so cleanup can remove it if empty.
        private bool _createdStreamingAssets;

        public void OnPreprocessBuild(BuildReport report)
        {
            var source = BeamWebConfigLocator.FindConfigPath();
            if (source == null)
            {
                if (File.Exists(StagedPath)) File.Delete(StagedPath);
                Debug.Log("[Beamable.Web] No .beamable/config.beam.json found — the hosted web app will use the config baked into the bundle.");
                return;
            }

            _createdStreamingAssets = !Directory.Exists(Application.streamingAssetsPath);
            Directory.CreateDirectory(Application.streamingAssetsPath);
            File.Copy(source, StagedPath, overwrite: true);
            AssetDatabase.Refresh();
            Debug.Log($"[Beamable.Web] Staged {StagedFileName} from {source} for this build.");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // Remove the staged copy: the build already packaged it, and leaving it would shadow the
            // Editor's live .beamable read and linger in version control.
            try
            {
                if (File.Exists(StagedPath)) File.Delete(StagedPath);
                var meta = StagedPath + ".meta";
                if (File.Exists(meta)) File.Delete(meta);

                if (_createdStreamingAssets
                    && Directory.Exists(Application.streamingAssetsPath)
                    && Directory.GetFileSystemEntries(Application.streamingAssetsPath).Length == 0)
                {
                    Directory.Delete(Application.streamingAssetsPath);
                    var saMeta = Application.streamingAssetsPath + ".meta";
                    if (File.Exists(saMeta)) File.Delete(saMeta);
                }

                AssetDatabase.Refresh();
            }
            catch { /* best-effort cleanup */ }
        }
    }
}
