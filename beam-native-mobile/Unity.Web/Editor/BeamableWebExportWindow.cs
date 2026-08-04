using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Beamable.Notifications.Web.Editor
{
    /// <summary>
    /// Editor tool that stages a React Native (Expo) web build into this Unity project's
    /// StreamingAssets so <see cref="StreamingAssetsServer"/> can serve it to a WebView.
    ///
    /// Pick the RN project folder and press <b>Build &amp; Copy</b>. The tool:
    ///   1. Runs the build command (default <c>npm run export:web</c>) in the RN project through a
    ///      login shell — so GUI-launched Unity resolves node/npm from your PATH — streaming output
    ///      into the window. On error it shows the message but still continues to copy.
    ///   2. Copies <c>dist/</c> into <c>Assets/StreamingAssets/&lt;projectName&gt;/</c>.
    ///   3. Writes <c>manifest.txt</c> (the file list the server needs — Android can't enumerate
    ///      StreamingAssets at runtime).
    ///   4. Writes <c>Assets/StreamingAssets/beamable-webview.json</c> = <c>{"contentFolder":"&lt;projectName&gt;"}</c>,
    ///      which the runtime server reads to know which folder to serve. Hand-edit it to switch bundles.
    ///
    /// Replaces the old <c>build-react-webview.sh</c>; works on macOS and Windows.
    /// </summary>
    public class BeamableWebExportWindow : EditorWindow
    {
        private const string PrefPathKey = "Beamable.Web.RnProjectPath";
        private const string PrefCmdKey = "Beamable.Web.BuildCommand";
        private const string DefaultCmd = "npm run export:web";
        private const string ConfigFileName = "beamable-webview.json";

        private string _rnPath;
        private string _buildCommand;
        private Vector2 _logScroll;
        private readonly StringBuilder _log = new StringBuilder();
        private readonly object _logLock = new object();
        private bool _running;

        private System.Diagnostics.Process _proc;
        private string _projectName;

        [MenuItem("Tools/Beamable/Web Bundle Exporter")]
        public static void Open() => GetWindow<BeamableWebExportWindow>("Web Bundle Exporter");

        private void OnEnable()
        {
            _rnPath = EditorPrefs.GetString(PrefPathKey, "");
            _buildCommand = EditorPrefs.GetString(PrefCmdKey, DefaultCmd);
        }

        private void OnDisable()
        {
            EditorApplication.update -= PollProcess;
            try { if (_proc != null && !_proc.HasExited) _proc.Kill(); }
            catch { /* already gone */ }
            _proc?.Dispose();
            _proc = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("React Native → Unity WebView bundle", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Builds the RN web export, copies it into StreamingAssets/<projectName>/, writes " +
                "manifest.txt, and points beamable-webview.json at it.", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                _rnPath = EditorGUILayout.TextField("RN project path", _rnPath);
                if (EditorGUI.EndChangeCheck()) EditorPrefs.SetString(PrefPathKey, _rnPath);
                if (GUILayout.Button("Browse…", GUILayout.Width(80)))
                {
                    var start = !string.IsNullOrEmpty(_rnPath) && Directory.Exists(_rnPath)
                        ? _rnPath : Application.dataPath;
                    var picked = EditorUtility.OpenFolderPanel("Select the React Native project", start, "");
                    if (!string.IsNullOrEmpty(picked)) { _rnPath = picked; EditorPrefs.SetString(PrefPathKey, _rnPath); }
                }
            }

            EditorGUI.BeginChangeCheck();
            _buildCommand = EditorGUILayout.TextField("Build command", _buildCommand);
            if (EditorGUI.EndChangeCheck()) EditorPrefs.SetString(PrefCmdKey, _buildCommand);

            using (new EditorGUI.DisabledScope(_running))
            {
                if (GUILayout.Button(_running ? "Working…" : "Build & Copy", GUILayout.Height(30)))
                    StartBuildAndCopy();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.MinHeight(220));
            string current;
            lock (_logLock) current = _log.ToString();
            EditorGUILayout.TextArea(current, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void AppendLog(string line) { lock (_logLock) _log.AppendLine(line); }

        private void StartBuildAndCopy()
        {
            if (string.IsNullOrEmpty(_rnPath) || !Directory.Exists(_rnPath))
            {
                EditorUtility.DisplayDialog("Web Bundle Exporter",
                    "Pick a valid React Native project folder first.", "OK");
                return;
            }

            lock (_logLock) _log.Clear();
            _projectName = new DirectoryInfo(_rnPath.TrimEnd('/', '\\')).Name;
            _running = true;
            AppendLog($"→ Building web bundle in {_rnPath}");
            AppendLog($"  command: {_buildCommand}");

            try
            {
                StartProcess();
                EditorApplication.update += PollProcess;
            }
            catch (Exception e)
            {
                AppendLog($"[error] could not start the build: {e.Message}");
                FinishAfterBuild(-1); // per spec: surface the error, then still try to copy
            }
        }

        private void StartProcess()
        {
            string file, args;
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                file = "cmd.exe";
                args = $"/c cd /d \"{_rnPath}\" && {_buildCommand}";
            }
            else
            {
                // Login shell so a GUI-launched Unity inherits the user's PATH (nvm/homebrew node).
                file = File.Exists("/bin/zsh") ? "/bin/zsh" : "/bin/bash";
                args = $"-lc \"cd '{_rnPath}' && {_buildCommand}\"";
            }

            var psi = new System.Diagnostics.ProcessStartInfo(file, args)
            {
                WorkingDirectory = _rnPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            _proc = new System.Diagnostics.Process { StartInfo = psi };
            _proc.OutputDataReceived += (_, e) => { if (e.Data != null) AppendLog(e.Data); };
            _proc.ErrorDataReceived += (_, e) => { if (e.Data != null) AppendLog(e.Data); };
            _proc.Start();
            _proc.BeginOutputReadLine();
            _proc.BeginErrorReadLine();
        }

        private void PollProcess()
        {
            Repaint();
            if (_proc == null || !_proc.HasExited) return;

            EditorApplication.update -= PollProcess;
            int code = _proc.ExitCode;
            _proc.Dispose();
            _proc = null;
            FinishAfterBuild(code);
        }

        private void FinishAfterBuild(int exitCode)
        {
            if (exitCode != 0)
            {
                AppendLog($"[build] exited with code {exitCode}");
                EditorUtility.DisplayDialog("Web Bundle Exporter",
                    $"The build command failed (exit {exitCode}). See the output log.\n\n" +
                    "Will still copy an existing dist/ if there is one.", "OK");
            }
            else
            {
                AppendLog("[build] ✓ succeeded");
            }

            try
            {
                CopyAndStage();
            }
            catch (Exception e)
            {
                AppendLog($"[error] {e.Message}");
                EditorUtility.DisplayDialog("Web Bundle Exporter", e.Message, "OK");
            }
            finally
            {
                _running = false;
                Repaint();
            }
        }

        private void CopyAndStage()
        {
            var dist = Path.Combine(_rnPath, "dist");
            if (!Directory.Exists(dist))
                throw new Exception($"No dist/ to copy at {dist}. Run the build (or fix the build error) first.");

            var dest = Path.Combine(Application.streamingAssetsPath, _projectName);
            AppendLog($"→ Copying dist → Assets/StreamingAssets/{_projectName}");
            Directory.CreateDirectory(Application.streamingAssetsPath);
            if (Directory.Exists(dest)) Directory.Delete(dest, true);
            CopyDir(dist, dest);

            // manifest.txt: relative, forward-slashed, sorted; excludes itself and any .meta.
            var files = Directory.GetFiles(dest, "*", SearchOption.AllDirectories)
                .Select(f => f.Substring(dest.Length + 1).Replace('\\', '/'))
                .Where(rel => rel != "manifest.txt" && !rel.EndsWith(".meta"))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
            File.WriteAllText(Path.Combine(dest, "manifest.txt"), string.Join("\n", files) + "\n");
            AppendLog($"→ Wrote manifest.txt ({files.Count} files)");

            // Config the runtime server reads to pick the folder to serve.
            File.WriteAllText(
                Path.Combine(Application.streamingAssetsPath, ConfigFileName),
                "{\"contentFolder\":\"" + _projectName + "\"}\n");
            AppendLog($"→ Wrote {ConfigFileName} (contentFolder = {_projectName})");

            AssetDatabase.Refresh();
            AppendLog("✓ Done.");
        }

        private static void CopyDir(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(dst, dir.Substring(src.Length + 1)));
            foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
                File.Copy(file, Path.Combine(dst, file.Substring(src.Length + 1)), true);
        }
    }
}
