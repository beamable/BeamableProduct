using System;
using System.Collections;
using System.Linq;
using Beamable.Api;
using Beamable.Api.Payments;
using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Platform.Tests.Content;
using Beamable.Tests.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests
{
    // Real Play Mode transitions and context-owned source disposal, without a realm or CLI connection.
    public class Issue4866LifecycleReproTests
    {
        const string Key = "Beamable.Issue4866.IsolatedLifecycle.";

        [UnityTest]
        public IEnumerator RealPlayExitDisposesSource_WithDomainReload() => Run(false);

        [UnityTest]
        public IEnumerator RealPlayExitDisposesSource_WithoutDomainReload() => Run(true);

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
            yield return CreateContextAndObserveExit();
            yield return new ExitPlayMode();
            AssertDisposed(1);

            // Keep expected counts literal: iterator locals do not survive the test runner's domain reload.
            yield return new EnterPlayMode();
            yield return CreateContextAndObserveExit();
            yield return new ExitPlayMode();
            AssertDisposed(2);
        }

        static void AssertDisposed(int count)
        {
            Assert.That(SessionState.GetInt(Key + "observed", 0), Is.EqualTo(count), SessionState.GetString(Key + "error", ""));
            Assert.That(Resources.FindObjectsOfTypeAll<ContentObject>().Any(content => content.name == Key), Is.False,
                "Play Mode exit must destroy the source's owned content objects.");
        }

        static IEnumerator CreateContextAndObserveExit()
        {
            Beam.RuntimeConfigProvider.Pid = null;
            var copy = new LocalWorkingCopy();
            copy.Write(LocalWorkingCopy.ItemId, LocalWorkingCopy.Json(17));
            var context = MockBeamContext.Create(mutateDependencies: builder =>
            {
                builder.RemoveIfExists<IServiceRoutingResolution>();
                builder.RemoveIfExists<IBeamablePurchaser>();
                // Subscription shutdown is covered by the real ContentService routing tests.
                builder.RemoveIfExists<IContentApi>();
                builder.AddSingleton<IContentApi>(new MockContentService());
                builder.RemoveIfExists<ILocalContentSource>();
                builder.AddSingleton<ILocalContentSource>(_ => copy.Source());
            }, onInit: ctx => ctx.AddStandardGuestLoginRequests().AddPubnubRequests().AddSessionRequests());
            yield return context.OnReady.ToYielder();
            var source = context.ServiceProvider.GetService<ILocalContentSource>();
            var request = source.GetContent(LocalWorkingCopy.ItemId, typeof(ContentObject), LocalWorkingCopy.Namespace);
            yield return LocalWorkingCopy.Await(request);
            ((ContentObject)request.GetResult()).name = Key;

            context.OnShutdownComplete += () =>
            {
                try
                {
                    Exception failure = null;
                    source.GetManifest(LocalWorkingCopy.Namespace).Error(error => failure = error);
                    if (!(failure is ObjectDisposedException))
                        throw new Exception("Context shutdown did not dispose its local source: " + failure);
                    SessionState.SetInt(Key + "observed", SessionState.GetInt(Key + "observed", 0) + 1);
                }
                catch (Exception error) { SessionState.SetString(Key + "error", error.ToString()); }
                finally { copy.Dispose(); }
            };
        }

        [TearDown]
        public void RestoreOptions()
        {
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
