using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Beamable.Content;
using Beamable.Editor.ContentService;
using Beamable.Tests.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests
{
    // Exercises real Unity transitions with manually constructed production sessions.
    // Does not resolve BeamContext/BeamEditorContext, run CLI, or prove DI registration.
    public class Issue4866LifecycleReproTests
    {
        const string Key = "Beamable.Issue4866.IsolatedLifecycle.";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator RealPlayExitDisposesSession_WithDomainReload() => Run(false);

        [UnityTest]
        public IEnumerator RealPlayExitDisposesSession_WithoutDomainReload() => Run(true);

        IEnumerator Run(bool disableDomainReload)
        {
            SessionState.SetBool(Key + "saved", true);
            SessionState.SetBool(Key + "oldEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt(Key + "oldOptions", (int)EditorSettings.enterPlayModeOptions);
            SessionState.SetInt(Key + "observed", 0);
            SessionState.SetString(Key + "error", "");
            EditorSettings.enterPlayModeOptionsEnabled = disableDomainReload;
            EditorSettings.enterPlayModeOptions = disableDomainReload ? EnterPlayModeOptions.DisableDomainReload : EnterPlayModeOptions.None;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            yield return new EnterPlayMode();
            Assert.That(EditorApplication.isPlaying, Is.True);
            CreateSessionAndObserveExit();
            yield return new ExitPlayMode();
            Assert.That(SessionState.GetInt(Key + "observed", 0), Is.EqualTo(1), SessionState.GetString(Key + "error", ""));

            // A second real session verifies callbacks do not retain the old owner when reload is off.
            yield return new EnterPlayMode();
            Assert.That(EditorApplication.isPlaying, Is.True);
            CreateSessionAndObserveExit();
            yield return new ExitPlayMode();
            Assert.That(SessionState.GetInt(Key + "observed", 0), Is.EqualTo(2), SessionState.GetString(Key + "error", ""));
            TestContext.WriteLine("CONFIRMED two real Play Mode exits disposed manually constructed sessions and their owned sources. DI initialization and snapshot recapture are outside this test.");
        }

        static void CreateSessionAndObserveExit()
        {
            var config = ScriptableObject.CreateInstance<ContentConfiguration>();
            config.EnableLocalContentInEditor = true;
            config.EditorManifestID = "isolated-4866";
            var session = new EditorLocalContentSession(config, new TestConfigProvider());
            UnityEngine.Object.DestroyImmediate(config);
            // Attach a production source only to observe ownership disposal; do not resolve the real editor singleton.
            var source = new EditorLocalContentSource(true, Path.Combine(Path.GetTempPath(), "beam4866-unused-" + Guid.NewGuid().ToString("N")), "test", "realm", "isolated-4866");
            typeof(EditorLocalContentSession).GetField("_source", Private).SetValue(session, source);
            Assert.That(session.HandlesManifest("isolated-4866"), Is.True);
            Action<PlayModeStateChange> observer = null;
            observer = state =>
            {
                if (state != PlayModeStateChange.ExitingPlayMode) return;
                EditorApplication.playModeStateChanged -= observer;
                // Registered after the production constructor's callback, so actual Unity exit disposes first.
                try
                {
                    bool disposed = (bool)typeof(EditorLocalContentSession).GetField("_disposed", Private).GetValue(session);
                    if (!disposed) throw new Exception("Production session was not disposed on ExitingPlayMode.");
                    Exception failure = null;
                    session.GetManifest("isolated-4866").Error(error => failure = error);
                    if (!(failure is ObjectDisposedException)) throw new Exception("Disposed session did not reject manifest request: " + failure);
                    if (!(bool)typeof(EditorLocalContentSource).GetField("_disposed", Private).GetValue(source))
                        throw new Exception("Session-owned source was not disposed.");
                    SessionState.SetInt(Key + "observed", SessionState.GetInt(Key + "observed", 0) + 1);
                }
                catch (Exception error) { SessionState.SetString(Key + "error", error.ToString()); }
                finally { session.OnDispose(); source.Dispose(); }
            };
            EditorApplication.playModeStateChanged += observer;
        }

        [UnityTearDown]
        public IEnumerator RestoreSettings()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            if (SessionState.GetBool(Key + "saved", false))
            {
                EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + "oldEnabled", false);
                EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(Key + "oldOptions", 0);
            }
            SessionState.EraseBool(Key + "saved");
            SessionState.EraseBool(Key + "oldEnabled");
            SessionState.EraseInt(Key + "oldOptions");
            SessionState.EraseInt(Key + "observed");
            SessionState.EraseString(Key + "error");
        }
    }
}
