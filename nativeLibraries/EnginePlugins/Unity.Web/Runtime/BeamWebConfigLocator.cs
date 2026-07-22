using System.IO;
using UnityEngine;

namespace Beamable.Notifications.Web
{
    /// <summary>
    /// Locates this Unity project's Beamable connection config — the CLI's
    /// <c>.beamable/config.beam.json</c> (written by <c>beam init</c>) — by walking up from the
    /// project root. Used at Editor/build time to serve or stage <c>beam-config.json</c> so the
    /// hosted web app can resolve its realm at runtime (see <see cref="StreamingAssetsServer"/> and
    /// the Editor build preprocessor). Intended for Editor/build-time use: a built player has no
    /// project tree, so at player runtime the walk finds nothing and returns null.
    /// </summary>
    public static class BeamWebConfigLocator
    {
        /// <summary>
        /// Absolute path to the nearest <c>.beamable/config.beam.json</c> that carries both a cid and
        /// a pid, or null if none. CLI-metadata-only configs (e.g. a repo root that holds only
        /// telemetry settings) are skipped and the walk continues upward.
        /// </summary>
        public static string FindConfigPath()
        {
            // Application.dataPath is <project>/Assets; start the walk at the project root.
            var dir = Directory.GetParent(Application.dataPath)?.FullName;
            while (!string.IsNullOrEmpty(dir))
            {
                var candidate = Path.Combine(dir, ".beamable", "config.beam.json");
                if (File.Exists(candidate))
                {
                    try
                    {
                        var text = File.ReadAllText(candidate);
                        if (text.Contains("\"cid\"") && text.Contains("\"pid\"")) return candidate;
                    }
                    catch { /* unreadable — keep walking */ }
                }
                dir = Directory.GetParent(dir)?.FullName;
            }
            return null;
        }

        /// <summary>Raw JSON of the nearest usable config (see <see cref="FindConfigPath"/>), or null.</summary>
        public static string ReadConfigJson()
        {
            var path = FindConfigPath();
            if (path == null) return null;
            try { return File.ReadAllText(path); }
            catch { return null; }
        }
    }
}
