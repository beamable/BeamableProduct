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
using Beamable.Common.Content.Validation;
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
    public class ContentUnifiedIssuesTests
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [TestCase(ContentChangeKind.UpToDate, false, false, false)]
        [TestCase(ContentChangeKind.LocalEdit, false, false, false)]
        [TestCase(ContentChangeKind.LocalEdit, true, false, true)]
        [TestCase(ContentChangeKind.LocalEdit, false, true, true)]
        [TestCase(ContentChangeKind.LocalEdit, true, true, true)]
        [TestCase(ContentChangeKind.IncomingChange, false, false, true)]
        [TestCase(ContentChangeKind.IncomingAddition, false, false, true)]
        [TestCase(ContentChangeKind.IncomingDeletion, false, false, true)]
        [TestCase(ContentChangeKind.IncomingChange, true, false, true)]
        [TestCase(ContentChangeKind.Unknown, false, false, true)]
        public void Issues_filter_and_publish_summary_agree(ContentChangeKind kind, bool invalid, bool conflict, bool expected)
        {
            var entry = Entry("test", kind, conflict);
            var window = Window(new[] { entry }, invalid ? new[] { entry.FullId } : Array.Empty<string>());
            var hasIssue = (bool)Call(window, "HasContentIssue", entry);
            var filtered = (bool)Call(window, "FilterItem", "", new HashSet<string>(), entry, new HashSet<string>(), new HashSet<string> { "issues" }, "");
            Assert.That(hasIssue, Is.EqualTo(expected));
            Assert.That(filtered, Is.EqualTo(expected));
            Assert.That(Total(window), Is.EqualTo(expected ? 1 : 0));
        }

        [TestCase(ContentChangeKind.IncomingAddition, ContentStatus.Deleted)]
        [TestCase(ContentChangeKind.IncomingDeletion, ContentStatus.Created)]
        public void Issues_includes_incoming_presence_changes_despite_local_status(ContentChangeKind kind, ContentStatus status)
        {
            var entry = Entry("presence", kind);
            entry.CurrentStatus = (int)status;
            var window = Window(new[] { entry }, Array.Empty<string>());
            Assert.That(Call(window, "FilterItem", "", new HashSet<string>(), entry, new HashSet<string>(), new HashSet<string> { "issues" }, ""), Is.True);
            Assert.That(Total(window), Is.EqualTo(1));
        }

        [Test]
        public void Combined_summary_counts_items_once_and_shows_all_blocker_categories()
        {
            var entries = new[] {
                Entry("clean", ContentChangeKind.UpToDate), Entry("edit", ContentChangeKind.LocalEdit),
                Entry("invalid", ContentChangeKind.LocalEdit), Entry("conflict", ContentChangeKind.LocalEdit, true),
                Entry("update", ContentChangeKind.IncomingChange), Entry("addition", ContentChangeKind.IncomingAddition),
                Entry("deletion", ContentChangeKind.IncomingDeletion), Entry("unknown", ContentChangeKind.Unknown)
            };
            var window = Window(entries, new[] { "currency.invalid", "currency.conflict", "currency.update" });
            var summary = Call(window, "GetPublishIssueSummary");
            Assert.That(Total(window), Is.EqualTo(6), "Invalid/incoming and invalid/conflicted items count once in Issues.");
            foreach (var expected in new[] { ("Invalid", 3), ("Conflicted", 1), ("Updates", 1), ("Additions", 1), ("Deletions", 1), ("Unknown", 1) })
                Assert.That(summary.GetType().GetField(expected.Item1).GetValue(summary), Is.EqualTo(expected.Item2));
        }

        [Test]
        public void Resolving_one_issue_keeps_remaining_issues_visible_until_all_are_cleared()
        {
            var incoming = Entry("incoming", ContentChangeKind.IncomingAddition);
            var conflict = Entry("conflict", ContentChangeKind.LocalEdit, true);
            var window = Window(new[] { incoming, conflict }, Array.Empty<string>());
            var service = (CliContentService)typeof(ContentWindow).GetField("_contentService", Members).GetValue(window);
            Assert.That(Total(window), Is.EqualTo(2));
            conflict.IsInConflict = false;
            service.EntriesCache[conflict.FullId] = conflict;
            Assert.That(Total(window), Is.EqualTo(1));
            Assert.That(Call(window, "HasContentIssue", incoming), Is.True);
            incoming.ChangeKind = (int)ContentChangeKind.UpToDate;
            service.EntriesCache[incoming.FullId] = incoming;
            Assert.That(Total(window), Is.Zero);
        }

        [Test]
        public void Status_picker_has_only_the_consolidated_issues_filter()
        {
            Assert.That(Enum.GetNames(typeof(ContentFilterStatus)), Does.Not.Contain("Incoming"));
            var statuses = (Dictionary<ContentFilterStatus, string>)typeof(ContentWindow).GetField("StatusMapToString", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.That(statuses.Values, Does.Not.Contain("incoming"));
            Assert.That(statuses.Values, Does.Contain("issues"));
        }

        static LocalContentManifestEntry Entry(string name, ContentChangeKind kind, bool conflict = false) => new LocalContentManifestEntry {
            FullId = "currency." + name, Name = name, TypeName = "currency", Tags = Array.Empty<string>(),
            ChangeKind = (int)kind, IsInConflict = conflict, CurrentStatus = (int)ContentStatus.Modified
        };
        static object Call(ContentWindow window, string method, params object[] args) => typeof(ContentWindow).GetMethod(method, Members).Invoke(window, args);
        static int Total(ContentWindow window)
        {
            var summary = Call(window, "GetPublishIssueSummary");
            return (int)summary.GetType().GetProperty("Total").GetValue(summary);
        }
        static ContentWindow Window(LocalContentManifestEntry[] entries, string[] invalidIds)
        {
            // Only managed cache/filter methods run: no Editor initialization, asset access or CLI commands.
            var service = (CliContentService)FormatterServices.GetUninitializedObject(typeof(CliContentService));
            var cache = new Dictionary<string, LocalContentManifestEntry>();
            foreach (var entry in entries) cache.Add(entry.FullId, entry);
            var invalid = new Dictionary<string, LocalContentManifestEntry>();
            foreach (var id in invalidIds) invalid.Add(id, cache[id]);
            typeof(CliContentService).GetField("<EntriesCache>k__BackingField", Members).SetValue(service, cache);
            typeof(CliContentService).GetField("_invalidContents", Members).SetValue(service, invalid);
            var renames = typeof(CliContentService).GetField("_renameRegistry", Members);
            renames.SetValue(service, Activator.CreateInstance(renames.FieldType));
            var window = (ContentWindow)FormatterServices.GetUninitializedObject(typeof(ContentWindow));
            typeof(ContentWindow).GetField("_contentService", Members).SetValue(window, service);
            typeof(ContentWindow).GetField("_activeFilters", Members).SetValue(window, new Dictionary<ContentSearchFilterType, HashSet<string>>());
            return window;
        }
    }

    public class ContentJsonComparisonTests
    {
        [TestCase("head\nold\ntail", "head\nnew\ntail", "head|old|tail", "head|new|tail")]
        [TestCase("head\ntail", "head\nadded\ntail", "head|<gap>|tail", "head|added|tail")]
        [TestCase("head\nremoved\ntail", "head\ntail", "head|removed|tail", "head|<gap>|tail")]
        [TestCase("head\nold\ntail", "head\nnew\nextra\ntail", "head|old|<gap>|tail", "head|new|extra|tail")]
        [TestCase(null, "{\n}", "<gap>|<gap>", "{|}")]
        [TestCase("{\n}", null, "{|}", "<gap>|<gap>")]
        [TestCase(null, null, "", "")]
        [TestCase("same\n", "same\n", "same|", "same|")]
        [TestCase("", "new", "", "new")]
        [TestCase("{\r\n  \"text\": \"<b>café</b>\\n\"\r\n}", "{\n  \"text\": \"<b>café</b>\\n\"\n}", "{|  \"text\": \"<b>café</b>\\n\"|}", "{|  \"text\": \"<b>café</b>\\n\"|}")]
        public void Aligns_changed_and_missing_lines_without_editing_values(string local, string realm, string left, string right)
        {
            var result = ContentReconciliationWindow.CompareJson(local, realm);
            Assert.That(Display(result.Local), Is.EqualTo(left));
            Assert.That(Display(result.Realm), Is.EqualTo(right));
            Assert.That(result.Simplified, Is.False);
            Assert.That(result.Changed.Length, Is.EqualTo(result.Local.Length));
            for (int i = 0; i < result.Changed.Length; i++)
                Assert.That(result.Changed[i], Is.EqualTo(result.Local[i] != result.Realm[i]));
            AssertPreserved(local, result.Local);
            AssertPreserved(realm, result.Realm);
        }

        [Test]
        public void Repeated_braces_and_separate_edits_keep_unchanged_lines_aligned()
        {
            var result = ContentReconciliationWindow.CompareJson("{\na\n}\n{\nb\n}\n{\nc\n}", "{\nA\n}\n{\nb\n}\n{\nC\n}");
            Assert.That(result.Local.Length, Is.EqualTo(9));
            for (int i = 0; i < 9; i++) Assert.That(result.Local[i] == result.Realm[i], Is.EqualTo(i != 1 && i != 7));
        }

        [Test]
        public void Large_disjoint_inputs_bound_alignment_work_without_losing_lines()
        {
            var local = new string[5000];
            var realm = new string[6000];
            for (int i = 0; i < local.Length; i++) local[i] = "local " + i;
            for (int i = 0; i < realm.Length; i++) realm[i] = "realm " + i;
            var result = ContentReconciliationWindow.CompareJson(string.Join("\n", local), string.Join("\n", realm));
            Assert.That(result.Simplified, Is.True);
            Assert.That(result.Local.Length, Is.EqualTo(result.Realm.Length));
            CollectionAssert.AreEqual(local, Array.FindAll(result.Local, x => x != null));
            CollectionAssert.AreEqual(realm, Array.FindAll(result.Realm, x => x != null));
        }

        [Test]
        public void Large_common_prefix_and_suffix_need_no_simplification()
        {
            var local = new string[20000];
            for (int i = 0; i < local.Length; i++) local[i] = "line " + i;
            var realm = (string[])local.Clone();
            realm[10000] = "changed";
            var result = ContentReconciliationWindow.CompareJson(string.Join("\n", local), string.Join("\n", realm));
            Assert.That(result.Simplified, Is.False);
            Assert.That(result.Local.Length, Is.EqualTo(local.Length));
            CollectionAssert.AreEqual(local, result.Local);
            CollectionAssert.AreEqual(realm, result.Realm);
        }

        [TestCase(999, false)]
        [TestCase(1000, true)]
        public void Alignment_budget_boundary_is_explicit(int localCount, bool simplified)
        {
            var local = new string[localCount];
            var realm = new string[999];
            for (int i = 0; i < local.Length; i++) local[i] = "local " + i;
            for (int i = 0; i < realm.Length; i++) realm[i] = "realm " + i;
            var result = ContentReconciliationWindow.CompareJson(string.Join("\n", local), string.Join("\n", realm));
            Assert.That(result.Simplified, Is.EqualTo(simplified));
            Assert.That(Array.TrueForAll(result.Changed, x => x), Is.True);
        }

        static string Display(string[] lines) => string.Join("|", Array.ConvertAll(lines, x => x ?? "<gap>"));
        static void AssertPreserved(string input, string[] aligned)
        {
            var expected = input == null ? Array.Empty<string>() : input.Replace("\r\n", "\n").Split('\n');
            CollectionAssert.AreEqual(expected, Array.FindAll(aligned, x => x != null));
        }
    }

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
        [TestCase("Unrecognized command or argument '--preview=True'.", true)]
        [TestCase("Unrecognized command or argument '--preview'.", true)]
        [TestCase("Unrecognized command or argument '--expected-local-revision=abc123'.", true)]
        [TestCase("Unrecognized command or argument '--expected-local-revision'.", true)]
        [TestCase("An error occurred while sending the request.", false)]
        [TestCase("The realm changed after this item was reviewed.", false)]
        [TestCase("Unrecognized command or argument '--preview-other=True'.", false)]
        public void Review_errors_distinguish_unsupported_cli_options_from_retryable_failures(string error, bool needsUpdate)
        {
            var message = ContentReconciliationWindow.ReviewErrorMessage(error);
            Assert.That(message, Does.Contain(error), "Keep the original diagnostic available.");
            if (needsUpdate)
            {
                Assert.That(message, Does.Contain("Update the CLI"));
                Assert.That(message, Does.Contain("restart Unity"));
                Assert.That(message, Does.Not.Contain("Refresh and review before choosing again."));
            }
            else
            {
                Assert.That(message, Does.Contain("Refresh and review before choosing again."));
                Assert.That(message, Does.Not.Contain("Update the CLI"));
            }
        }

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

        [TestCase(false, true, ContentChangeKind.IncomingAddition, "New item in the realm")]
        [TestCase(true, false, ContentChangeKind.IncomingDeletion, "Deleted from the realm")]
        [TestCase(false, true, ContentChangeKind.Conflict, "Only in the published realm")]
        [TestCase(true, false, ContentChangeKind.Unknown, "Only present locally")]
        public void Review_summary_only_attributes_history_when_known(bool local, bool realm, ContentChangeKind kind, string title)
        {
            Assert.That(ContentReconciliationWindow.SummaryTitle(local, realm, kind), Is.EqualTo(title));
        }

        [TestCase(false, true, ContentChangeKind.IncomingAddition, "New custom content in the realm")]
        [TestCase(false, true, ContentChangeKind.Conflict, "Only in the published realm")]
        [TestCase(true, false, ContentChangeKind.IncomingDeletion, "Deleted from the realm")]
        public void Missing_script_summary_preserves_known_change_direction(bool local, bool realm, ContentChangeKind kind, string title)
        {
            Assert.That(ContentReconciliationWindow.SummaryTitle(local, realm, kind, true), Is.EqualTo(title));
        }

        [TestCase(false, true, false, "Import this item?")]
        [TestCase(false, true, true, "Import this item as JSON?")]
        [TestCase(true, false, false, "Keep this item or delete it locally?")]
        [TestCase(true, true, false, "Which version should stay local?")]
        [TestCase(false, false, false, "Which version should stay local?")]
        public void Outcome_title_describes_the_whole_item_decision(bool local, bool realm, bool missing, string expected)
        {
            Assert.That(ContentReconciliationWindow.DecisionTitle(local, realm, missing), Is.EqualTo(expected));
        }

        [TestCase(false, true, "Keep Local Version", "deletion on your next publish")]
        [TestCase(true, false, "Use Realm Version", "deletes your local copy")]
        public void Destructive_consequences_remain_visible_with_consistent_button_names(bool local, bool realm, string action, string consequence)
        {
            var text = ContentReconciliationWindow.DecisionConsequence(local, realm);
            Assert.That(text, Does.Contain(action));
            Assert.That(text, Does.Contain(consequence));
        }

        [TestCase(true, true)]
        [TestCase(false, false)]
        public void Non_destructive_choices_do_not_invent_deletion_warnings(bool local, bool realm)
        {
            Assert.That(ContentReconciliationWindow.DecisionConsequence(local, realm), Is.Empty);
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

        [TestCase(false, true, null, "{}", true)]
        [TestCase(false, true, "", "{}", true)]
        [TestCase(true, false, "{}", null, true)]
        [TestCase(true, false, "{}", "", true)]
        [TestCase(false, false, "", "", true)]
        [TestCase(true, true, "{}", "{}", true)]
        [TestCase(true, true, "", "{}", false)]
        [TestCase(true, true, "{}", "", false)]
        [TestCase(false, true, "{}", "{}", false)]
        [TestCase(true, false, "{}", "{}", false)]
        public void Review_validates_presence_and_normalizes_absent_json(bool localExists, bool realmExists,
            string localJson, string realmJson, bool valid)
        {
            var preview = new ContentReconciliationPreview {
                ContentId = "currency.test", ManifestUid = "reviewed", LocalRevision = "revision",
                LocalExists = localExists, RealmExists = realmExists, LocalJson = localJson, RealmJson = realmJson,
                Differences = Array.Empty<ContentReconciliationDifference>()
            };
            if (!valid)
            {
                Assert.Throws<InvalidOperationException>(() => ContentReconciliationWindow.ValidatePreview(preview, "currency.test", "reviewed"));
                return;
            }
            ContentReconciliationWindow.ValidatePreview(preview, "currency.test", "reviewed");
            Assert.That(preview.LocalJson, Is.EqualTo(localExists ? localJson : null), "Absent local content must use the placeholder and disable Copy JSON.");
            Assert.That(preview.RealmJson, Is.EqualTo(realmExists ? realmJson : null), "Absent realm content must use the placeholder and disable Copy JSON.");
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

        [TestCase(300f, 4)]
        [TestCase(450f, 4)]
        [TestCase(700f, 4)]
        [TestCase(1200f, 4)]
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
            Assert.That(ContentWindow_TooltipsHelper.GetStatusLabel(entry), Is.EqualTo(label));
            Assert.That(Call("GetStatusDescription", entry, true), Does.Contain(explanation));
        }

        [TestCase(ContentChangeKind.UpToDate)]
        [TestCase(ContentChangeKind.LocalEdit)]
        [TestCase(ContentChangeKind.IncomingChange)]
        [TestCase(ContentChangeKind.IncomingAddition)]
        [TestCase(ContentChangeKind.IncomingDeletion)]
        [TestCase(ContentChangeKind.Conflict)]
        [TestCase(ContentChangeKind.Unknown)]
        public void Validation_label_takes_priority_without_changing_sync_state(ContentChangeKind kind)
        {
            var entry = new LocalContentManifestEntry { ChangeKind = (int)kind };
            Assert.That(ContentWindow_TooltipsHelper.GetStatusLabel(entry, true), Is.EqualTo("Validation Error"));
            Assert.That(ContentWindow_TooltipsHelper.GetChangeKind(entry), Is.EqualTo(kind));
            Assert.That(ContentWindow_TooltipsHelper.GetStatusLabel(entry, false), Is.Not.EqualTo("Validation Error"));
        }

        [TestCase(ContentChangeKind.LocalEdit, false)]
        [TestCase(ContentChangeKind.Conflict, true)]
        [TestCase(ContentChangeKind.IncomingChange, false)]
        public void Invalid_inspector_explains_actual_errors_and_retains_other_blockers(ContentChangeKind kind, bool expectedConflict)
        {
            var entry = new LocalContentManifestEntry { ChangeKind = (int)kind };
            var message = ContentWindow_TooltipsHelper.GetSelectionDescription(new[] { entry }, 0, true, out var conflict,
                1, "Summary: Cannot be empty string");
            Assert.That(message, Does.StartWith("Validation Error\n"));
            Assert.That(message, Does.Contain("Summary: Cannot be empty string"));
            Assert.That(message, Does.Contain("before publishing"));
            Assert.That(conflict, Is.EqualTo(expectedConflict));
            if (expectedConflict) Assert.That(message, Does.Contain("Resolve the conflict"));
            if (kind == ContentChangeKind.IncomingChange) Assert.That(message, Does.Contain("Auto-sync is paused"));
        }

        [Test]
        public void Invalid_multi_selection_aggregates_without_misattributing_first_item_details()
        {
            var entries = new[] { new LocalContentManifestEntry { ChangeKind = 1 }, new LocalContentManifestEntry { ChangeKind = 2 } };
            var message = ContentWindow_TooltipsHelper.GetSelectionDescription(entries, 1, false, out _, 1, "Summary: test error");
            Assert.That(message, Does.StartWith("Validation Error\n1 selected item(s)"));
            Assert.That(message, Does.Contain("Historical preview"));
            Assert.That(message, Does.Not.Contain("Summary: test error"));
        }

        [Test]
        public void Historical_preview_never_inherits_current_validation_errors()
        {
            var message = ContentWindow_TooltipsHelper.GetSelectionDescription(Array.Empty<LocalContentManifestEntry>(), 1,
                false, out _, 1, "Summary: test error");
            Assert.That(message, Does.StartWith("Historical preview"));
            Assert.That(message, Does.Not.Contain("Validation Error"));
        }

        [Test]
        public void Conflict_flag_wins_over_stale_or_missing_metadata()
        {
            var entry = new LocalContentManifestEntry { ChangeKind = 1, IsInConflict = true };
            Assert.That(ContentWindow_TooltipsHelper.GetStatusLabel(entry), Is.EqualTo("Conflict"));
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
            Assert.That(ContentWindow_TooltipsHelper.GetStatusLabel(default), Is.EqualTo("Status unavailable"));
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

        [Test]
        public void Shared_table_header_places_status_immediately_after_name()
        {
            var labels = (string[])typeof(ContentWindow).GetField("ItemColumnLabels", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.That(labels, Is.EqualTo(new[] { "Name", "Status", "Tags", "Latest Update" }));
        }

        [TestCase(600f)]
        [TestCase(800f)]
        [TestCase(1400f)]
        public void Shared_table_cells_fit_without_overlapping_and_reserve_status_icon_space(float available)
        {
            var widths = (float[])typeof(ContentWindow).GetMethod("CalculateColumnWidths", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { available });
            var labelMethod = typeof(ContentWindow).GetMethod("GetTableLabelRect", BindingFlags.Static | BindingFlags.NonPublic);
            float x = 0;
            for (int column = 0; column < widths.Length; column++)
            {
                var cell = new Rect(x, 0, widths[column], 20);
                var label = (Rect)labelMethod.Invoke(null, new object[] { cell, column });
                Assert.That(label.xMin, Is.EqualTo(x + (column == 1 ? 25 : 5)));
                Assert.That(label.xMax, Is.LessThanOrEqualTo(cell.xMax - 5));
                Assert.That(label.width, Is.GreaterThan(0));
                if (column == 1) Assert.That(label.width, Is.GreaterThanOrEqualTo(130));
                x += widths[column];
            }
            Assert.That(x, Is.EqualTo(available).Within(0.01f));
        }

        [TestCase(0f)]
        [TestCase(120f)]
        public void Header_and_rows_keep_the_same_status_alignment_when_scrolled(float scrollX)
        {
            var method = typeof(ContentWindow).GetMethod("GetTableLabelRect", BindingFlags.Static | BindingFlags.NonPublic);
            var header = (Rect)method.Invoke(null, new object[] { new Rect(200 - scrollX, 0, 160, 20), 1 });
            var row = (Rect)method.Invoke(null, new object[] { new Rect(200, 100, 160, 20), 1 });
            Assert.That(header.x, Is.EqualTo(row.x - scrollX));
            Assert.That(header.width, Is.EqualTo(row.width));
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

    // Uses real content validators and native Unity objects; run inside Unity.
    public class ContentTableStyleTests
    {
        [Test]
        public void Table_styles_are_cached_and_long_labels_clip_within_their_cell()
        {
            var window = (ContentWindow)FormatterServices.GetUninitializedObject(typeof(ContentWindow));
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var build = typeof(ContentWindow).GetMethod("BuildItemsPanelStyles", flags);
            build.Invoke(window, null);
            var names = new[] { "_itemPanelHeaderFieldStyle", "_itemLabelStyle", "_incomingStatusStyle", "_conflictStatusStyle", "_invalidStatusStyle" };
            var styles = new GUIStyle[names.Length];
            for (int i = 0; i < names.Length; i++) styles[i] = (GUIStyle)typeof(ContentWindow).GetField(names[i], flags).GetValue(window);
            build.Invoke(window, null);
            for (int i = 0; i < names.Length; i++)
            {
                Assert.That(typeof(ContentWindow).GetField(names[i], flags).GetValue(window), Is.SameAs(styles[i]));
                Assert.That(styles[i].padding.left, Is.Zero, "Header and row label geometry must agree.");
            }
#if UNITY_6000_3_OR_NEWER
            Assert.That(styles[1].clipping, Is.EqualTo(TextClipping.Ellipsis));
#else
            Assert.That(styles[1].clipping, Is.EqualTo(TextClipping.Clip));
#endif
            Assert.That(styles[1].wordWrap, Is.False);
            foreach (var name in new[] { "_itemPanelHeaderRowStyle", "_rowEvenItemStyle", "_rowOddItemStyle", "_rowSelectedItemStyle" })
            {
                var style = (GUIStyle)typeof(ContentWindow).GetField(name, flags).GetValue(window);
                UnityEngine.Object.DestroyImmediate(style.normal.background);
            }
        }
    }

    public class ContentValidationStatusTests
    {
        [Test]
        public void Announcement_summary_error_is_readable_and_clears_after_correction()
        {
            var content = ScriptableObject.CreateInstance<global::Beamable.Common.Announcements.AnnouncementContent>();
            try
            {
                content.summary = "";
                var context = new ValidationContext();
                var errors = content.GetMemberValidationErrors(context);
                var details = global::Beamable.Editor.Content.UI.ContentObjectEditor.FormatValidationDetails(content, errors);
                Assert.That(details, Does.Contain("Summary: Cannot be empty string"));
                Assert.That(details, Does.Not.Contain("System.String"));
                content.summary = "Valid summary";
                Assert.That(global::Beamable.Editor.Content.UI.ContentObjectEditor.FormatValidationDetails(content,
                    content.GetMemberValidationErrors(context)), Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(content); }
        }

        [Test]
        public void Many_nested_errors_are_bounded_but_not_silently_dropped()
        {
            var content = ScriptableObject.CreateInstance<SaveRecoveryContent>();
            try
            {
                for (int i = 0; i < 500; i++) content.rows.Add(new SaveRecoveryRow { amount = -1 });
                var errors = content.GetMemberValidationErrors(new ValidationContext());
                var details = global::Beamable.Editor.Content.UI.ContentObjectEditor.FormatValidationDetails(content, errors);
                Assert.That(details.Split('\n').Length, Is.EqualTo(6));
                Assert.That(details, Does.Contain("495 more validation errors"));
                Assert.That(details, Does.Contain("Amount:"));
            }
            finally { UnityEngine.Object.DestroyImmediate(content); }
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

    // Runs in Unity: managed-only checks do not exercise native IMGUI layout and scrolling.
    public class ContentReconciliationLayoutTests
    {
        const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        const BindingFlags StaticFields = BindingFlags.Static | BindingFlags.NonPublic;

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Combined_publish_popup_measures_all_categories_in_both_modes(bool localMode, bool permission)
        {
            var owner = ScriptableObject.CreateInstance<ContentReconciliationWindow>();
            var previousEvent = Event.current;
            typeof(GUIUtility).GetMethod("BeginContainerFromOwner", StaticFields).Invoke(null, new object[] { owner });
            try
            {
                Event.current = new Event { type = EventType.Layout };
                typeof(GUIUtility).GetMethod("BeginGUI", StaticFields).Invoke(null, new object[] { 1, owner.GetInstanceID(), 1 });
                var summaryType = typeof(ContentWindow).GetNestedType("PublishIssueSummary", BindingFlags.NonPublic);
                var summary = Activator.CreateInstance(summaryType, new object[] { 6, 3, 1, 1, 1, 1, 1 });
                var popupType = typeof(ContentWindow).GetNestedType("PublishIssuesPopup", BindingFlags.NonPublic);
                var popup = (PopupWindowContent)Activator.CreateInstance(popupType, new object[] { summary, permission, "Realm: Test  |  Namespace: global", localMode, (Action)(() => { }) });
                var size = popup.GetWindowSize();
                Assert.That(size.x, Is.EqualTo(380));
                Assert.That(size.y, Is.InRange(200f, 650f), "All issue categories and the action must fit in the measured popup.");
            }
            finally
            {
                typeof(GUIUtility).GetMethod("EndContainer", StaticFields).Invoke(null, null);
                Event.current = previousEvent;
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [TestCase(560, 340, false, true, true)]
        [TestCase(560, 340, true, false, true)]
        [TestCase(560, 380, false, false, false)]
        [TestCase(560, 380, false, true, false)]
        [TestCase(560, 380, false, true, true)]
        [TestCase(560, 380, true, false, true)]
        [TestCase(760, 430, false, true, true)]
        [TestCase(1200, 600, false, true, true)]
        public void Json_review_keeps_usable_viewports_and_footer_inside_window(int width, int height, bool deletion, bool missingScript, bool error)
        {
            CheckLayout(width, height, deletion, missingScript, error ? "The realm changed after this item was reviewed." : null, 1);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Long_review_errors_scroll_without_displacing_footer(int tab)
        {
            CheckLayout(560, 380, false, true, new string('x', 4000), tab);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Existing_item_review_remains_usable_with_long_errors(int tab)
        {
            CheckLayout(560, 500, false, false, new string('x', 4000), tab, true);
        }

        [TestCase(560, 0)]
        [TestCase(560, 240)]
        [TestCase(1200, 240)]
        [TestCase(1200, 2000)]
        public void Json_columns_share_vertical_scroll_and_clamp_on_resize(int width, int offset)
        {
            var window = ScriptableObject.CreateInstance<ContentReconciliationWindow>();
            var previousEvent = Event.current;
            try
            {
                window.position = new Rect(0, 0, width, 360);
                var lines = new List<string>();
                for (int i = 0; i < 120; i++) lines.Add("  \"" + i + new string('x', i == 0 ? 400 : 0) + "\"");
                var localJson = "[\r\n" + string.Join(",\r\n", lines) + "\r\n]";
                lines.Insert(40, "  \"incoming\"");
                var realmJson = "[\r\n" + string.Join(",\r\n", lines) + "\r\n]";
                var comparison = ContentReconciliationWindow.CompareJson(localJson, realmJson);
                void Set(string field, object value) => typeof(ContentReconciliationWindow).GetField(field, InstanceFields).SetValue(window, value);
                object Get(string field) => typeof(ContentReconciliationWindow).GetField(field, InstanceFields).GetValue(window);
                Set("_preview", new ContentReconciliationPreview { LocalExists = true, RealmExists = true, LocalJson = localJson, RealmJson = realmJson });
                Set("_localLines", comparison.Local);
                Set("_realmLines", comparison.Realm);
                Set("_jsonChangedLines", comparison.Changed);
                Rect viewport = default;
                void Draw(Event input)
                {
                    typeof(GUIUtility).GetMethod("BeginContainerFromOwner", StaticFields).Invoke(null, new object[] { window });
                    try
                    {
                        Event.current = input;
                        typeof(GUIUtility).GetMethod("BeginGUI", StaticFields).Invoke(null, new object[] { 1, window.GetInstanceID(), 1 });
                        // The normal Editor event loop resets layout cursors between events.
                        var layoutCache = typeof(GUILayoutUtility).GetField("current", StaticFields).GetValue(null);
                        layoutCache.GetType().GetMethod("ResetCursor").Invoke(layoutCache, null);
                        typeof(ContentReconciliationWindow).GetMethod("EnsureStyles", InstanceFields).Invoke(window, null);
                        typeof(ContentReconciliationWindow).GetMethod("DrawJsonComparison", InstanceFields).Invoke(window, null);
                        if (input.type == EventType.Layout)
                            typeof(GUILayoutUtility).GetMethod("LayoutFromContainer", StaticFields).Invoke(null, new object[] { (float)width, window.position.height });
                        var cache = typeof(GUILayoutUtility).GetField("current", StaticFields).GetValue(null);
                        var root = cache.GetType().GetField("topLevel").GetValue(cache);
                        var viewports = new List<Rect>();
                        bool scrollable = false;
                        InspectLayout(root, viewports, ref scrollable);
                        Assert.That(viewports.Count, Is.EqualTo(1), "Both columns must share a single vertical viewport.");
                        viewport = viewports[0];

                    }
                    finally { typeof(GUIUtility).GetMethod("EndContainer", StaticFields).Invoke(null, null); }
                }
                Draw(new Event { type = EventType.Layout });
                Draw(new Event { type = EventType.Repaint });
                Assert.That(((GUIStyle)Get("_json")).richText, Is.False, "JSON must be displayed literally, not interpreted as markup.");
                Set("_jsonScroll", new Vector2(0, offset));
                Draw(new Event { type = EventType.Repaint });
                var scroll = (Vector2)Get("_jsonScroll");
                Assert.That(scroll.y, Is.EqualTo(offset));
                Assert.That(scroll.x, Is.Zero);
                Assert.That((float)Get("_localScroll"), Is.Zero);
                Assert.That((float)Get("_realmScroll"), Is.Zero);

                // Long lines scroll horizontally without moving the other column or changing vertical alignment.
                Set("_localScroll", 100f);
                Draw(new Event { type = EventType.Repaint });
                Assert.That((float)Get("_localScroll"), Is.EqualTo(100f));
                Assert.That((float)Get("_realmScroll"), Is.Zero);
                Assert.That(((Vector2)Get("_jsonScroll")).y, Is.EqualTo(scroll.y));
                Set("_jsonScroll", new Vector2(0, float.MaxValue));
                Draw(new Event { type = EventType.Repaint });
                Assert.That(((Vector2)Get("_jsonScroll")).y, Is.EqualTo(comparison.Local.Length * 24 - viewport.height).Within(1));
                window.position = new Rect(0, 0, width, 500);
                Draw(new Event { type = EventType.Layout });
                Draw(new Event { type = EventType.Repaint });
                Assert.That(((Vector2)Get("_jsonScroll")).y, Is.EqualTo(comparison.Local.Length * 24 - viewport.height).Within(1));


            }
            finally
            {
                Event.current = previousEvent;
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [TestCase(560, 340, false, false)]
        [TestCase(560, 340, false, true)]
        [TestCase(560, 340, true, false)]
        [TestCase(680, 360, false, true)]
        [TestCase(1200, 500, true, false)]
        public void Compact_summary_keeps_consistent_actions_visible(int width, int height, bool deletion, bool missingScript)
        {
            CheckLayout(width, height, deletion, missingScript, null, 0);
        }

        [TestCase(560, 340, 0)]
        [TestCase(560, 340, 1)]
        [TestCase(680, 420, 0)]
        public void Conflict_review_with_long_identifier_keeps_actions_and_comparison_visible(int width, int height, int tab)
        {
            CheckLayout(width, height, false, false, null, tab, true, true);
        }

        static void CheckLayout(int width, int height, bool deletion, bool missingScript, string error, int tab, bool both = false, bool conflict = false)
        {
            // Execute the real window in Unity's IMGUI container without showing a window,
            // connecting a CLI, or using the project's configuration/content objects.
            var window = ScriptableObject.CreateInstance<ContentReconciliationWindow>();
            var config = ScriptableObject.CreateInstance<ContentConfiguration>();
            var configs = (System.Collections.IDictionary)typeof(ContentConfiguration).BaseType.BaseType
                .GetField("_typeToConfig", StaticFields).GetValue(null);
            var previousConfig = configs[typeof(ContentConfiguration)];
            var previousEvent = Event.current;
            try
            {
                config.EnableLocalContentInEditor = true;
                configs[typeof(ContentConfiguration)] = config;
                window.position = new Rect(0, 0, width, height);
                var service = (CliContentService)FormatterServices.GetUninitializedObject(typeof(CliContentService));
                typeof(CliContentService).GetField("<CurrentProgress>k__BackingField", InstanceFields)
                    .SetValue(service, new ContentOperationProgress());
                void Set(string field, object value) => typeof(ContentReconciliationWindow).GetField(field, InstanceFields).SetValue(window, value);
                Set("_service", service);
                Set("_contentId", conflict ? "currency." + new string('w', 500) : "currency.layout_test");
                Set("_scope", "test/realm/global");
                Set("_entry", new LocalContentManifestEntry {
                    TypeName = new string('w', 500),
                    ChangeKind = (int)(conflict ? ContentChangeKind.Conflict : both ? ContentChangeKind.IncomingChange : deletion ? ContentChangeKind.IncomingDeletion : ContentChangeKind.IncomingAddition)
                });
                Set("_preview", new ContentReconciliationPreview {
                    LocalExists = both || deletion, RealmExists = !deletion,
                    LocalJson = both || deletion ? "{}" : null, RealmJson = deletion ? null : "{}"
                });
                Set("_missingType", missingScript);
                Set("_error", error);
                Set("_tab", tab);
                Set("_rows", new[] { new[] { new GUIContent("amount"), new GUIContent("10"), new GUIContent("20") } });
                Set("_localLines", new[] { "{}" });
                Set("_realmLines", new[] { "{}" });
                Set("_jsonChangedLines", new[] { false });

                Rect footer = default;
                foreach (var eventType in new[] { EventType.Layout, EventType.Repaint })
                {
                    // Same native container/layout lifecycle used by Unity's IMGUIContainer.
                    typeof(GUIUtility).GetMethod("BeginContainerFromOwner", StaticFields).Invoke(null, new object[] { window });
                    try
                    {
                        Event.current = new Event { type = eventType };
                        typeof(GUIUtility).GetMethod("BeginGUI", StaticFields).Invoke(null, new object[] { 1, window.GetInstanceID(), 1 });
                        typeof(ContentReconciliationWindow).GetMethod("OnGUI", InstanceFields).Invoke(window, null);
                        if (eventType == EventType.Layout)
                            typeof(GUILayoutUtility).GetMethod("LayoutFromContainer", StaticFields).Invoke(null, new object[] { (float)width, (float)height });
                        else footer = GUILayoutUtility.GetLastRect();
                    }
                    finally { typeof(GUIUtility).GetMethod("EndContainer", StaticFields).Invoke(null, null); }
                }
                var keep = (GUIContent)typeof(ContentReconciliationWindow).GetField("_keepLocalLabel", InstanceFields).GetValue(window);
                var realm = (GUIContent)typeof(ContentReconciliationWindow).GetField("_useRealmLabel", InstanceFields).GetValue(window);
                Assert.That(keep.text, Is.EqualTo("Keep Local Version"));
                Assert.That(realm.text, Is.EqualTo("Use Realm Version"));
                Assert.That(keep.tooltip, Is.Not.Empty);
                Assert.That(realm.tooltip, Is.Not.Empty);
                Assert.That(footer.yMax, Is.LessThanOrEqualTo(height), "Footer must remain inside the window even when review details overflow.");
                var cache = typeof(GUILayoutUtility).GetField("current", StaticFields).GetValue(null);
                var root = cache.GetType().GetField("topLevel").GetValue(cache);
                var viewports = new List<Rect>();
                bool scrollable = false;
                InspectLayout(root, viewports, ref scrollable);
                CheckActionBounds(root, width, height);
                if (error?.Length > 1000)
                    Assert.That(scrollable, Is.True, "Overflowing review details must remain reachable by scrolling.");
                if (tab == 1 || both)
                {
                    Assert.That(viewports.Count, Is.EqualTo(1), "The shared JSON viewport or property viewport must be laid out.");
                    foreach (var viewport in viewports)
                        Assert.That(viewport.height, Is.GreaterThanOrEqualTo(96), "Content needs room for at least four lines, independently of warning/error height.");
                }
            }
            finally
            {
                Event.current = previousEvent;
                if (previousConfig == null) configs.Remove(typeof(ContentConfiguration));
                else configs[typeof(ContentConfiguration)] = previousConfig;
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        static void CheckActionBounds(object entry, int width, int height)
        {
            var type = entry.GetType();
            var rect = (Rect)type.GetField("rect").GetValue(entry);
            if (rect.height == 30 && (rect.width == 160 || rect.width == 90))
            {
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0));
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(width), "Action button cannot be clipped horizontally.");
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(height), "Action button cannot be clipped vertically.");
            }
            if (type.GetField("entries")?.GetValue(entry) is System.Collections.IEnumerable children)
                foreach (var child in children) CheckActionBounds(child, width, height);
        }

        static void InspectLayout(object entry, List<Rect> viewports, ref bool scrollable)
        {
            var type = entry.GetType();
            var rect = (Rect)type.GetField("rect").GetValue(entry);
            if (type.Name == "GUILayoutEntry" && rect.width > 0 && (int)type.GetField("stretchHeight").GetValue(entry) > 0)
                viewports.Add(rect);
            // EditorGUILayout passes clientHeight and rect.height to the native scroll view.
            if (type.Name == "GUIScrollGroup" && (float)type.GetField("clientHeight").GetValue(entry) > rect.height)
                scrollable = true;
            if (type.GetField("entries")?.GetValue(entry) is System.Collections.IEnumerable children)
                foreach (var child in children) InspectLayout(child, viewports, ref scrollable);
        }
    }

    public class ContentReconciliationSerializationTests
    {
        // Captured from ContentService.PreviewReconciliation with the CLI reporter's
        // UnitySerializationSettings (0.0.123.25), using controlled local/realm content.
        private const string Update = @"{
  ""ContentId"": ""currency.coins"",
  ""ManifestUid"": ""reviewed"",
  ""LocalRevision"": ""0807cf41b2ffd934b9cba416998f412e2c0f84fcd0eb9b49a0cac08ce8689369"",
  ""LocalExists"": true,
  ""RealmExists"": true,
  ""LocalJson"": ""{\r\n  \""properties\"": {\r\n    \""startingAmount\"": {\r\n      \""data\"": 10\r\n    }\r\n  },\r\n  \""tags\"": []\r\n}"",
  ""RealmJson"": ""{\r\n  \""properties\"": {\r\n    \""startingAmount\"": {\r\n      \""data\"": 20\r\n    }\r\n  },\r\n  \""tags\"": [\r\n    \""realm\""\r\n  ]\r\n}"",
  ""Differences"": [
    {
      ""Property"": ""startingAmount"",
      ""LocalValue"": ""10"",
      ""RealmValue"": ""20""
    },
    {
      ""Property"": ""Tags"",
      ""LocalValue"": ""[]"",
      ""RealmValue"": ""[\""realm\""]""
    }
  ]
}";

        private const string Addition = @"{
  ""ContentId"": ""currency.coins"",
  ""ManifestUid"": ""reviewed"",
  ""LocalRevision"": ""c4d7738295a1a672911beae946f3875988edfee51ae3355f85c766777fb532b6"",
  ""LocalExists"": false,
  ""RealmExists"": true,
  ""LocalJson"": null,
  ""RealmJson"": ""{\r\n  \""properties\"": {\r\n    \""startingAmount\"": {\r\n      \""data\"": 20\r\n    }\r\n  },\r\n  \""tags\"": [\r\n    \""realm\""\r\n  ]\r\n}"",
  ""Differences"": []
}";

        private const string Deletion = @"{
  ""ContentId"": ""currency.coins"",
  ""ManifestUid"": ""reviewed"",
  ""LocalRevision"": ""b3a10ce959cc8d7bfcfe9c19cc527a4bfada18feddc3924795e983d9c57c7bff"",
  ""LocalExists"": true,
  ""RealmExists"": false,
  ""LocalJson"": ""{\r\n  \""properties\"": {\r\n    \""startingAmount\"": {\r\n      \""data\"": 10\r\n    }\r\n  },\r\n  \""tags\"": []\r\n}"",
  ""RealmJson"": null,
  ""Differences"": []
}";

        private const string Absent = @"{
  ""ContentId"": ""currency.coins"",
  ""ManifestUid"": ""reviewed"",
  ""LocalRevision"": ""01b62fed87cc0b6107fc840c5c6bc8d26e3b5383205d2ad41979559a852fa93c"",
  ""LocalExists"": false,
  ""RealmExists"": false,
  ""LocalJson"": null,
  ""RealmJson"": null,
  ""Differences"": []
}";

        [TestCase(Update, true, true)]
        [TestCase(Addition, false, true)]
        [TestCase(Deletion, true, false)]
        [TestCase(Absent, false, false)]
        public void Cli_preview_survives_native_unity_deserialization(string payload, bool localExists, bool realmExists)
        {
            var preview = JsonUtility.FromJson<ContentReconciliationPreview>(payload);
            Assert.That(preview.LocalExists, Is.EqualTo(localExists));
            Assert.That(preview.RealmExists, Is.EqualTo(realmExists));
            ContentReconciliationWindow.ValidatePreview(preview, "currency.coins", "reviewed");
            Assert.That(string.IsNullOrEmpty(preview.LocalJson), Is.EqualTo(!localExists));
            Assert.That(string.IsNullOrEmpty(preview.RealmJson), Is.EqualTo(!realmExists));
            if (!localExists) Assert.That(preview.LocalJson, Is.Null, "Absent local content must use its JSON placeholder.");
            if (!realmExists) Assert.That(preview.RealmJson, Is.Null, "Absent realm content must use its JSON placeholder.");
            var comparison = ContentReconciliationWindow.CompareJson(preview.LocalJson, preview.RealmJson);
            Assert.That(comparison.Local.Length, Is.EqualTo(comparison.Realm.Length));
            foreach (var side in new[] { (preview.LocalJson, comparison.Local), (preview.RealmJson, comparison.Realm) })
                CollectionAssert.AreEqual(side.Item1?.Replace("\r\n", "\n").Split('\n') ?? Array.Empty<string>(), Array.FindAll(side.Item2, x => x != null));
            if (localExists && realmExists)
            {
                int localValue = Array.FindIndex(comparison.Local, x => x != null && x.Contains("\"data\": 10"));
                Assert.That(localValue, Is.GreaterThanOrEqualTo(0));
                Assert.That(comparison.Realm[localValue], Does.Contain("\"data\": 20"));
                int realmTag = Array.FindIndex(comparison.Realm, x => x != null && x.Contains("\"realm\""));
                Assert.That(realmTag, Is.GreaterThanOrEqualTo(0));
                Assert.That(comparison.Local[realmTag], Is.Null);
            }
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
