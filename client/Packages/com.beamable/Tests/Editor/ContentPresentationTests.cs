using System;
using System.Reflection;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Content;
using Beamable.Editor.UI.ContentWindow;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Tests
{
    public class ContentPresentationTests
    {
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
