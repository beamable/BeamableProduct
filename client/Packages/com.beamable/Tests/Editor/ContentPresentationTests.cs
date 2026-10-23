using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Realms;
using Beamable.Common.Dependencies;
using Beamable.Common.Content;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Content;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.ContentService;
using Beamable.Editor.UI.ContentWindow;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Tests
{
    public class ContentReconciliationFeedbackTests
    {
        [TestCase(false, "success")]
        [TestCase(true, "success")]
        [TestCase(false, "reported failure")]
        [TestCase(true, "reported failure")]
        [TestCase(false, "transport failure")]
        [TestCase(true, "transport failure")]
        public void Reconciliation_retains_write_outcomes_during_delayed_watcher_shutdown(bool tags, string outcome)
        {
            // Controlled completions run inline, independent of the Editor's synchronization context.
            var previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                var commands = new ResolveCommands();
                var service = CreateService(commands);
                var shutdown = new TaskCompletionSource<bool>();
                const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(CliContentService).GetField("_contentWatcher", fields)
                    .SetValue(service, new ContentPsWrapper { Command = new ResolveCommand() });
                typeof(CliContentService).GetField("_contentWatcherCompletion", fields).SetValue(service, shutdown.Task);
                var method = typeof(CliContentService).GetMethod(tags ? "SetContentTagsAsync" : "SaveContentPropertiesAsync", fields);
                var args = tags ? new object[] { "currency.test", new[] { "new" } } : new object[] { "currency.test", "{}", null };
                var write = (Promise)method.Invoke(service, args);
                write.Error(_ => { });
                var operation = service.ReconcileContentWithProgress("currency.test", false);
                Exception observed = null;
                operation.Error(error => observed = error);

                if (outcome == "transport failure") commands.Pending.Completion.CompleteError(new InvalidOperationException("Edit failed."));
                else
                {
                    if (outcome == "reported failure") commands.Pending.ReportError("Edit failed.");
                    commands.Pending.Completion.CompleteSuccess();
                }
                Assert.That(write.IsCompleted, Is.True);
                Assert.That(operation.IsCompleted, Is.False, "Reconciliation must still await shutdown.");
                Assert.That(service.CurrentProgress.IsActive, Is.True);
                Assert.That(commands.Calls, Is.Zero);
                commands.Pending = new ResolveCommand();
                shutdown.SetResult(true);

                if (outcome == "success")
                {
                    Assert.That(commands.Calls, Is.EqualTo(1));
                    commands.Pending.Completion.CompleteSuccess();
                    Assert.That(operation.IsCompleted && !operation.IsFailed, Is.True);
                }
                else
                {
                    Assert.That(commands.Calls, Is.Zero, "A failed edit must not be forgotten during shutdown.");
                    Assert.That(observed, Is.Not.Null);
                    Assert.That(observed.Message, Does.Contain("Edit failed."));
                    var retry = service.ReconcileContentWithProgress("currency.test", false);
                    commands.Pending.Completion.CompleteSuccess();
                    Assert.That(retry.IsCompleted && !retry.IsFailed, Is.True);
                }
                Assert.That(service.CurrentProgress.IsActive, Is.False);
            }
            finally { SynchronizationContext.SetSynchronizationContext(previousContext); }
        }

        [Test]
        public void Deselecting_an_inspector_keeps_both_pending_header_guards_without_flushing_writes()
        {
            var service = CreateService(new ResolveCommands());
            var type = typeof(global::Beamable.Editor.Content.UI.ContentObjectEditor);
            var editor = FormatterServices.GetUninitializedObject(type);
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var completed = new List<string>();
            type.GetField("_editingService", fields).SetValue(editor, service);
            type.GetField("_editingContentId", fields).SetValue(editor, "currency.test");
            type.GetField("_pendingNameEdit", fields).SetValue(editor, (Action)(() => completed.Add("name")));
            type.GetField("_pendingTagEdit", fields).SetValue(editor, (Action)(() => completed.Add("tags")));
            var observer = (Func<string, bool>)Delegate.CreateDelegate(typeof(Func<string, bool>), editor,
                type.GetMethod("HasPendingEdits", fields));
            service.HasPendingInspectorEdits += observer;
            type.GetMethod("OnDisable", fields).Invoke(editor, null);
            Assert.That(completed, Is.Empty);
            Assert.That(observer("currency.test"), Is.True);
            type.GetMethod("CompleteHeaderEdit", fields).Invoke(editor, new object[] { false });
            Assert.That(completed, Is.EqualTo(new[] { "tags" }));
            Assert.That(observer("currency.test"), Is.True);
            type.GetMethod("CompleteHeaderEdit", fields).Invoke(editor, new object[] { true });
            Assert.That(completed, Is.EqualTo(new[] { "tags", "name" }));
            Assert.That(observer("currency.test"), Is.False);
            Assert.That(typeof(CliContentService).GetField("HasPendingInspectorEdits", fields).GetValue(service), Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Reported_write_rejection_aborts_waiting_reconciliation(bool tags)
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            var method = typeof(CliContentService).GetMethod(tags ? "SetContentTagsAsync" : "SaveContentPropertiesAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var args = tags ? new object[] { "currency.test", new[] { "new" } } : new object[] { "currency.test", "{}", null };
            var write = (Promise)method.Invoke(service, args);
            write.Error(_ => { });
            var operation = service.ReconcileContentWithProgress("currency.test", false);
            Exception observed = null;
            operation.Error(error => observed = error);
            commands.Pending.ReportError("The file could not be saved.");
            commands.Pending.Completion.CompleteSuccess();
            Assert.That(write.IsFailed, Is.True);
            Assert.That(observed?.Message, Does.Contain("could not be saved"));
            Assert.That(commands.Calls, Is.Zero);
            Assert.That(service.CurrentProgress.IsActive, Is.False);
        }

        [TestCase("realm")]
        [TestCase("publication")]
        [TestCase("missing item")]
        public void Inspector_resolution_rejects_a_menu_choice_from_an_old_review(string change)
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            var scope = service.GetContentScopeKey();
            var uid = service.EntriesCache["currency.test"].TargetManifestUid;
            // Capture exactly the data held by the Inspector menu, before the callback runs.
            Func<Promise> chooseRealm = () => service.ReconcileContentWithProgress("currency.test", false, scope, uid);
            if (change == "realm") service.manifestIdOverride = "another";
            else if (change == "missing item") service.EntriesCache.Remove("currency.test");
            else
            {
                var updated = service.EntriesCache["currency.test"];
                updated.TargetManifestUid = "new-publication";
                service.EntriesCache["currency.test"] = updated;
            }
            var result = chooseRealm();
            result.Error(_ => { });
            Assert.That(result.IsFailed, Is.True);
            Assert.That(commands.Calls, Is.Zero);
            Assert.That(service.CurrentProgress.IsActive, Is.False);
        }

        [Test]
        public void Pending_inspector_edits_are_preserved_and_require_retry_after_saving()
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            bool pending = true;
            service.HasPendingInspectorEdits += id => pending && id == "currency.test";
            Exception observed = null;
            service.ReconcileContentWithProgress("currency.test", false).Error(error => observed = error);
            Assert.That(observed?.Message, Does.Contain("still being saved"));
            Assert.That(pending, Is.True, "Reconciliation must not discard the edit.");
            Assert.That(commands.Calls, Is.Zero);
            Assert.That(service.CurrentProgress.IsActive, Is.False);
            pending = false;
            var retry = service.ReconcileContentWithProgress("currency.test", false);
            commands.Pending.Completion.CompleteSuccess();
            Assert.That(retry.IsCompleted && !retry.IsFailed, Is.True);
        }

        [TestCase("scope")]
        [TestCase("publication")]
        public void Review_is_checked_again_after_submitted_writes_finish(string change)
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            var writes = (ContentWriteTracker)typeof(CliContentService)
                .GetField("_contentWrites", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
            var pending = new Promise();
            writes.Run("test/realm/global/currency.test", () => pending);
            var result = service.ReconcileContentWithProgress("currency.test", false);
            result.Error(_ => { });
            if (change == "scope") service.manifestIdOverride = "another";
            else
            {
                var entry = service.EntriesCache["currency.test"];
                entry.TargetManifestUid = "new-publication";
                service.EntriesCache["currency.test"] = entry;
            }
            pending.CompleteSuccess();
            Assert.That(result.IsFailed, Is.True);
            Assert.That(commands.Calls, Is.Zero);
            Assert.That(service.CurrentProgress.IsActive, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Publish_rejection_is_reported_and_retry_releases_progress(bool transportFailure)
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            var result = service.PublishContentsWithProgress(false);
            Exception observed = null;
            result.Error(error => observed = error);
            const string message = "Publication changed; reconcile before publishing.";
            if (transportFailure) commands.Pending.Completion.CompleteError(new InvalidOperationException(message));
            else { commands.Pending.ReportError(message); commands.Pending.Completion.CompleteSuccess(); }
            Assert.That(observed?.Message, Does.Contain(message));
            Assert.That(result.IsFailed, Is.True);
            Assert.That(service.CurrentProgress.IsActive, Is.False);
            commands.Pending = new ResolveCommand();
            var retry = service.PublishContentsWithProgress(false);
            commands.Pending.Completion.CompleteSuccess();
            Assert.That(retry.IsCompleted && !retry.IsFailed, Is.True);
            Assert.That(service.CurrentProgress.IsActive, Is.False);
        }

        [Test]
        public void Reconciliation_waits_for_submitted_content_edits_before_running_the_command()
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            var writes = (ContentWriteTracker)typeof(CliContentService)
                .GetField("_contentWrites", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
            var pendingEdit = new Promise();
            var write = writes.Run("test/realm/global/currency.test", () => pendingEdit);

            var operation = service.ReconcileContentWithProgress("currency.test", true);

            Assert.That(commands.Calls, Is.Zero, "Reconciliation must not read content before its submitted edits finish.");
            Assert.That(service.CurrentProgress.IsActive, Is.True);
            pendingEdit.CompleteSuccess();
            Assert.That(write.IsCompleted, Is.True);
            Assert.That(commands.Calls, Is.EqualTo(1));
            commands.Pending.Completion.CompleteSuccess();
            Assert.That(operation.IsCompleted && !operation.IsFailed, Is.True);
        }

        [Test]
        public void Failed_submitted_edit_aborts_reconciliation_and_allows_retry()
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            var writes = (ContentWriteTracker)typeof(CliContentService)
                .GetField("_contentWrites", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
            var pending = new Promise();
            writes.Run("test/realm/global/currency.test", () => pending).Error(_ => { });
            var second = new Promise();
            writes.Run("test/realm/global/currency.test", () => second);
            var operation = service.ReconcileContentWithProgress("currency.test", false);
            Exception observed = null;
            operation.Error(error => observed = error);
            pending.CompleteError(new InvalidOperationException("The edit could not be saved."));
            Assert.That(operation.IsCompleted, Is.False, "Failure must not release busy state while another write is running.");
            Assert.That(service.CurrentProgress.IsActive, Is.True);
            second.CompleteSuccess();
            Assert.That(observed?.Message, Does.Contain("could not be saved"));
            Assert.That(commands.Calls, Is.Zero);
            Assert.That(service.CurrentProgress.IsActive, Is.False);
            var retry = service.ReconcileContentWithProgress("currency.test", false);
            commands.Pending.Completion.CompleteSuccess();
            Assert.That(retry.IsCompleted && !retry.IsFailed, Is.True);
        }

        [Test]
        public void Busy_window_cannot_begin_a_rename()
        {
            var service = CreateService(new ResolveCommands());
            var window = CreateWindow(service);
            service.ReconcileContentWithProgress("currency.test", false);

            InvokeWindow(window, "StartEditEntryName", service.EntriesCache["currency.test"]);

            Assert.That(typeof(ContentWindow).GetField("_editItemId", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(window), Is.Null, "Busy input must not begin editing the selected content.");
            Assert.That(service.CurrentProgress.IsActive, Is.True);
        }

        [TestCase("RenameEntry")]
        [TestCase("DuplicateContent")]
        [TestCase("ShowItemOptionsMenu")]
        [TestCase("ShowTypeMenu")]
        [TestCase("CreateNewItem")]
        [TestCase("DeleteContentEntries")]
        public void Busy_window_blocks_direct_and_delayed_mutation_entry_points(string method)
        {
            var service = CreateService(new ResolveCommands());
            var window = CreateWindow(service);
            service.ReconcileContentWithProgress("currency.test", false);
            var entry = service.EntriesCache["currency.test"];
            object[] args = method switch
            {
                "RenameEntry" => new object[] { entry.FullId, "renamed" },
                "ShowItemOptionsMenu" => new object[] { Vector2.zero },
                "ShowTypeMenu" or "CreateNewItem" => new object[] { "currency" },
                "DeleteContentEntries" => new object[] { new[] { entry } },
                _ => new object[] { entry }
            };

            var result = InvokeWindow(window, method, args);

            if (result is Promise deletion)
                Assert.That(deletion.IsCompleted && !deletion.IsFailed, Is.True);
            Assert.That(service.CurrentProgress.IsActive, Is.True);
            Assert.That(typeof(ContentWindow).GetField("_editItemId", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(window), Is.Null);
        }

        [TestCase("success")]
        [TestCase("reported failure")]
        [TestCase("transport failure")]
        public void Menu_callback_checks_busy_at_invocation_and_resumes_only_on_new_input(string outcome)
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            var window = CreateWindow(service);
            int calls = 0;
            var callback = (GenericMenu.MenuFunction)InvokeWindow(window, "GuardContentInput",
                new GenericMenu.MenuFunction(() => calls++));
            callback();
            Assert.That(calls, Is.EqualTo(1), "Idle behavior is unchanged.");

            var operation = service.ReconcileContentWithProgress("currency.test", false);
            operation.Error(_ => { });
            callback();
            callback();
            Assert.That(calls, Is.EqualTo(1), "An already-open menu must respect the current busy state.");
            Assert.That(service.CurrentProgress.IsActive, Is.True);
            Assert.That(commands.Calls, Is.EqualTo(1));

            if (outcome == "transport failure")
                commands.Pending.Completion.CompleteError(new InvalidOperationException("controlled failure"));
            else
            {
                if (outcome == "reported failure") commands.Pending.ReportError("controlled failure");
                commands.Pending.Completion.CompleteSuccess();
            }

            Assert.That(service.CurrentProgress.IsActive, Is.False);
            Assert.That(calls, Is.EqualTo(1), "Blocked input must not be queued for later execution.");
            callback();
            Assert.That(calls, Is.EqualTo(2));
            InvokeWindow(window, "StartEditEntryName", service.EntriesCache["currency.test"]);
            Assert.That(typeof(ContentWindow).GetField("_editItemId", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(window), Is.EqualTo("currency.test"));
        }

        [TestCase(false, false, true, true, 1)]
        [TestCase(false, false, false, false, 1)]
        [TestCase(true, false, true, false, 0)]
        [TestCase(false, true, true, false, 1)]
        public void Delete_confirmation_rechecks_busy_before_accepting_the_choice(
            bool initiallyBusy, bool becomesBusy, bool confirmed, bool expected, int expectedPrompts)
        {
            var service = CreateService(new ResolveCommands());
            var window = CreateWindow(service);
            if (initiallyBusy) service.ReconcileContentWithProgress("currency.test", false);
            int prompts = 0;
            var accepted = (bool)InvokeWindow(window, "ConfirmContentInput", new Func<bool>(() =>
            {
                prompts++;
                if (becomesBusy) service.ReconcileContentWithProgress("currency.test", false);
                return confirmed;
            }));
            Assert.That(accepted, Is.EqualTo(expected));
            Assert.That(prompts, Is.EqualTo(expectedPrompts));
        }

        static ContentWindow CreateWindow(CliContentService service)
        {
            var window = (ContentWindow)FormatterServices.GetUninitializedObject(typeof(ContentWindow));
            typeof(ContentWindow).GetField("_contentService", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(window, service);
            return window;
        }

        static object InvokeWindow(ContentWindow window, string method, params object[] args) =>
            typeof(ContentWindow).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(window, args);

        [TestCase(false)]
        [TestCase(true)]
        public void Reported_failure_is_visible_and_retry_releases_progress(bool transportFailure)
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            Exception observed = null;
            var operation = service.ReconcileContentWithProgress("currency.test", false);
            operation.Error(error => observed = error);
            Assert.That(service.CurrentProgress.IsActive, Is.True);
            Assert.That(operation.IsCompleted, Is.False);
            const string message = "The realm changed. Refresh and review this item again.";
            if (transportFailure) commands.Pending.Completion.CompleteError(new InvalidOperationException(message));
            else { commands.Pending.ReportError(message); commands.Pending.Completion.CompleteSuccess(); }
            Assert.That(operation.IsCompleted, Is.True);
            Assert.That(observed, Is.Not.Null);
            Assert.That(observed.Message, Does.Contain(message));
            Assert.That(service.CurrentProgress.IsActive, Is.False);
            commands.Pending = new ResolveCommand();
            var retry = service.ReconcileContentWithProgress("currency.test", false);
            commands.Pending.Completion.CompleteSuccess();
            Assert.That(retry.IsCompleted && !retry.IsFailed, Is.True);
            Assert.That(service.CurrentProgress.IsActive, Is.False);
            Assert.That(service.EntriesCache["currency.test"].TargetManifestUid, Is.EqualTo("reviewed"));
        }

        [Test]
        public void Repeated_resolution_does_not_start_another_command_or_clear_active_progress()
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            var first = service.ReconcileContentWithProgress("currency.test", false);
            Exception rejected = null;
            var duplicate = service.ReconcileContentWithProgress("currency.test", true);
            duplicate.Error(error => rejected = error);
            Assert.That(commands.Calls, Is.EqualTo(1));
            Assert.That(rejected, Is.Not.Null);
            Assert.That(service.CurrentProgress.IsActive, Is.True);
            commands.Pending.Completion.CompleteSuccess();
            Assert.That(first.IsCompleted && !first.IsFailed, Is.True);
            Assert.That(service.CurrentProgress.IsActive, Is.False);
        }

        static CliContentService CreateService(ResolveCommands commands)
        {
            var service = (CliContentService)FormatterServices.GetUninitializedObject(typeof(CliContentService));
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            void Set(string name, object value) => typeof(CliContentService).GetField(name, fields).SetValue(service, value);
            Set("_cli", commands);
            var cli = (global::Beamable.Editor.BeamCli.BeamCli)FormatterServices.GetUninitializedObject(typeof(global::Beamable.Editor.BeamCli.BeamCli));
            cli.latestConfig = new BeamConfigCommandResult { cid = "test", pid = "realm" };
            cli.pidToRealm = new Dictionary<string, RealmView> { ["realm"] = new RealmView { Cid = "test", Pid = "realm" } };
            var context = new BeamEditorContext();
            typeof(BeamEditorContext).GetField("<ServiceScope>k__BackingField", fields)
                .SetValue(context, new DependencyBuilder().AddSingleton(cli).Build());
            Set("_beamContext", context);
            Set("_contentWrites", new ContentWriteTracker());
            Set("_invalidContents", new Dictionary<string, LocalContentManifestEntry>());
            Set("_contentScriptableCache", new Dictionary<string, ContentObject>());
            Set("_pendingTagSaves", Activator.CreateInstance(typeof(CliContentService).GetField("_pendingTagSaves", fields).FieldType));
            Set("_lastSavedPropertiesCache", Activator.CreateInstance(typeof(CliContentService).GetField("_lastSavedPropertiesCache", fields).FieldType));
            var configuration = (ContentConfiguration)FormatterServices.GetUninitializedObject(typeof(ContentConfiguration));
            configuration.MaxLocalAutoSnapshot = new OptionalInt();
            Set("_contentConfiguration", configuration);
            Set("<CurrentProgress>k__BackingField", new ContentOperationProgress());
            Set("<EntriesCache>k__BackingField", new Dictionary<string, LocalContentManifestEntry> {
                ["currency.test"] = new LocalContentManifestEntry { FullId = "currency.test", TargetManifestUid = "reviewed" }
            });
            // Exercise the real operation without starting native Editor watchers in this isolated test.
            Set("_watchersDisposed", true);
            service.manifestIdOverride = "global";
            return service;
        }

        class ResolveCommands : BeamCommands
        {
            public ResolveCommand Pending = new();
            public int Calls;
            public ContentResolveArgs LastArgs;
            public ResolveCommands() : base(null, null) { }
            public override ContentPublishWrapper ContentPublish(ContentPublishArgs args) => new() { Command = Pending };
            public override ContentSaveWrapper ContentSave(ContentSaveArgs args) => new() { Command = Pending };
            public override ContentTagSetWrapper ContentTagSet(ContentTagSetArgs args) => new() { Command = Pending };
            public override ContentResolveWrapper ContentResolve(ContentResolveArgs args)
            {
                LastArgs = args;
                Calls++;
                Assert.That(args.expectedManifestUid, Is.EqualTo("reviewed"));
                Assert.That(args.filter, Is.EqualTo("currency.test"));
                return new ContentResolveWrapper { Command = Pending };
            }
        }

        [Test]
        public void Preview_is_scoped_read_only_and_does_not_start_an_action()
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            service.CreateReconciliationPreview("currency.test", "test/realm/global", "reviewed");
            Assert.That(commands.LastArgs.preview, Is.True);
            Assert.That(commands.LastArgs.manifestIds, Is.EqualTo(new[] { "global" }));
            Assert.That(commands.LastArgs.filterType, Is.EqualTo(ContentFilterType.ExactIds));
            Assert.That(service.CurrentProgress.IsActive, Is.False);
        }

        [TestCase("scope")]
        [TestCase("publication")]
        [TestCase("busy")]
        [TestCase("unsaved")]
        [TestCase("write")]
        public void Preview_rejects_stale_or_unsettled_content_before_dispatch(string reason)
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            var pending = new Promise();
            if (reason == "scope") service.manifestIdOverride = "different";
            if (reason == "publication") service.EntriesCache.Clear();
            if (reason == "busy") service.CurrentProgress.IsActive = true;
            if (reason == "unsaved") service.HasPendingInspectorEdits += _ => true;
            if (reason == "write")
            {
                var writes = (ContentWriteTracker)typeof(CliContentService).GetField("_contentWrites", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
                _ = writes.Run("test/realm/global/currency.test", () => pending);
            }
            Assert.Throws<InvalidOperationException>(() => service.CreateReconciliationPreview("currency.test", "test/realm/global", "reviewed"));
            Assert.That(commands.Calls, Is.Zero);
            pending.CompleteSuccess();
        }

        [Test]
        public void Apply_passes_the_reviewed_local_revision_to_the_cli()
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            var operation = service.ReconcileContentWithProgress("currency.test", false, "test/realm/global", "reviewed", "revision");
            Assert.That(commands.LastArgs.expectedLocalRevision, Is.EqualTo("revision"));
            Assert.That(commands.LastArgs.preview, Is.False);
            commands.Pending.Completion.CompleteSuccess();
            Assert.That(operation.IsCompleted && !operation.IsFailed, Is.True);
        }

        [Test]
        public void Review_cannot_keep_an_invalid_conflict_but_can_accept_the_realm()
        {
            var commands = new ResolveCommands();
            var service = CreateService(commands);
            var entry = service.EntriesCache["currency.test"];
            entry.IsInConflict = true;
            service.EntriesCache["currency.test"] = entry;
            service.UpdateContentValidationStatus("currency.test", true);
            Exception observed = null;
            service.ReconcileContentWithProgress("currency.test", true).Error(error => observed = error);
            Assert.That(observed?.Message, Does.Contain("Fix validation errors"));
            Assert.That(commands.Calls, Is.Zero);
            var operation = service.ReconcileContentWithProgress("currency.test", false);
            commands.Pending.Completion.CompleteSuccess();
            Assert.That(operation.IsCompleted && !operation.IsFailed, Is.True);
        }

        class ResolveCommand : IBeamCommand
        {
            public readonly Promise Completion = new();
            Action<ReportDataPoint<ErrorOutput>> error;
            public void ReportError(string message) => error?.Invoke(new ReportDataPoint<ErrorOutput> { data = new ErrorOutput { message = message } });
            public void SetCommand(string command) { }
            public Promise Run() => Completion;
            public void Cancel() { }
            public IBeamCommand On<T>(string type, Action<ReportDataPoint<T>> callback) => this;
            public IBeamCommand On(Action<ReportDataPointDescription> callback) => this;
            public IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> callback) { error += callback; return this; }
            public IBeamCommand OnTerminate(Action<ReportDataPoint<EofOutput>> callback) => this;
        }
    }

    public class ContentPresentationTests
    {
        [TestCase(560f, 2000f)]
        [TestCase(760f, 6000f)]
        [TestCase(1200f, 10000f)]
        public void Review_headers_allow_long_text_to_shrink_to_the_available_width(float windowWidth, float textWidth)
        {
            const BindingFlags fields = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var optionsField = typeof(ContentReconciliationWindow).GetField("HeaderLabelOptions", fields);
            Assert.That(optionsField, Is.Not.Null, "Header labels must override their intrinsic text minimum width.");
            // Exercise Unity's real layout-option consumer, without needing a native GUIStyle or Editor window.
            var entryType = typeof(GUILayoutUtility).Assembly.GetType("UnityEngine.GUILayoutEntry", true);
            var entry = FormatterServices.GetUninitializedObject(entryType);
            entryType.GetField("minWidth", fields).SetValue(entry, textWidth);
            entryType.GetField("maxWidth", fields).SetValue(entry, textWidth);
            entryType.GetMethod("ApplyOptions", fields).Invoke(entry, new[] { optionsField.GetValue(null) });
            var minimum = (float)entryType.GetField("minWidth", fields).GetValue(entry);
            Assert.That(minimum, Is.Zero);
            Assert.That(minimum, Is.LessThan(windowWidth - 28), "Leave the existing outer padding inside the window.");
            Assert.That(entryType.GetField("stretchWidth", fields).GetValue(entry), Is.EqualTo(1));
        }

        [TestCase(0f, 240f, 100000, 0, 11)]
        [TestCase(2400f, 240f, 100000, 100, 111)]
        [TestCase(2399976f, 240f, 100000, 99999, 100000)]
        [TestCase(0f, 240f, 0, 0, 0)]
        public void Review_scrolling_limits_rendering_to_visible_rows(float scroll, float height, int count, int expectedFirst, int expectedEnd)
        {
            ContentReconciliationWindow.VisibleRows(scroll, height, count, out var first, out var end);
            Assert.That(first, Is.EqualTo(expectedFirst));
            Assert.That(end, Is.EqualTo(expectedEnd));
        }

        [TestCase(false, true, ContentChangeKind.IncomingAddition, "Added in the realm")]
        [TestCase(true, false, ContentChangeKind.IncomingDeletion, "Deleted from the realm")]
        [TestCase(false, true, ContentChangeKind.Conflict, "Only in the published realm")]
        [TestCase(true, false, ContentChangeKind.Unknown, "Only present locally")]
        public void Review_summary_only_attributes_history_when_known(bool local, bool realm, ContentChangeKind kind, string title)
        {
            Assert.That(ContentReconciliationWindow.SummaryTitle(local, realm, kind), Is.EqualTo(title));
        }

        [TestCase(false, true, true, "a later publish will delete it")]
        [TestCase(false, true, false, "Add the published item")]
        [TestCase(true, false, true, "Keep your local copy")]
        [TestCase(true, false, false, "Delete this item from your local content")]
        [TestCase(true, true, false, "Replace your local copy")]
        [TestCase(false, false, false, "Keep this item absent locally")]
        [TestCase(false, false, true, "Keep this item absent locally")]
        public void Review_explains_whole_item_choices_including_rejected_additions(bool local, bool realm, bool keepLocal, string expected)
        {
            Assert.That(ContentReconciliationWindow.ActionExplanation(local, realm, keepLocal), Does.Contain(expected));
        }

        [TestCase("none")]
        [TestCase("item")]
        [TestCase("manifest")]
        [TestCase("revision")]
        [TestCase("local")]
        [TestCase("realm")]
        [TestCase("differences")]
        public void Incomplete_or_mismatched_reviews_cannot_enable_actions(string missing)
        {
            var preview = new ContentReconciliationPreview {
                ContentId = "currency.test", ManifestUid = "reviewed", LocalRevision = "revision",
                LocalExists = true, RealmExists = true, LocalJson = "{}", RealmJson = "{}",
                Differences = Array.Empty<ContentReconciliationDifference>()
            };
            Assert.DoesNotThrow(() => ContentReconciliationWindow.ValidatePreview(preview, "currency.test", "reviewed"));
            switch (missing)
            {
                case "none": preview = null; break;
                case "item": preview.ContentId = "another"; break;
                case "manifest": preview.ManifestUid = "newer"; break;
                case "revision": preview.LocalRevision = null; break;
                case "local": preview.LocalJson = null; break;
                case "realm": preview.RealmJson = null; break;
                case "differences": preview.Differences = null; break;
            }
            Assert.Throws<InvalidOperationException>(() => ContentReconciliationWindow.ValidatePreview(preview, "currency.test", "reviewed"));
        }

        [Test]
        public void Preview_and_review_revision_are_additive_command_arguments()
        {
            Assert.That(new ContentResolveArgs().Serialize(), Does.Not.Contain("--preview"));
            Assert.That(new ContentResolveArgs().Serialize(), Does.Not.Contain("--expected-local-revision"));
            var serialized = new ContentResolveArgs { preview = true, expectedLocalRevision = "token" }.Serialize();
            Assert.That(serialized, Does.Contain("--preview=True"));
            Assert.That(serialized, Does.Contain("--expected-local-revision=token"));
        }

        [TestCase(300f, 5)]
        [TestCase(450f, 5)]
        [TestCase(700f, 5)]
        [TestCase(1200f, 5)]
        [TestCase(300f, 1)]
        public void Footer_controls_fit_without_hiding_the_last_filter(float available, int count)
        {
            var method = typeof(ContentWindow).GetMethod("CalculateFooterButtonWidth", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var width = (float)method.Invoke(null, new object[] { available, count, 160f });
            Assert.That(width, Is.GreaterThanOrEqualTo(44f), "Leave room for the icon and count.");
            Assert.That(width, Is.LessThanOrEqualTo(160f));
            // The production footer reserves BASE_PADDING (5) between controls.
            Assert.That(width * count + 5f * (count - 1), Is.LessThanOrEqualTo(available));
        }

        [Test]
        public void Incoming_footer_counts_all_reviewable_entries_but_not_conflicts_or_local_edits()
        {
            var entries = new[] {
                new LocalContentManifestEntry { ChangeKind = 1 },
                new LocalContentManifestEntry { ChangeKind = 2 },
                new LocalContentManifestEntry { ChangeKind = 3 },
                new LocalContentManifestEntry { ChangeKind = 5 },
                new LocalContentManifestEntry { ChangeKind = 6 },
                new LocalContentManifestEntry { ChangeKind = 3, IsInConflict = true },
                default(LocalContentManifestEntry)
            };
            var method = typeof(ContentWindow).GetMethod("CountIncomingEntries", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            Assert.That(method.Invoke(null, new object[] { entries }), Is.EqualTo(4));
            Assert.That(method.Invoke(null, new object[] { Array.Empty<LocalContentManifestEntry>() }), Is.EqualTo(0));
        }

        [Test]
        public void Content_groups_include_types_present_only_in_cached_entries()
        {
            var method = typeof(ContentWindow).GetMethod("GetVisibleContentTypes", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "Incoming types must be visible even without a Unity class.");
            var types = (System.Collections.Generic.IEnumerable<string>)method.Invoke(null, new object[]
            {
                new[] { "currency", "items" }, new[] { "remote_only.child", "currency" }
            });
            Assert.That(types, Is.EqualTo(new[] { "currency", "items", "remote_only.child" }));
            var refreshed = (System.Collections.Generic.IEnumerable<string>)method.Invoke(null, new object[]
            {
                new[] { "currency", "items" }, new[] { "another_remote_type" }
            });
            Assert.That(refreshed, Does.Contain("another_remote_type"));
            Assert.That(refreshed, Does.Not.Contain("remote_only.child"));
        }

        [TestCase(0, true)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, true)]
        [TestCase(4, true)]
        [TestCase(5, true)]
        [TestCase(6, true)]
        public void Row_reconciliation_is_available_without_a_typed_Inspector(int kind, bool expected)
        {
            var entry = new LocalContentManifestEntry { FullId = "remote_only.item", TypeName = "remote_only",
                ChangeKind = kind, IsInConflict = kind == 4, TargetManifestUid = "reviewed" };
            Assert.That(Call("CanReviewEntry", entry), Is.EqualTo(expected));
        }

        [Test]
        public void Reconciliation_command_carries_selected_namespace_item_and_reviewed_publication()
        {
            var args = new global::Beamable.Editor.BeamCli.Commands.ContentResolveArgs
            {
                manifestIds = new[] { "seasonal" }, filter = "currency.coins",
                filterType = global::Beamable.Common.Content.ContentFilterType.ExactIds,
                use = "local", expectedManifestUid = "reviewed-uid"
            };
            var command = args.Serialize();
            Assert.That(command, Does.Contain("--manifest-ids=seasonal"));
            Assert.That(command, Does.Contain("--filter=currency.coins"));
            Assert.That(command, Does.Contain("--use=local"));
            Assert.That(command, Does.Contain("--expected-manifest-uid=reviewed-uid"));
        }

        [Test]
        public void Unknown_history_requires_reconciliation_even_when_values_match()
        {
            var entry = new LocalContentManifestEntry { ChangeKind = 0, CurrentStatus = (int)ContentStatus.UpToDate };
            Assert.That(ContentWindow_TooltipsHelper.NeedsReconciliation(entry), Is.True);
        }

        [TestCase(1, "Up to date", "matches the last known publication")]
        [TestCase(2, "Local edit", "unpublished changes")]
        [TestCase(3, "Incoming change", "your local copy is unchanged")]
        [TestCase(4, "Conflict", "Resolve the conflict before publishing")]
        [TestCase(5, "Incoming addition", "added in the realm")]
        [TestCase(6, "Incoming deletion", "deleted from the realm")]
        [TestCase(0, "Differs from publication", "Differs from latest publication")]
        [TestCase(98, "Differs from publication", "Differs from latest publication")]
        public void Uses_authoritative_presentation(int kind, string label, string explanation)
        {
            var entry = new LocalContentManifestEntry { CurrentStatus = (int)ContentStatus.Modified, ChangeKind = kind };
            Assert.That(Call("GetStatusLabel", entry), Is.EqualTo(label));
            Assert.That(Call("GetStatusDescription", entry, true), Does.Contain(explanation));
        }

        [Test]
        public void Conflict_flag_wins_over_stale_or_missing_metadata()
        {
            var entry = new LocalContentManifestEntry { ChangeKind = 1, IsInConflict = true };
            Assert.That(Call("GetStatusLabel", entry), Is.EqualTo("Conflict"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Incoming_addition_placeholder_is_not_described_as_deleted(bool localMode)
        {
            // The CLI retains Deleted for an absent local file, including a new realm item.
            var entry = new LocalContentManifestEntry
            {
                CurrentStatus = (int)ContentStatus.Deleted,
                ChangeKind = (int)ContentChangeKind.IncomingAddition
            };
            var message = ContentWindow_TooltipsHelper.GetSelectionDescription(new[] { entry }, 0, localMode, out var conflict);
            Assert.That(message, Does.StartWith("Incoming addition\n"));
            Assert.That(message, Does.Contain("added in the realm"));
            Assert.That(message, Does.Contain(localMode ? "Auto-sync is paused" : "Auto-sync is enabled"));
            Assert.That(message, Does.Not.Contain("deleted"));
            Assert.That(conflict, Is.False);
        }

        [Test]
        public void Confirmed_local_deletion_retains_an_explicit_unpublished_deletion_explanation()
        {
            var entry = new LocalContentManifestEntry
            {
                CurrentStatus = (int)ContentStatus.Deleted,
                ChangeKind = (int)ContentChangeKind.LocalEdit
            };
            var message = ContentWindow_TooltipsHelper.GetSelectionDescription(new[] { entry }, 0, true, out var conflict);
            Assert.That(message, Does.StartWith("Local edit\n"));
            Assert.That(message, Does.Contain("Your local copy has been deleted."));
            Assert.That(message, Does.Contain("This deletion has not been published."));
            Assert.That(conflict, Is.False);
        }

        [TestCase(0)]
        [TestCase(98)]
        public void Missing_placeholder_history_does_not_infer_a_deletion(int kind)
        {
            var entry = new LocalContentManifestEntry { CurrentStatus = (int)ContentStatus.Deleted, ChangeKind = kind };
            var message = ContentWindow_TooltipsHelper.GetSelectionDescription(new[] { entry }, 0, true, out _);
            Assert.That(message, Does.Contain("origin of this difference is unavailable"));
            Assert.That(message, Does.Not.Contain("deleted"));
            Assert.That(message, Does.Not.Contain("Incoming addition"));
        }

        [Test]
        public void Conflicted_placeholder_is_described_as_a_conflict_before_a_local_deletion()
        {
            var entry = new LocalContentManifestEntry
            {
                CurrentStatus = (int)ContentStatus.Deleted,
                ChangeKind = (int)ContentChangeKind.LocalEdit,
                IsInConflict = true
            };
            var message = ContentWindow_TooltipsHelper.GetSelectionDescription(new[] { entry }, 0, true, out var conflict);
            Assert.That(message, Does.StartWith("Conflict\n"));
            Assert.That(message, Does.Contain("Resolve the conflict before publishing"));
            Assert.That(message, Does.Not.Contain("Your local copy has been deleted."));
            Assert.That(conflict, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Mixed_placeholders_are_aggregated_regardless_of_selection_order(bool reverse)
        {
            var entries = new[]
            {
                new LocalContentManifestEntry { CurrentStatus = (int)ContentStatus.Deleted, ChangeKind = (int)ContentChangeKind.IncomingAddition },
                new LocalContentManifestEntry { CurrentStatus = (int)ContentStatus.Deleted, ChangeKind = (int)ContentChangeKind.LocalEdit }
            };
            if (reverse) Array.Reverse(entries);
            var message = ContentWindow_TooltipsHelper.GetSelectionDescription(entries, 0, true, out _);
            Assert.That(message, Does.StartWith("2 selected:"));
            Assert.That(message, Does.Contain("1 Incoming addition"));
            Assert.That(message, Does.Contain("1 Local edit"));
            Assert.That(message, Does.Not.Contain("Your local copy has been deleted."));
        }

        [TestCase(true, "paused")]
        [TestCase(false, "enabled")]
        public void Incoming_explains_auto_sync_without_promising_a_download(bool local, string state)
        {
            var entry = new LocalContentManifestEntry { ChangeKind = 3 };
            var explanation = (string)Call("GetStatusDescription", entry, local);
            Assert.That(explanation, Does.Contain("Auto-sync is " + state));
            Assert.That(explanation, Does.Not.Contain("Modified locally"));
        }

        [Test]
        public void Missing_metadata_never_infers_clean_from_legacy_status()
        {
            Assert.That(Call("GetStatusLabel", default(LocalContentManifestEntry)), Is.EqualTo("Status unavailable"));
        }

        [Test]
        public void Multi_selection_counts_each_state_and_preserves_conflict_warning()
        {
            var entries = new[] {
                new LocalContentManifestEntry { ChangeKind = 1 },
                new LocalContentManifestEntry { ChangeKind = 3 },
                new LocalContentManifestEntry { IsInConflict = true },
                default(LocalContentManifestEntry)
            };
            var message = ContentWindow_TooltipsHelper.GetSelectionDescription(entries, 0, true, out var conflict);
            Assert.That(conflict, Is.True);
            Assert.That(message, Does.Contain("4 selected"));
            Assert.That(message, Does.Contain("1 Up to date"));
            Assert.That(message, Does.Contain("1 Incoming change"));
            Assert.That(message, Does.Contain("1 Conflict"));
            Assert.That(message, Does.Contain("1 Status unavailable"));
            Assert.That(message, Does.Contain("Auto-sync is paused"));
        }

        [Test]
        public void Historical_previews_never_claim_to_describe_the_current_working_copy()
        {
            var message = ContentWindow_TooltipsHelper.GetSelectionDescription(Array.Empty<LocalContentManifestEntry>(), 1, false, out var conflict);
            Assert.That(conflict, Is.False);
            Assert.That(message, Does.StartWith("Historical preview"));
            Assert.That(message, Does.Contain("not the current working copy"));
            Assert.That(message, Does.Not.Contain("Up to date"));
        }

        [Test]
        public void Mixed_historical_and_current_selection_is_explicit()
        {
            var message = ContentWindow_TooltipsHelper.GetSelectionDescription(new[] { new LocalContentManifestEntry { ChangeKind = 2 } }, 1, false, out _);
            Assert.That(message, Does.Contain("2 selected"));
            Assert.That(message, Does.Contain("1 Historical preview"));
            Assert.That(message, Does.Contain("1 Local edit"));
        }

        [Test]
        public void Incoming_additions_and_deletions_are_included_in_multi_selection()
        {
            var entries = new[] { new LocalContentManifestEntry { ChangeKind = 5 }, new LocalContentManifestEntry { ChangeKind = 6 } };
            var message = ContentWindow_TooltipsHelper.GetSelectionDescription(entries, 0, true, out var conflict);
            Assert.That(conflict, Is.False);
            Assert.That(message, Does.Contain("1 Incoming addition"));
            Assert.That(message, Does.Contain("1 Incoming deletion"));
            Assert.That(message, Does.Contain("Auto-sync is paused"));
        }

        [TestCase(350f)]
        [TestCase(450f)]
        [TestCase(800f)]
        [TestCase(1400f)]
        public void Four_columns_fit_the_available_table_width(float available)
        {
            var method = typeof(ContentWindow).GetMethod("CalculateColumnWidths", BindingFlags.Static | BindingFlags.NonPublic);
            var widths = (float[])method.Invoke(null, new object[] { available });
            Assert.That(widths, Has.Length.EqualTo(4));
            Assert.That(Array.TrueForAll(widths, width => width > 0), Is.True);
            float sum = 0;
            foreach (var width in widths) sum += width;
            Assert.That(sum, Is.LessThanOrEqualTo(available));
        }

        private static object Call(string name, params object[] args)
        {
            var method = typeof(ContentWindow_TooltipsHelper).GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "Shared presentation mapping is missing");
            return method.Invoke(null, args);
        }

        [TestCase(202f)]
        [TestCase(280f)]
        [TestCase(450f)]
        [TestCase(900f)]
        public void Local_mode_layout_reserves_warning_before_adjacent_controls(float available)
        {
            var method = typeof(ContentWindow).GetMethod("CalculateLocalModeWidths", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var widths = (float[])method.Invoke(null, new object[] { available, true });
            Assert.That(widths[0], Is.GreaterThanOrEqualTo(30), "Switch must remain operable.");
            Assert.That(widths[1], Is.GreaterThanOrEqualTo(24), "Sync state must remain visible.");
            Assert.That(widths[2], Is.GreaterThanOrEqualTo(100), "Reserve the icon and Local Mode On label.");
            Assert.That(widths[0] + widths[1] + widths[2] + 8, Is.LessThanOrEqualTo(available));
        }

        [TestCase(0f, true)]
        [TestCase(70f, true)]
        [TestCase(120f, true)]
        [TestCase(202f, false)]
        public void Extremely_narrow_mode_controls_never_overlap_existing_controls(float available, bool local)
        {
            var method = typeof(ContentWindow).GetMethod("CalculateLocalModeWidths", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var widths = (float[])method.Invoke(null, new object[] { available, local });
            Assert.That(Array.TrueForAll(widths, width => width >= 0), Is.True);
            Assert.That(widths[0] + widths[1] + widths[2], Is.LessThanOrEqualTo(available));
            if (!local) Assert.That(widths[2], Is.Zero);
        }

        [TestCase("Switching", false, "Switching...")]
        [TestCase("Switching", true, "Switching...")]
        [TestCase("Failed", false, "Sync failed - Retry")]
        [TestCase("Failed", true, "Retry")]
        [TestCase("Paused", false, "Auto-sync paused")]
        [TestCase("Enabled", false, "Auto-sync enabled")]
        public void Auto_sync_labels_distinguish_requested_from_acknowledged_mode(string state, bool compact, string expected)
        {
            var method = typeof(ContentWindow).GetMethod("GetAutoSyncLabel", BindingFlags.Static | BindingFlags.NonPublic);
            var stateType = typeof(global::Beamable.Editor.ContentService.CliContentService).GetProperty("AutoSyncState").PropertyType;
            Assert.That(method.Invoke(null, new object[] { Enum.Parse(stateType, state), compact }), Is.EqualTo(expected));
        }
    }

    public class ContentLocalModeDialogTests
    {
        private const string Key = "Beamable.Content.LocalMode.SuppressWarning";
        private ContentConfiguration config;
        private bool previous;

        [SetUp]
        public void SetUp()
        {
            previous = SessionState.GetBool(Key, false);
            SessionState.EraseBool(Key);
            config = ScriptableObject.CreateInstance<ContentConfiguration>();
            config.EnableLocalContentInEditor = false;
        }

        [TearDown]
        public void TearDown()
        {
            SessionState.SetBool(Key, previous);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Only_confirmation_applies_activation_and_suppression(bool confirm, bool suppress)
        {
            Choose(confirm, suppress);
            Assert.That(config.EnableLocalContentInEditor, Is.EqualTo(confirm));
            Assert.That(SessionState.GetBool(Key, false), Is.EqualTo(confirm && suppress));
        }

        [Test]
        public void Cancellation_does_not_clear_previous_suppression()
        {
            SessionState.SetBool(Key, true);
            Choose(false, false);
            Assert.That(SessionState.GetBool(Key, false), Is.True);
            Assert.That(config.EnableLocalContentInEditor, Is.False);
        }

        [Test]
        public void Unchecked_activation_does_not_suppress_next_activation()
        {
            Choose(true, false);
            config.EnableLocalContentInEditor = false;
            Assert.That(SessionState.GetBool(Key, false), Is.False);
            Choose(false, true);
            Assert.That(config.EnableLocalContentInEditor, Is.False);
        }

        [Test]
        public void New_dialog_is_unchecked_and_closing_it_leaves_mode_unchanged()
        {
            var type = typeof(ContentWindow_TooltipsHelper).Assembly.GetType("Beamable.Editor.UI.ContentWindow.ContentLocalModeWindow");
            var dialog = (EditorWindow)ScriptableObject.CreateInstance(type);
            Assert.That(type.GetField("_suppress", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(dialog), Is.False);
            type.GetField("_configuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(dialog, config);
            // Closing a window destroys it. Exercise that lifetime without requiring a graphics device in CI.
            UnityEngine.Object.DestroyImmediate(dialog);
            Assert.That(config.EnableLocalContentInEditor, Is.False);
            Assert.That(SessionState.GetBool(Key, false), Is.False);
        }

        [Test]
        public void Confirmation_saves_only_the_configuration_asset()
        {
            var prefix = "Assets/__ContentModeTest_" + Guid.NewGuid().ToString("N");
            var configPath = prefix + ".asset";
            var unrelatedPath = prefix + "_unrelated.asset";
            var unrelated = ScriptableObject.CreateInstance<ContentConfiguration>();
            try
            {
                AssetDatabase.CreateAsset(config, configPath);
                AssetDatabase.CreateAsset(unrelated, unrelatedPath);
                var before = System.IO.File.ReadAllBytes(unrelatedPath);
                unrelated.EnableLocalContentInEditor = true;
                EditorUtility.SetDirty(unrelated);
                Choose(true, false);
                Assert.That(System.IO.File.ReadAllText(configPath), Does.Contain("EnableLocalContentInEditor: 1"));
                Assert.That(System.IO.File.ReadAllBytes(unrelatedPath), Is.EqualTo(before));
                Assert.That(EditorUtility.IsDirty(unrelated), Is.True);
            }
            finally
            {
                AssetDatabase.DeleteAsset(configPath);
                AssetDatabase.DeleteAsset(unrelatedPath);
                if (unrelated != null) UnityEngine.Object.DestroyImmediate(unrelated);
            }
        }

        private void Choose(bool confirm, bool suppress)
        {
            var type = typeof(ContentWindow_TooltipsHelper).Assembly.GetType("Beamable.Editor.UI.ContentWindow.ContentLocalModeWindow");
            Assert.That(type, Is.Not.Null, "Local Mode confirmation dialog is missing");
            type.GetMethod("ApplyChoice", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { config, confirm, suppress });
        }
    }

    public class ContentLocalModeSessionReloadTests
    {
        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator Suppression_survives_real_script_reload()
        {
            const string key = "Beamable.Content.LocalMode.SuppressWarning";
            const string savedKey = key + ".TestPrevious";
            SessionState.SetBool(savedKey, SessionState.GetBool(key, false));
            SessionState.SetBool(key, true);
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            yield return new UnityEngine.TestTools.RecompileScripts();
            try { Assert.That(SessionState.GetBool(key, false), Is.True); }
            finally
            {
                SessionState.SetBool(key, SessionState.GetBool(savedKey, false));
                SessionState.EraseBool(savedKey);
            }
        }
    }

}
