using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Beamable.Common;
using Beamable.Common.Api.Realms;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content.Validation;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Content;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.ContentService;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests
{
    public class ContentWatcherSwitchingTests
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        CliContentService service;
        ContentConfiguration config;
        Commands commands;
        readonly List<BeamWebCommandStopTests.Fixture> transports = new();

        [SetUp]
        public void Setup()
        {
            commands = new Commands();
            config = ScriptableObject.CreateInstance<ContentConfiguration>();
            var cli = new BeamCli.BeamCli(null)
            {
                latestConfig = new BeamConfigCommandResult { cid = "test", pid = "realm" },
                pidToRealm = new Dictionary<string, RealmView> { ["realm"] = new RealmView { Cid = "test", Pid = "realm" } }
            };
            var provider = new DependencyBuilder().AddSingleton(cli).AddSingleton(new ValidationContext()).Build();
            var context = new BeamEditorContext();
            Set(context, "<ServiceScope>k__BackingField", provider);
            // The command transport is controlled; watcher startup, callbacks and switching are production code.
            service = (CliContentService)FormatterServices.GetUninitializedObject(typeof(CliContentService));
            foreach (var field in typeof(CliContentService).GetFields(Fields))
                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    field.SetValue(service, Activator.CreateInstance(field.FieldType));
            Set(service, "_cli", commands);
            Set(service, "_beamContext", context);
            Set(service, "_provider", provider);
            Set(service, "_contentConfiguration", config);
            Set(service, "<CurrentProgress>k__BackingField", new ContentOperationProgress());
            var writes = typeof(CliContentService).GetField("_contentWrites", Fields);
            writes.SetValue(service, Activator.CreateInstance(writes.FieldType));
            Set(service, "_contentHistoryChangesRequest", new ContentHistoryRequestSlot<ContentHistorySyncChangelistWrapper>(request => request.Cancel()));
            Set(service, "_contentHistoryPreviewRequest", new ContentHistoryRequestSlot<ContentHistorySyncContentWrapper>(request => request.Cancel()));
            var cache = typeof(CliContentService).GetField("_contentHistoryEntryCache", Fields);
            cache.SetValue(service, Activator.CreateInstance(cache.FieldType));
            service.manifestIdOverride = "global";
        }

        [TestCase(ContentChangeKind.IncomingAddition)]
        [TestCase(ContentChangeKind.IncomingChange)]
        [TestCase(ContentChangeKind.IncomingDeletion)]
        [TestCase(ContentChangeKind.Conflict)]
        [TestCase(ContentChangeKind.Unknown)]
        public void Exit_rechecks_incoming_content_even_when_editor_cache_is_empty(ContentChangeKind kind)
        {
            config.EnableLocalContentInEditor = true;
            commands.ExitEntries = new[] { new LocalContentManifestEntry { FullId = "currency.incoming", ChangeKind = (int)kind, IsInConflict = kind == ContentChangeKind.Conflict } };
            Exception error = null;
            Exit().Error(e => error = e);
            Assert.That(error, Is.Not.Null);
            Assert.That(error.Message, Does.Contain("need review"));
            Assert.That(config.EnableLocalContentInEditor, Is.True);
            Assert.That(commands.Content, Is.Empty);
            Assert.That(commands.Checks.Single().Local, Is.True);
        }

        [TestCase(ContentChangeKind.UpToDate)]
        [TestCase(ContentChangeKind.LocalEdit)]
        public void Exit_preserves_unrelated_local_edits_and_accepts_reconciled_content(ContentChangeKind kind)
        {
            config.EnableLocalContentInEditor = true;
            commands.ExitEntries = new[] { new LocalContentManifestEntry { FullId = "currency.local", ChangeKind = (int)kind } };
            var exit = Exit();
            Assert.That(exit.IsCompleted, Is.True);
            exit.GetResult();
            Assert.That(config.EnableLocalContentInEditor, Is.False);
            Assert.That(commands.Content, Is.Empty, "An inactive watcher must not be activated by exit.");
            Assert.That(commands.Checks.Single().Local, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Exit_does_not_resume_after_cancellation_or_scope_change_during_check(bool cancel)
        {
            config.EnableLocalContentInEditor = true;
            commands.DelayExitCheck = true;
            var closed = false;
            Exception error = null;
            var exit = Exit(() => closed).Error(e => error = e);
            if (cancel) closed = true;
            else service.manifestIdOverride = "other";
            commands.Checks.Single().Emit();
            commands.Checks.Single().Completion.CompleteSuccess();
            Assert.That(exit.IsCompleted, Is.True);
            Assert.That(error, Is.Not.Null);
            Assert.That(config.EnableLocalContentInEditor, Is.True);
            Assert.That(commands.Content, Is.Empty);
        }

        [Test]
        public void Exit_rejects_wrong_scope_or_failed_snapshot()
        {
            config.EnableLocalContentInEditor = true;
            commands.ExitManifestId = "other";
            Exception error = null;
            Exit().Error(e => error = e);
            Assert.That(error, Is.Not.Null);
            Assert.That(config.EnableLocalContentInEditor, Is.True);
            commands.ExitManifestId = "global";
            commands.DelayExitCheck = true;
            error = null;
            Exit().Error(e => error = e);
            commands.Checks.Last().ReportError();
            commands.Checks.Last().Emit();
            commands.Checks.Last().Completion.CompleteSuccess();
            Assert.That(error, Is.Not.Null);
            Assert.That(config.EnableLocalContentInEditor, Is.True);
        }

        [UnityTest]
        public IEnumerator Exit_waits_for_both_paused_watchers_before_resuming()
        {
            config.EnableLocalContentInEditor = true;
            yield return Wait(service.Reload());
            yield return Wait(service.StartContentHistory());
            var exit = Exit();
            Assert.That(commands.Content[0].Cancelled, Is.True);
            Assert.That(commands.History[0].Cancelled, Is.True);
            Assert.That(commands.Content.Count, Is.EqualTo(1));
            Assert.That(config.EnableLocalContentInEditor, Is.True, "Preference stays local until shutdown and final review succeed.");
            Assert.That(commands.Checks, Is.Empty, "The authoritative check must follow watcher shutdown.");
            commands.Content[0].Completion.CompleteSuccess();
            yield return null;
            Assert.That(commands.Content.Count, Is.EqualTo(1));
            commands.History[0].Completion.CompleteSuccess();
            yield return Wait(exit);
            exit.GetResult();
            Assert.That(commands.Content[1].Local, Is.False);
            Assert.That(commands.History[1].Local, Is.False);
            Assert.That(config.EnableLocalContentInEditor, Is.False);
        }

        [UnityTest]
        public IEnumerator Failed_exit_restart_restores_local_preference_and_paused_watcher()
        {
            config.EnableLocalContentInEditor = true;
            yield return Wait(service.Reload());
            commands.FailNextContentStart = true;
            Exception error = null;
            var exit = Exit().Error(e => error = e);
            commands.Content[0].Completion.CompleteSuccess();
            yield return Wait(exit);
            Assert.That(error, Is.Not.Null);
            Assert.That(config.EnableLocalContentInEditor, Is.True);
            Assert.That(commands.Content.Last().Local, Is.True);
            AssertMode("Paused");
        }

        [UnityTest]
        public IEnumerator Cancelling_while_watchers_stop_never_starts_auto_sync()
        {
            config.EnableLocalContentInEditor = true;
            yield return Wait(service.Reload());
            var cancelled = false;
            Exception error = null;
            var exit = Exit(() => cancelled).Error(e => error = e);
            cancelled = true;
            commands.Content[0].Completion.CompleteSuccess();
            yield return Wait(exit);
            Assert.That(error, Is.Not.Null);
            Assert.That(config.EnableLocalContentInEditor, Is.True);
            Assert.That(commands.Checks, Is.Empty);
            Assert.That(commands.Content.All(command => command.Local), Is.True);
        }

        [Test]
        public void Exit_and_refresh_reject_pending_inspector_edits_without_clearing_caches()
        {
            config.EnableLocalContentInEditor = true;
            service.EntriesCache["currency.local"] = new LocalContentManifestEntry { FullId = "currency.local" };
            Set(service, "HasPendingInspectorEdits", new Func<string, bool>(_ => true));
            Exception error = null;
            Exit().Error(e => error = e);
            Assert.That(error, Is.Not.Null);
            error = null;
            var refresh = (Promise)typeof(CliContentService).GetMethod("RefreshLocalModeExit", Fields)
                .Invoke(service, new object[] { "test/realm/global", null });
            refresh.Error(e => error = e);
            Assert.That(error, Is.Not.Null);
            Assert.That(service.EntriesCache.ContainsKey("currency.local"), Is.True);
            Assert.That(commands.Content, Is.Empty);
            Assert.That(commands.Checks, Is.Empty);
            Assert.That(config.EnableLocalContentInEditor, Is.True);
        }

        Promise Exit(Func<bool> cancelled = null) => (Promise)typeof(CliContentService)
            .GetMethod("ExitLocalMode", Fields).Invoke(service, new object[] { "test/realm/global", cancelled });

        [UnityTest]
        public IEnumerator Cancelling_stalled_exit_check_stops_command_and_restores_watchers_without_a_response()
        {
            config.EnableLocalContentInEditor = true;
            yield return Wait(service.Reload());
            commands.DelayExitCheck = true;
            var cancelled = false;
            Exception error = null;
            var exit = Exit(() => cancelled).Error(e => error = e);
            commands.Content[0].Completion.CompleteSuccess();
            yield return null;
            Assert.That(commands.Checks.Count, Is.EqualTo(1));
            cancelled = true;
            // No response and no manual completion of the exit check: cancellation must settle it.
            yield return Wait(exit);
            Assert.That(error, Is.TypeOf<OperationCanceledException>());
            Assert.That(commands.Checks[0].Cancelled, Is.True);
            Assert.That(config.EnableLocalContentInEditor, Is.True);
            Assert.That(commands.Content.Last().Local, Is.True);
            AssertMode("Paused");
            var resumed = commands.Content.Last();
            resumed.OnCancel = () => resumed.Completion.CompleteSuccess();
            yield return Wait(service.Reload());
            AssertMode("Paused");
        }

        [UnityTest]
        public IEnumerator Stalled_exit_check_times_out_and_restores_paused_watchers()
        {
            config.EnableLocalContentInEditor = true;
            commands.DelayExitCheck = true;
            Set(service, "_localModeExitTimeoutMs", 25);
            Exception error = null;
            var exit = Exit().Error(e => error = e);
            yield return Wait(exit);
            Assert.That(error, Is.TypeOf<TimeoutException>());
            Assert.That(commands.Checks[0].Cancelled, Is.True);
            Assert.That(config.EnableLocalContentInEditor, Is.True);
            AssertMode("Paused");
            commands.DelayExitCheck = false;
            var retry = Exit();
            yield return Wait(retry);
            retry.GetResult();
            Assert.That(config.EnableLocalContentInEditor, Is.False);
        }

        [UnityTest]
        public IEnumerator Failed_exit_check_stop_releases_gate_but_keeps_cleanup_ownership_for_retry()
        {
            config.EnableLocalContentInEditor = true;
            commands.DelayExitCheck = true;
            commands.CompleteExitCheckOnCancel = false;
            Set(service, "_localModeExitTimeoutMs", 25);
            Set(service, "_localModeExitStopTimeoutMs", 25);
            Exception error = null;
            var exit = Exit().Error(e => error = e);
            yield return Wait(exit);
            Assert.That(error, Is.TypeOf<TimeoutException>());
            Assert.That(config.EnableLocalContentInEditor, Is.True);
            AssertMode("Failed");
            Assert.That(commands.Content, Is.Empty);
            commands.Checks[0].Completion.CompleteSuccess();
            var retry = service.Reload();
            yield return Wait(retry);
            retry.GetResult();
            Assert.That(commands.Content.Single().Local, Is.True);
            AssertMode("Paused");
        }

        [UnityTest]
        public IEnumerator Stalled_exit_check_is_stopped_when_service_is_disposed()
        {
            config.EnableLocalContentInEditor = true;
            commands.DelayExitCheck = true;
            var exit = Exit().Error(_ => { });
            var dispose = service.OnDispose();
            yield return Wait(dispose);
            yield return Wait(exit);
            dispose.GetResult();
            Assert.That(commands.Checks[0].Cancelled, Is.True);
            Assert.That(commands.Content, Is.Empty);
            Assert.That(config.EnableLocalContentInEditor, Is.True);
        }

        [Test]
        public void Assembly_reload_cancels_owned_exit_check_without_restarting_watchers()
        {
            config.EnableLocalContentInEditor = true;
            commands.DelayExitCheck = true;
            var exit = Exit().Error(_ => { });
            typeof(CliContentService).GetMethod("PrepareForAssemblyReload", Fields).Invoke(service, null);
            Assert.That(commands.Checks[0].Cancelled, Is.True);
            Assert.That(exit.IsCompleted, Is.True);
            Assert.That(exit.IsFailed, Is.True);
            Assert.That(config.EnableLocalContentInEditor, Is.True);
            Assert.That(commands.Content, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Exit_check_cancellation_waits_for_server_acknowledgement_before_restarting()
        {
            config.EnableLocalContentInEditor = true;
            yield return Wait(service.Reload());
            var transport = new BeamWebCommandStopTests.Fixture(transports);
            commands.ExitCheckWebCommand = transport.Command;
            var cancelled = false;
            var exit = Exit(() => cancelled).Error(_ => { });
            commands.Content[0].Completion.CompleteSuccess();
            transport.Discover();
            transport.AllowHeaders.TrySetResult(true);
            cancelled = true;
            var deadline = UnityEditor.EditorApplication.timeSinceStartup + 10;
            while (!transport.StopReceived.Task.IsCompleted && UnityEditor.EditorApplication.timeSinceStartup < deadline)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.That(transport.StopReceived.Task.IsCompleted, Is.True);
            Assert.That(exit.IsCompleted, Is.False);
            Assert.That(commands.Content.Count, Is.EqualTo(1));
            transport.ReleaseWorker.TrySetResult(true);
            transport.AllowAcknowledgement.TrySetResult(true);
            yield return Wait(exit);
            Assert.That(exit.IsFailed, Is.True);
            Assert.That(config.EnableLocalContentInEditor, Is.True);
            Assert.That(commands.Content.Last().Local, Is.True);
            AssertMode("Paused");
        }

        [TestCase(0, false)]
        [TestCase(30, false)]
        [TestCase(30, true)]
        public void Exit_review_keeps_actions_visible_with_long_errors_and_pending_items(int count, bool error)
        {
            var type = typeof(CliContentService).Assembly.GetType("Beamable.Editor.UI.ContentWindow.ContentLocalModeExitWindow");
            var window = (UnityEditor.EditorWindow)ScriptableObject.CreateInstance(type);
            var oldEvent = Event.current;
            const BindingFlags statics = BindingFlags.Static | BindingFlags.NonPublic;
            try
            {
                config.EnableLocalContentInEditor = true;
                window.position = new Rect(0, 0, 560, 340);
                Set(window, "_service", service);
                Set(window, "_configuration", config);
                Set(window, "_scope", "test/realm/global");
                if (error) Set(window, "_error", new string('x', 4000));
                for (var i = 0; i < count; i++)
                {
                    var id = "currency." + new string('w', 250) + i;
                    service.EntriesCache[id] = new LocalContentManifestEntry { FullId = id, ChangeKind = (int)ContentChangeKind.IncomingChange };
                }
                foreach (var eventType in new[] { EventType.Layout, EventType.Repaint })
                {
                    typeof(GUIUtility).GetMethod("BeginContainerFromOwner", statics).Invoke(null, new object[] { window });
                    try
                    {
                        Event.current = new Event { type = eventType };
                        typeof(GUIUtility).GetMethod("BeginGUI", statics).Invoke(null, new object[] { 1, window.GetInstanceID(), 1 });
                        type.GetMethod("OnGUI", Fields).Invoke(window, null);
                        if (eventType == EventType.Layout)
                            typeof(GUILayoutUtility).GetMethod("LayoutFromContainer", statics).Invoke(null, new object[] { 560f, 340f });
                        else Assert.That(GUILayoutUtility.GetLastRect().yMax, Is.LessThanOrEqualTo(340), "Exit and cancellation actions must remain visible.");
                    }
                    finally { typeof(GUIUtility).GetMethod("EndContainer", statics).Invoke(null, null); }
                }
            }
            finally
            {
                Event.current = oldEvent;
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [UnityTest] public IEnumerator NormalToLocalWaitsForBothWatchers() => Switch(false, true);
        [UnityTest] public IEnumerator LocalToNormalWaitsForBothWatchers() => Switch(true, true);
        [UnityTest] public IEnumerator SwitchingKeepsInactiveHistoryStopped() => Switch(false, false);

        [UnityTest] public IEnumerator SlowNamespaceRequestDoesNotBlockNormalContent() => DelayedNamespaces(false, false);
        [UnityTest] public IEnumerator SlowNamespaceRequestDoesNotBlockLocalContent() => DelayedNamespaces(true, false);
        [UnityTest] public IEnumerator NamespaceTransportFailureDoesNotBlockNormalContent() => DelayedNamespaces(false, true);
        [UnityTest] public IEnumerator NamespaceTransportFailureDoesNotBlockLocalContent() => DelayedNamespaces(true, true);

        [UnityTest]
        public IEnumerator Failed_mode_start_can_retry_without_changing_the_preference()
        {
            config.EnableLocalContentInEditor = false;
            yield return Wait(service.Reload());
            config.EnableLocalContentInEditor = true;
            commands.FailNextContentStart = true;
            var switching = ApplyMode().Error(_ => { });
            commands.Content[0].Completion.CompleteSuccess();
            yield return Wait(switching);
            Assert.That(switching.IsFailed, Is.True);
            AssertMode("Failed");
            var retry = ApplyMode();
            yield return Wait(retry);
            retry.GetResult();
            AssertMode("Paused");
            Assert.That(config.EnableLocalContentInEditor, Is.True);
        }

        IEnumerator DelayedNamespaces(bool initialLocal, bool transportFailure)
        {
            commands.DelayManifests = true;
            config.EnableLocalContentInEditor = initialLocal;
            var loading = service.Reload();
            yield return null;
            Assert.That(service.isReloading, Is.False, "The content snapshot is ready even while namespace discovery is pending.");
            Assert.That(loading.IsCompleted, Is.True, "Content readiness must not depend on namespace discovery.");
            loading.GetResult();
            Assert.That(service.RemoteManifestsLoaded, Is.False);
            Assert.That(service.RemoteManifestsErrored, Is.False);

            yield return Wait(service.StartContentHistory());
            config.EnableLocalContentInEditor = !initialLocal;
            var switching = ApplyMode();
            commands.Content[0].Completion.CompleteSuccess();
            commands.History[0].Completion.CompleteSuccess();
            yield return Wait(switching);
            switching.GetResult();
            Assert.That(service.isReloading, Is.False);
            Assert.That(commands.Content[1].Local, Is.EqualTo(!initialLocal));
            Assert.That(commands.History[1].Local, Is.EqualTo(!initialLocal));

            var metadataVersion = MetadataVersion();
            commands.Manifests[0].Emit();
            commands.Manifests[0].ReportError();
            commands.Manifests[0].Completion.CompleteError(new InvalidOperationException("retired namespace request"));
            Assert.That(service.RemoteManifestsLoaded, Is.False, "Retired responses cannot update the new watcher.");
            Assert.That(service.RemoteManifestsErrored, Is.False);
            Assert.That(MetadataVersion(), Is.EqualTo(metadataVersion), "Retired requests must not repaint current metadata.");

            if (transportFailure)
            {
                LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: namespace transport failed"));
                commands.Manifests[1].Completion.CompleteError(new InvalidOperationException("namespace transport failed"));
            }
            else
            {
                LogAssert.Expect(LogType.Error, "namespace request failed");
                commands.Manifests[1].ReportError();
                commands.Manifests[1].Completion.CompleteSuccess();
            }
            yield return null;
            Assert.That(service.RemoteManifestsErrored, Is.True);
            Assert.That(MetadataVersion(), Is.GreaterThan(metadataVersion), "A late error must notify the window.");
            Assert.That(service.RemoteManifestsLoaded, Is.False, "A failed request must not imply an empty realm.");
            Assert.That(service.isReloading, Is.False);
            Assert.That(commands.Content[1].Cancelled, Is.False);
            Assert.That(commands.History[1].Cancelled, Is.False);

            var retry = service.Reload();
            commands.Content[1].Completion.CompleteSuccess();
            yield return Wait(retry);
            retry.GetResult();
            metadataVersion = MetadataVersion();
            commands.Manifests[2].Emit();
            commands.Manifests[2].Completion.CompleteSuccess();
            Assert.That(service.availableManifestIds, Does.Contain("global"));
            Assert.That(service.RemoteManifestsLoaded, Is.True);
            Assert.That(service.RemoteManifestsErrored, Is.False);
            Assert.That(MetadataVersion(), Is.GreaterThan(metadataVersion), "A late namespace result must notify the window.");
        }

        int MetadataVersion()
        {
            var property = typeof(CliContentService).GetProperty("ContentMetadataVersion");
            Assert.That(property, Is.Not.Null, "Metadata completion needs its own repaint signal.");
            return (int)property.GetValue(service);
        }

        void AssertMode(string expected)
        {
            var property = typeof(CliContentService).GetProperty("AutoSyncState");
            Assert.That(property, Is.Not.Null, "The UI must use acknowledged watcher state.");
            Assert.That(property.GetValue(service).ToString(), Is.EqualTo(expected));
        }

        [UnityTest] public IEnumerator ContentFailureCanRetryAfterAcknowledgedShutdown() => RetryFailedWatcher(false);
        [UnityTest] public IEnumerator HistoryFailureCanRetryAfterAcknowledgedShutdown() => RetryFailedWatcher(true);

        [UnityTest] public IEnumerator ContentRetainsOwnershipAfterFailedAcknowledgement() => RetryFailedAcknowledgement(false);
        [UnityTest] public IEnumerator HistoryRetainsOwnershipAfterFailedAcknowledgement() => RetryFailedAcknowledgement(true);
        [UnityTest] public IEnumerator ModeSwitchCanRetryFailedAcknowledgement() => RetryFailedAcknowledgement(false, true);
        [UnityTest] public IEnumerator ExplicitHistoryStopCancelsPendingModeRestart() => RetryFailedAcknowledgement(false, true, true);

        [UnityTest]
        public IEnumerator FailedHistoryCloseRemainsClosedAfterModeSwitch()
        {
            var transport = new BeamWebCommandStopTests.Fixture(transports) { StopStatus = 500, RetryStop = true };
            commands.FirstWebCommand = transport.Command;
            yield return Wait(service.StartContentHistory());
            transport.Discover();
            transport.AllowHeaders.TrySetResult(true);
            var close = service.StopContentHistory();
            close.Error(_ => { });
            transport.ReleaseWorker.TrySetResult(true);
            transport.AllowAcknowledgement.TrySetResult(true);
            yield return Wait(close);
            Assert.That(close.IsFailed, Is.True);

            config.EnableLocalContentInEditor = true;
            var switching = ApplyMode();
            switching.Error(_ => { });
            Assert.That(commands.History.Count, Is.Zero);
            transport.AllowRetryAcknowledgement.TrySetResult(true);
            yield return Wait(switching);
            switching.GetResult();
            Assert.That(transport.RetryReceived.Task.IsCompleted, Is.True, "Closed History still owes cleanup.");
            Assert.That(commands.History.Count, Is.Zero, "Cleanup ownership must not reopen History.");
            Assert.That(service.IsContentHistoryWatching, Is.False);
            yield return Wait(service.StartContentHistory());
            Assert.That(commands.History.Count, Is.EqualTo(1), "An explicit reopen must still work.");
        }

        [UnityTest]
        public IEnumerator HistoryCloseDuringModeSwitchPreventsReplacement()
        {
            yield return Wait(service.Reload());
            yield return Wait(service.StartContentHistory());
            config.EnableLocalContentInEditor = true;
            var switching = ApplyMode();
            var close = service.StopContentHistory();
            commands.Content[0].Completion.CompleteSuccess();
            commands.History[0].Completion.CompleteSuccess();
            yield return Wait(switching);
            Assert.That(commands.History.Count, Is.EqualTo(1), "Close intent must take effect before queued cleanup acquires the gate.");
            yield return Wait(close);
            Assert.That(commands.Content.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ModeSwitchDoesNotResumeTemporarilyDisabledContent()
        {
            yield return Wait(service.Reload());
            var operation = new Promise();
            var started = false;
            var pause = service.TempDisableWatcher(() => { started = true; return operation; });
            config.EnableLocalContentInEditor = true;
            var switching = ApplyMode();
            commands.Content[0].Completion.CompleteSuccess();
            yield return Wait(switching);
            Assert.That(started, Is.True);
            Assert.That(commands.Content.Count, Is.EqualTo(1), "A temporary operation owns the pause until it finishes.");
            operation.CompleteSuccess();
            yield return Wait(pause);
            yield return null;
            Assert.That(commands.Content.Count, Is.EqualTo(2));
            Assert.That(commands.Content[1].Local, Is.True);
        }

        IEnumerator RetryFailedAcknowledgement(bool history, bool switching = false, bool stopHistory = false)
        {
            var transport = new BeamWebCommandStopTests.Fixture(transports) { StopStatus = 500, RetryStop = true };
            commands.FirstWebCommand = transport.Command;
            var initial = history ? service.StartContentHistory() : service.Reload();
            initial.Error(_ => { });
            transport.Discover();
            transport.AllowHeaders.TrySetResult(true);
            if (switching)
            {
                yield return Wait(service.StartContentHistory());
                config.EnableLocalContentInEditor = true;
            }
            var first = switching ? ApplyMode() : history ? service.RestartContentHistory() : service.Reload();
            first.Error(_ => { });
            if (switching) commands.History[0].Completion.CompleteSuccess();
            transport.ReleaseWorker.TrySetResult(true);
            transport.AllowAcknowledgement.TrySetResult(true);
            yield return Wait(first);
            Assert.That(first.IsFailed, Is.True);
            AssertMode("Failed");
            Assert.That(commands.Content.Count + commands.History.Count, Is.EqualTo(switching ? 1 : 0));
            if (stopHistory) yield return Wait(service.StopContentHistory());

            var retry = switching ? ApplyMode() : history ? service.RestartContentHistory() : service.Reload();
            retry.Error(_ => { });
            AssertMode("Switching");
            Assert.That(retry.IsFailed, Is.False, "Retry must contact the server again, not reuse a failed shutdown.");
            Assert.That(commands.Content.Count + commands.History.Count, Is.EqualTo(switching ? 1 : 0), "The retiring command still owns shutdown.");
            transport.AllowRetryAcknowledgement.TrySetResult(true);
            yield return Wait(retry);
            Assert.That(transport.RetryReceived.Task.IsCompleted, Is.True);
            Assert.That(transport.StopId, Is.EqualTo(transport.Command.id));
            Assert.That(retry.IsFailed, Is.False);
            AssertMode(config.EnableLocalContentInEditor ? "Paused" : "Enabled");
            Assert.That(commands.Content.Count + commands.History.Count, Is.EqualTo(switching ? (stopHistory ? 2 : 3) : 1));
            if (switching)
            {
                Assert.That(commands.Content[0].Local, Is.True);
                if (!stopHistory) Assert.That(commands.History[1].Local, Is.True);
            }
        }

        IEnumerator RetryFailedWatcher(bool history)
        {
            var transport = new BeamWebCommandStopTests.Fixture(transports);
            var failure = new IOException("Controlled command completion failure");
            // Fail the real command's terminal callback, after its HTTP stream and dispatcher drain.
            transport.Command.OnTerminate(_ => throw failure);
            commands.FirstWebCommand = transport.Command;
            var initial = history ? service.StartContentHistory() : service.Reload();
            initial.Error(_ => { });
            var run = (Promise)transport.Command.GetType().GetField("_runPromise", Fields).GetValue(transport.Command);
            Exception observed = null;
            run.Error(error => observed = error);
            transport.Discover();
            transport.AllowHeaders.TrySetResult(true);
            var stop = transport.Command.StopAsync();
            stop.Error(_ => { });
            if (history) LogAssert.Expect(LogType.Error, new Regex("Content History operation failed"));
            transport.ReleaseWorker.TrySetResult(true);
            yield return Wait(run);
            if (history)
            {
                // The service observes the completed command asynchronously. Retry must follow that observation.
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (service.ContentHistoryWatcherError == null && DateTime.UtcNow < deadline)
                {
                    UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                    yield return null;
                }
                Assert.That(service.ContentHistoryWatcherError, Is.Not.Null);
            }
            Assert.That(observed, Is.SameAs(failure), "Shutdown must not erase the original command failure.");
            Assert.That(stop.IsCompleted, Is.False, "A failed stream does not prove server shutdown.");

            var retry = history ? service.RestartContentHistory() : service.Reload();
            retry.Error(_ => { });
            Assert.That(commands.Content.Count + commands.History.Count, Is.Zero, "Retry must await acknowledgement.");
            transport.AllowAcknowledgement.TrySetResult(true);
            yield return Wait(retry);
            Assert.That(retry.IsFailed, Is.False, "Confirmed shutdown must allow retry despite the earlier command failure.");
            Assert.That(commands.Content.Count + commands.History.Count, Is.EqualTo(1));
            Assert.That(run.IsFailed, Is.True);
            Assert.That(stop.IsFailed, Is.False);
            if (history) Assert.That(service.IsContentHistoryWatching, Is.True);
        }

        [UnityTest]
        public IEnumerator RapidChangesUseLatestModeWithoutRestartingTwice()
        {
            yield return Wait(service.Reload());
            service.StartContentHistory();
            config.EnableLocalContentInEditor = true;
            var first = ApplyMode();
            config.EnableLocalContentInEditor = false;
            var second = ApplyMode();
            commands.Content[0].Completion.CompleteSuccess();
            commands.History[0].Completion.CompleteSuccess();
            yield return Wait(first);
            yield return null;
            Assert.That(commands.Content[1].Local, Is.False);
            Assert.That(commands.History[1].Local, Is.False);
            Assert.That(commands.Content[1].Cancelled, Is.False, "A queued toggle must reuse the already-correct replacement.");
            Assert.That(commands.History[1].Cancelled, Is.False);
            yield return Wait(second);
        }

        [UnityTest]
        public IEnumerator FailedShutdownDoesNotStartReplacements()
        {
            yield return Wait(service.Reload());
            service.StartContentHistory();
            config.EnableLocalContentInEditor = true;
            Exception observed = null;
            var switching = ApplyMode().Error(error => observed = error);
            commands.Content[0].Completion.CompleteError(new InvalidOperationException("stop failed"));
            commands.History[0].Completion.CompleteSuccess();
            yield return Wait(switching);
            Assert.That(observed, Is.Not.Null);
            Assert.That(commands.Content.Count, Is.EqualTo(1));
            Assert.That(commands.History.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SwitchingCanStopAWatcherStillWaitingForInitialContent()
        {
            commands.DelayInitialContent = true;
            var loading = service.Reload();
            config.EnableLocalContentInEditor = true;
            var switching = ApplyMode();
            Assert.That(commands.Content[0].Cancelled, Is.True, "Initial network fetch must not hold the transition lock.");
            commands.DelayInitialContent = false;
            commands.Content[0].Completion.CompleteSuccess();
            yield return Wait(switching);
            yield return Wait(loading);
            switching.GetResult();
            loading.GetResult();
            Assert.That(commands.Content[1].Local, Is.True);
        }

        [UnityTest]
        public IEnumerator DisposalDuringSwitchDoesNotRestartWatchers()
        {
            yield return Wait(service.Reload());
            service.StartContentHistory();
            config.EnableLocalContentInEditor = true;
            var switching = ApplyMode();
            var dispose = (Promise)typeof(CliContentService).GetMethod("OnDispose").Invoke(service, null);
            commands.Content[0].Completion.CompleteSuccess();
            commands.History[0].Completion.CompleteSuccess();
            yield return Wait(switching);
            yield return Wait(dispose);
            Assert.That(commands.Content.Count, Is.EqualTo(1));
            Assert.That(commands.History.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DisposalDuringReloadDoesNotStartAnotherCommand()
        {
            yield return Wait(service.Reload());
            var reload = service.Reload();
            var dispose = (Promise)typeof(CliContentService).GetMethod("OnDispose").Invoke(service, null);
            commands.Content[0].Completion.CompleteSuccess();
            yield return null;
            Assert.That(commands.Content.Count, Is.EqualTo(1));
            yield return Wait(reload);
            yield return Wait(dispose);
        }

        [UnityTest]
        public IEnumerator DependencyDisposalKeepsDispatcherAliveUntilWatchersFinish()
        {
            yield return Wait(service.Reload());
            var dispatcher = new BeamableDispatcher();
            var scope = new DependencyBuilder().AddSingleton(service).AddSingleton(dispatcher).Build();
            scope.GetService<CliContentService>();
            scope.GetService<BeamableDispatcher>();
            var command = commands.Content[0];
            command.OnCancel = () => dispatcher.Schedule(() => command.Completion.CompleteSuccess());
            try
            {
                var disposal = scope.Dispose();
                Assert.That(dispatcher.IsForceStopped, Is.False, "Watcher cleanup still needs the dispatcher.");
                yield return Wait(disposal);
                disposal.GetResult();
                Assert.That(dispatcher.IsForceStopped, Is.True);
            }
            finally { dispatcher.OnDispose(); command.Completion.CompleteSuccess(); }
        }

        [UnityTest]
        public IEnumerator AssemblyReloadDisposalDoesNotWaitForGateOrEditorCallbacks()
        {
            yield return Wait(service.Reload());
            yield return Wait(service.StartContentHistory());
            config.EnableLocalContentInEditor = true;
            var switching = ApplyMode();
            // Both commands now belong to pending shutdown, with the gate still held.
            var prepare = typeof(CliContentService).GetMethod("PrepareForAssemblyReload",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(prepare, Is.Not.Null, "Reload needs teardown that can finish without editor updates.");
            prepare.Invoke(service, null);
            var disposal = service.OnDispose();
            Assert.That(disposal.IsCompleted, Is.True, "Assembly reload blocks the editor update loop.");
            disposal.GetResult();
            Assert.That(service.IsContentHistoryWatching, Is.False);
            commands.Content[0].Completion.CompleteSuccess();
            commands.History[0].Completion.CompleteSuccess();
            yield return Wait(switching);
            Assert.That(commands.Content.Count, Is.EqualTo(1));
            Assert.That(commands.History.Count, Is.EqualTo(1));
        }

        IEnumerator Switch(bool initial, bool history)
        {
            config.EnableLocalContentInEditor = initial;
            yield return Wait(service.Reload());
            if (history) service.StartContentHistory();
            AssertMode(initial ? "Paused" : "Enabled");
            var oldContent = commands.Content[0];
            var oldHistory = history ? commands.History[0] : null;
            config.EnableLocalContentInEditor = !initial;
            AssertMode("Switching");
            var switching = ApplyMode();
            AssertMode("Switching");
            Assert.That(oldContent.Cancelled, Is.True);
            if (history) Assert.That(oldHistory.Cancelled, Is.True);
            Assert.That(commands.Content.Count, Is.EqualTo(1), "No replacement before shutdown acknowledgement.");
            Assert.That(commands.History.Count, Is.EqualTo(history ? 1 : 0));
            oldContent.Completion.CompleteSuccess();
            yield return null;
            if (history)
            {
                AssertMode("Switching");
                Assert.That(commands.Content.Count, Is.EqualTo(1), "Content must also wait for history shutdown.");
                oldHistory.Completion.CompleteSuccess();
            }
            yield return Wait(switching);
            switching.GetResult();
            AssertMode(initial ? "Enabled" : "Paused");
            Assert.That(commands.Content.Count, Is.EqualTo(2));
            Assert.That(commands.Content[1].Local, Is.EqualTo(!initial));
            Assert.That(commands.History.Count, Is.EqualTo(history ? 2 : 0));
            if (history) Assert.That(commands.History[1].Local, Is.EqualTo(!initial));
            var version = service.ManifestChangedCount;
            oldContent.Emit();
            Assert.That(service.ManifestChangedCount, Is.EqualTo(version), "Late old callbacks must be ignored.");
            if (history)
            {
                var historyVersion = service.ContentHistoryVersion;
                oldHistory.Emit();
                Assert.That(service.ContentHistoryVersion, Is.EqualTo(historyVersion), "Late history callbacks must be ignored too.");
            }
        }

        static IEnumerator Wait(PromiseBase promise)
        {
            var deadline = UnityEditor.EditorApplication.timeSinceStartup + 10;
            while (!promise.IsCompleted && UnityEditor.EditorApplication.timeSinceStartup < deadline)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.That(promise.IsCompleted, Is.True, "Watcher transition did not complete within 10 seconds.");
        }

        Promise ApplyMode()
        {
            var method = typeof(CliContentService).GetMethod("ApplyLocalContentMode", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "The service must coordinate active watcher mode changes.");
            return (Promise)method.Invoke(service, null);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            // Detach ownership first so controlled completion is not reported as an unexpected failure.
            Set(service, "_contentHistoryWatcher", null);
            Set(service, "_contentWatcher", null);
            foreach (var command in commands.Content.ToArray()) command.Completion.CompleteSuccess();
            foreach (var command in commands.History.ToArray()) command.Completion.CompleteSuccess();
            foreach (var command in commands.Manifests.ToArray()) command.Completion.CompleteSuccess();
            foreach (var command in commands.Checks.ToArray()) command.Completion.CompleteSuccess();
            var cleanup = BeamWebCommandStopTests.CleanupFixtures(transports);
            try
            {
                // Drive directly so a cleanup assertion cannot skip this iterator's finally block.
                while (cleanup.MoveNext()) yield return cleanup.Current;
            }
            finally
            {
                (cleanup as IDisposable)?.Dispose();
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
        static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);

        class Commands : BeamCommands
        {
            public readonly List<HeldCommand> Content = new();
            public readonly List<HeldCommand> History = new();
            public readonly List<HeldCommand> Manifests = new();
            public readonly List<HeldCommand> Checks = new();
            public LocalContentManifestEntry[] ExitEntries = Array.Empty<LocalContentManifestEntry>();
            public string ExitManifestId = "global";
            public bool DelayExitCheck;
            public bool CompleteExitCheckOnCancel = true;
            public BeamCli.BeamWebCommand ExitCheckWebCommand;
            public bool DelayInitialContent;
            public bool DelayManifests;
            public bool FailNextContentStart;
            public BeamCli.BeamWebCommand FirstWebCommand;
            public Commands() : base(null, null) { }
            public override ContentPsWrapper ContentPs(ContentPsArgs args)
            {
                if (!args.watch)
                {
                    if (ExitCheckWebCommand != null)
                    {
                        var web = ExitCheckWebCommand;
                        ExitCheckWebCommand = null;
                        return new ContentPsWrapper { Command = web };
                    }
                    // Mirrors the CLI producer CreateFullRebuildEvent: one scoped manifest, even when empty.
                    var check = new HeldCommand(args.noAutoSync, new BeamContentPsCommandEvent
                    {
                        EventType = 0,
                        RelevantManifestsAgainstLatest = new List<LocalContentManifest>
                        {
                            new LocalContentManifest { OwnerCid = "test", OwnerPid = "realm", ManifestId = ExitManifestId, Entries = ExitEntries }
                        },
                        ToRemoveLocalEntries = new List<LocalContentManifest>()
                    }, true) { DelayInitial = DelayExitCheck };
                    if (CompleteExitCheckOnCancel) check.OnCancel = () => check.Completion.CompleteSuccess();
                    Checks.Add(check);
                    return new ContentPsWrapper { Command = check };
                }
                if (FailNextContentStart)
                {
                    FailNextContentStart = false;
                    throw new IOException("Controlled watcher startup failure");
                }
                if (FirstWebCommand != null)
                {
                    var web = FirstWebCommand;
                    FirstWebCommand = null;
                    return new ContentPsWrapper { Command = web };
                }
                var command = new HeldCommand(args.noAutoSync, new BeamContentPsCommandEvent
                {
                    EventType = 0, RelevantManifestsAgainstLatest = new List<LocalContentManifest>(),
                    ToRemoveLocalEntries = new List<LocalContentManifest>()
                });
                command.DelayInitial = DelayInitialContent;
                Content.Add(command);
                return new ContentPsWrapper { Command = command };
            }
            public override ContentHistoryWrapper ContentHistory(ContentHistoryArgs args)
            {
                if (FirstWebCommand != null)
                {
                    var web = FirstWebCommand;
                    FirstWebCommand = null;
                    return new ContentHistoryWrapper { Command = web };
                }
                var command = new HeldCommand(args.noAutoSync, null);
                History.Add(command);
                return new ContentHistoryWrapper { Command = command };
            }
            public override ContentListManifestsWrapper ContentListManifests(ContentListManifestsArgs args)
            {
                var command = new HeldCommand(false, new BeamContentListManifestsCommandResults
                { localManifests = new List<string> { "global" }, remoteManifests = new List<string> { "global" } }, true)
                { DelayInitial = DelayManifests };
                Manifests.Add(command);
                return new ContentListManifestsWrapper { Command = command };
            }
        }
        class HeldCommand : IBeamCommand
        {
            public readonly Promise Completion = new();
            public readonly bool Local;
            public bool Cancelled;
            public bool DelayInitial;
            public Action OnCancel;
            readonly object data;
            readonly bool finite;
            Action emit = () => { };
            Action<ReportDataPoint<ErrorOutput>> error;
            public HeldCommand(bool local, object data, bool finite = false) { Local = local; this.data = data; this.finite = finite; }
            public void SetCommand(string command) { }
            public Promise Run() { if (!DelayInitial) { Emit(); if (finite) Completion.CompleteSuccess(); } return Completion; }
            public void Emit() => emit();
            public void ReportError() => error?.Invoke(new ReportDataPoint<ErrorOutput> { data = new ErrorOutput { message = "namespace request failed" } });
            public void Cancel() { Cancelled = true; OnCancel?.Invoke(); }
            public IBeamCommand On<T>(string type, Action<ReportDataPoint<T>> callback)
            {
                if (data is T value) emit += () => callback(new ReportDataPoint<T> { data = value });
                return this;
            }
            public IBeamCommand On(Action<ReportDataPointDescription> callback)
            {
                emit += () => callback(new ReportDataPointDescription { json = "{\"ts\":1,\"type\":\"stream\",\"data\":{\"EventType\":0,\"EntriesPage\":{\"Entries\":[]},\"EntriesToRemove\":[]}}" });
                return this;
            }
            public IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> callback) { error += callback; return this; }
            public IBeamCommand OnTerminate(Action<ReportDataPoint<EofOutput>> callback) => this;
        }
    }
}
