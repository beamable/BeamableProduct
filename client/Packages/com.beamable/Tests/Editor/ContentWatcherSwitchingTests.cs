using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Beamable.Common;
using Beamable.Common.Api.Realms;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content.Validation;
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
            Set(service, "_contentHistoryChangesRequest", new ContentHistoryRequestSlot<ContentHistorySyncChangelistWrapper>(request => request.Cancel()));
            Set(service, "_contentHistoryPreviewRequest", new ContentHistoryRequestSlot<ContentHistorySyncContentWrapper>(request => request.Cancel()));
            var cache = typeof(CliContentService).GetField("_contentHistoryEntryCache", Fields);
            cache.SetValue(service, Activator.CreateInstance(cache.FieldType));
            service.manifestIdOverride = "global";
        }

        [UnityTest] public IEnumerator NormalToLocalWaitsForBothWatchers() => Switch(false, true);
        [UnityTest] public IEnumerator LocalToNormalWaitsForBothWatchers() => Switch(true, true);
        [UnityTest] public IEnumerator SwitchingKeepsInactiveHistoryStopped() => Switch(false, false);

        [UnityTest] public IEnumerator ContentFailureCanRetryAfterAcknowledgedShutdown() => RetryFailedWatcher(false);
        [UnityTest] public IEnumerator HistoryFailureCanRetryAfterAcknowledgedShutdown() => RetryFailedWatcher(true);

        [UnityTest] public IEnumerator ContentRetainsOwnershipAfterFailedAcknowledgement() => RetryFailedAcknowledgement(false);
        [UnityTest] public IEnumerator HistoryRetainsOwnershipAfterFailedAcknowledgement() => RetryFailedAcknowledgement(true);
        [UnityTest] public IEnumerator ModeSwitchCanRetryFailedAcknowledgement() => RetryFailedAcknowledgement(false, true);
        [UnityTest] public IEnumerator ExplicitHistoryStopCancelsPendingModeRestart() => RetryFailedAcknowledgement(false, true, true);

        [UnityTest]
        public IEnumerator FailedHistoryCloseRemainsClosedAfterModeSwitch()
        {
            using var transport = new BeamWebCommandStopTests.Fixture { StopStatus = 500, RetryStop = true };
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
            using var transport = new BeamWebCommandStopTests.Fixture { StopStatus = 500, RetryStop = true };
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
            Assert.That(commands.Content.Count + commands.History.Count, Is.EqualTo(switching ? 1 : 0));
            if (stopHistory) yield return Wait(service.StopContentHistory());

            var retry = switching ? ApplyMode() : history ? service.RestartContentHistory() : service.Reload();
            retry.Error(_ => { });
            Assert.That(retry.IsFailed, Is.False, "Retry must contact the server again, not reuse a failed shutdown.");
            Assert.That(commands.Content.Count + commands.History.Count, Is.EqualTo(switching ? 1 : 0), "The retiring command still owns shutdown.");
            transport.AllowRetryAcknowledgement.TrySetResult(true);
            yield return Wait(retry);
            Assert.That(transport.RetryReceived.Task.IsCompleted, Is.True);
            Assert.That(transport.StopId, Is.EqualTo(transport.Command.id));
            Assert.That(retry.IsFailed, Is.False);
            Assert.That(commands.Content.Count + commands.History.Count, Is.EqualTo(switching ? (stopHistory ? 2 : 3) : 1));
            if (switching)
            {
                Assert.That(commands.Content[0].Local, Is.True);
                if (!stopHistory) Assert.That(commands.History[1].Local, Is.True);
            }
        }

        IEnumerator RetryFailedWatcher(bool history)
        {
            using var transport = new BeamWebCommandStopTests.Fixture();
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

        IEnumerator Switch(bool initial, bool history)
        {
            config.EnableLocalContentInEditor = initial;
            yield return Wait(service.Reload());
            if (history) service.StartContentHistory();
            var oldContent = commands.Content[0];
            var oldHistory = history ? commands.History[0] : null;
            config.EnableLocalContentInEditor = !initial;
            var switching = ApplyMode();
            Assert.That(oldContent.Cancelled, Is.True);
            if (history) Assert.That(oldHistory.Cancelled, Is.True);
            Assert.That(commands.Content.Count, Is.EqualTo(1), "No replacement before shutdown acknowledgement.");
            Assert.That(commands.History.Count, Is.EqualTo(history ? 1 : 0));
            oldContent.Completion.CompleteSuccess();
            yield return null;
            if (history)
            {
                Assert.That(commands.Content.Count, Is.EqualTo(1), "Content must also wait for history shutdown.");
                oldHistory.Completion.CompleteSuccess();
            }
            yield return Wait(switching);
            switching.GetResult();
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
            while (!promise.IsCompleted && UnityEditor.EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.That(promise.IsCompleted, Is.True, "Watcher transition did not complete within 10 seconds.");
        }

        Promise ApplyMode()
        {
            var method = typeof(CliContentService).GetMethod("ApplyLocalContentMode", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "The service must coordinate active watcher mode changes.");
            return (Promise)method.Invoke(service, null);
        }

        [TearDown]
        public void Cleanup()
        {
            // Detach ownership first so controlled completion is not reported as an unexpected failure.
            Set(service, "_contentHistoryWatcher", null);
            Set(service, "_contentWatcher", null);
            foreach (var command in commands.Content.ToArray()) command.Completion.CompleteSuccess();
            foreach (var command in commands.History.ToArray()) command.Completion.CompleteSuccess();
            UnityEngine.Object.DestroyImmediate(config);
        }
        static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);

        class Commands : BeamCommands
        {
            public readonly List<HeldCommand> Content = new();
            public readonly List<HeldCommand> History = new();
            public bool DelayInitialContent;
            public BeamCli.BeamWebCommand FirstWebCommand;
            public Commands() : base(null, null) { }
            public override ContentPsWrapper ContentPs(ContentPsArgs args)
            {
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
            public override ContentListManifestsWrapper ContentListManifests(ContentListManifestsArgs args) =>
                new ContentListManifestsWrapper { Command = new HeldCommand(false, new BeamContentListManifestsCommandResults
                { localManifests = new List<string>(), remoteManifests = new List<string>() }, true) };
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
            public HeldCommand(bool local, object data, bool finite = false) { Local = local; this.data = data; this.finite = finite; }
            public void SetCommand(string command) { }
            public Promise Run() { if (!DelayInitial) Emit(); if (finite) Completion.CompleteSuccess(); return Completion; }
            public void Emit() => emit();
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
            public IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> callback) => this;
            public IBeamCommand OnTerminate(Action<ReportDataPoint<EofOutput>> callback) => this;
        }
    }
}
