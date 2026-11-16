using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Beamable.Common;
using Beamable.Common.Api.Realms;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Common.Content.Validation;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Content.UI;
using Beamable.Editor.ContentService;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Tests
{
	public class ContentSaveRecoveryTests
	{
		private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
		private CliContentService _service;
		private HeldCommands _commands;
		private SaveRecoveryContent _content;
		private string _path;
		private string _noticePath;

		[SetUp]
		public void SetUp()
		{
			_commands = new HeldCommands();
			var cli = new BeamCli.BeamCli(null)
			{
				latestConfig = new BeamConfigCommandResult { cid = "test", pid = "realm" },
				pidToRealm = new Dictionary<string, RealmView> { ["realm"] = new RealmView { Cid = "test", Pid = "realm" } }
			};
			var provider = new DependencyBuilder().AddSingleton(cli).AddSingleton(new ValidationContext()).Build();
			var context = new BeamEditorContext();
			Set(context, "<ServiceScope>k__BackingField", provider);
			// Bypass the constructor's real watcher/auth initialization; only command completion is controlled.
			_service = (CliContentService)FormatterServices.GetUninitializedObject(typeof(CliContentService));
			foreach (var field in typeof(CliContentService).GetFields(Fields))
				if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
					field.SetValue(_service, Activator.CreateInstance(field.FieldType));
			Set(_service, "_cli", _commands);
			Set(_service, "_beamContext", context);
			Set(_service, "_provider", provider);
			Set(_service, "_contentWrites", new ContentWriteTracker());
			Set(_service, "<CurrentProgress>k__BackingField", new ContentOperationProgress());
			Set(_service, "_watchersDisposed", true);
			_noticePath = Path.Combine(Path.GetTempPath(), "beam-schema-notices-" + Guid.NewGuid().ToString("N") + ".json");
			Set(_service, "_schemaNoticeCachePath", _noticePath);
			_service.manifestIdOverride = "global";
			_content = ScriptableObject.CreateInstance<SaveRecoveryContent>();
			_content.SetIdAndVersion("saveRecovery.test", "");
			_content.CancelPendingEditorChangeNotification();
			_content.ContentStatus = ContentStatus.UpToDate;
			_content.Tags = new[] { "old" };
			_content.ResetEditorChangeTracking();
		}

		[TearDown]
		public void TearDown()
		{
			// Settle held commands before destroying their objects, including tags started by a properties completion.
			foreach (var command in _commands.Properties.ToArray()) command.Completion.CompleteSuccess();
			foreach (var command in _commands.Tags.ToArray()) command.Completion.CompleteSuccess();
			_content.CancelPendingEditorChangeNotification();
			UnityEngine.Object.DestroyImmediate(_content);
			if (_path != null && File.Exists(_path)) File.Delete(_path);
			if (_noticePath != null && File.Exists(_noticePath)) File.Delete(_noticePath);
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Reconciliation_preserves_a_pending_or_running_property_save(bool commandStarted)
		{
			PrepareFile();
			var entry = _service.EntriesCache[_content.Id];
			entry.TargetManifestUid = "reviewed";
			_service.EntriesCache[_content.Id] = entry;
			_content.amount = 25;
			if (commandStarted) Save();
			else _content.CheckForNonDetectedChanges();
			Exception observed = null;
			_service.ReconcileContentWithProgress(_content.Id, false).Error(error => observed = error);
			Assert.That(observed?.Message, Does.Contain("still being saved"));
			Assert.That(_content.amount, Is.EqualTo(25));
			Assert.That(_service.CurrentProgress.IsActive, Is.False);
			Assert.That(_content.HasPendingEditorChangeNotification, Is.EqualTo(!commandStarted));
		}

		[TestCase("_pendingNameEdit")]
		[TestCase("_pendingTagEdit")]
		public void Inspector_header_debounce_blocks_reconciliation_until_its_callback_finishes(string field)
		{
			PrepareFile();
			var entry = _service.EntriesCache[_content.Id];
			entry.TargetManifestUid = "reviewed";
			_service.EntriesCache[_content.Id] = entry;
			var editor = UnityEditor.Editor.CreateEditor(_content, typeof(ContentObjectEditor));
			int edits = 0;
			try
			{
				typeof(ContentObjectEditor).GetMethod("ObservePendingEdits", Fields).Invoke(editor, new object[] { _service });
				Set(editor, field, (Action)(() => edits++));
				Exception observed = null;
				_service.ReconcileContentWithProgress(_content.Id, false).Error(error => observed = error);
				Assert.That(observed?.Message, Does.Contain("still being saved"));
				Assert.That(edits, Is.Zero);
			}
			finally { UnityEngine.Object.DestroyImmediate(editor); }
			Assert.That(edits, Is.Zero, "Deselecting must not flush delayed edits out of order.");
			Assert.That(Get(_service, "HasPendingInspectorEdits"), Is.Not.Null);
			typeof(ContentObjectEditor).GetMethod("CompleteHeaderEdit", Fields).Invoke(editor, new object[] { field == "_pendingNameEdit" });
			Assert.That(edits, Is.EqualTo(1));
			Assert.That(Get(_service, "HasPendingInspectorEdits"), Is.Null, "Destroyed Inspectors must release their subscription.");
		}

		[Test]
		public void Inspector_guard_follows_the_same_target_after_its_id_changes()
		{
			var editor = UnityEditor.Editor.CreateEditor(_content, typeof(ContentObjectEditor));
			var observe = typeof(ContentObjectEditor).GetMethod("ObservePendingEdits", Fields);
			var pending = typeof(ContentObjectEditor).GetMethod("HasPendingEdits", Fields);
			try
			{
				observe.Invoke(editor, new object[] { _service });
				_content.SetContentName("renamed");
				observe.Invoke(editor, new object[] { _service });
				Set(editor, "_pendingTagEdit", (Action)(() => { }));
				Assert.That(pending.Invoke(editor, new object[] { _content.Id }), Is.True);
				Assert.That(pending.Invoke(editor, new object[] { "saveRecovery.test" }), Is.False);
			}
			finally
			{
				Set(editor, "_pendingTagEdit", null);
				UnityEngine.Object.DestroyImmediate(editor);
			}
		}

		[TestCase(false)]
		[TestCase(true)]
		public void NewerDirectTagsSuppressOldPropertiesContinuation(bool directTagsFinishFirst)
		{
			var save = Save();
			_content.Tags = new[] { "new" };
			_service.SetContentTags(_content.Id, _content.Tags);
			if (directTagsFinishFirst) _commands.Tags[0].Completion.CompleteSuccess();
			_commands.Properties[0].Completion.CompleteSuccess();
			save.GetResult();
			Assert.That(_commands.Tags.Count, Is.EqualTo(1));
			Assert.That(_commands.TagValues.Single(), Is.EqualTo("new"));
		}

		[TestCase(false)]
		[TestCase(true)]
		public void NewestSaveOwnsTagsRegardlessOfPropertiesCompletionOrder(bool newestFinishesFirst)
		{
			// Complete the held writes in both orders to make the stale continuation deterministic.
			var older = Save();
			_content.amount++;
			_content.Tags = new[] { "new" };
			var newer = Save();
			_commands.Properties[newestFinishesFirst ? 1 : 0].Completion.CompleteSuccess();
			_commands.Properties[newestFinishesFirst ? 0 : 1].Completion.CompleteSuccess();
			Assert.That(_commands.TagValues, Is.EqualTo(new[] { "new" }));
			_commands.Tags[0].Completion.CompleteSuccess();
			older.GetResult();
			newer.GetResult();
			Assert.That(PendingTagCount(), Is.Zero);
		}

		[Test]
		public void FailedSaveReleasesPendingTagsAndAllowsRetry()
		{
			var save = Save();
			Exception observed = null;
			save.Error(e => observed = e);
			_commands.Properties[0].Completion.CompleteError(new Exception("Write failed"));
			Assert.That(observed, Is.Not.Null);
			Assert.That(PendingTagCount(), Is.Zero);
			var retry = Save();
			_commands.Properties[1].Completion.CompleteSuccess();
			_commands.Tags[0].Completion.CompleteSuccess();
			retry.GetResult();
		}

		[Test]
		public void FileDeleteFailureRestoresStatusAndSaveCallback()
		{
			PrepareFile();
			var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
			Set(_service, "_contentConfiguration", config);
			var revision = _service.ManifestChangedCount;
			config.validateSchemaDifference = true;
			_service.UpdateSchemaDifference(_service.EntriesCache[_content.Id], SchemaDifference.UnknownFieldsInJson);
			Action callback = () => _service.SaveContent(_content);
			_content.OnEditorChanged = callback;
			Exception error = null;
			// A directory deterministically rejects File.Delete on all Editor platforms.
			File.Delete(_path);
			Directory.CreateDirectory(_path);
			try
			{
				_service.DeleteContent(_content.Id).Error(e => error = e);
				Assert.That(error, Is.Not.Null);
				Assert.That(_content.ContentStatus, Is.EqualTo(ContentStatus.UpToDate));
				Assert.That(_service.ManifestChangedCount, Is.EqualTo(revision));
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.UnknownFieldsInJson));
				Assert.That(_content.OnEditorChanged, Is.SameAs(callback));
				_content.OnEditorChanged();
				Assert.That(_commands.Properties.Count, Is.EqualTo(1));
			}
			finally { Directory.Delete(_path); UnityEngine.Object.DestroyImmediate(config); }
		}

		[TestCase(ContentStatus.Created)]
		[TestCase(ContentStatus.Modified)]
		[TestCase(ContentStatus.UpToDate)]
		public void Successful_deletion_refreshes_inspectors_and_removes_schema_notice(ContentStatus previousStatus)
		{
			PrepareFile();
			var entry = _service.EntriesCache[_content.Id];
			entry.CurrentStatus = (int)previousStatus;
			entry.ChangeKind = (int)(previousStatus == ContentStatus.UpToDate ? ContentChangeKind.UpToDate : ContentChangeKind.LocalEdit);
			entry.TypeName = "saveRecovery";
			_service.EntriesCache[_content.Id] = entry;
			var reflection = new ContentTypeReflectionCache();
			reflection.OnSetupForCacheGeneration();
			((Dictionary<string, Type>)Get(reflection, "_contentTypeToClass"))["saveRecovery"] = typeof(SaveRecoveryContent);
			((Dictionary<Type, string>)Get(reflection, "_classToContentType"))[typeof(SaveRecoveryContent)] = "saveRecovery";
			Set(_service, "_contentTypeReflectionCache", reflection);
			var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
			try
			{
				Set(_service, "_contentConfiguration", config);
				config.validateSchemaDifference = true;
				_service.UpdateSchemaDifference(entry, SchemaDifference.UnknownFieldsInJson);
				var revision = _service.ManifestChangedCount;
				_service.DeleteContent(_content.Id).GetResult();
				Assert.That(File.Exists(_path), Is.False);
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.None));
				Assert.That(_service.ManifestChangedCount, Is.GreaterThan(revision));
				Assert.That(_service.EntriesCache.ContainsKey(_content.Id), Is.EqualTo(previousStatus != ContentStatus.Created));
				if (previousStatus != ContentStatus.Created)
				{
					var deleted = _service.EntriesCache[_content.Id];
					Assert.That(deleted.StatusEnum, Is.EqualTo(ContentStatus.Deleted));
					Assert.That(global::Beamable.Editor.UI.ContentWindow.ContentWindow_TooltipsHelper.GetStatusLabel(deleted), Is.EqualTo("Deleted"));
				}
			}
			finally
			{
				foreach (var cached in ((Dictionary<string, ContentObject>)Get(_service, "_contentScriptableCache")).Values)
					if (cached != _content) UnityEngine.Object.DestroyImmediate(cached);
				UnityEngine.Object.DestroyImmediate(config);
			}
		}

		[Test]
		public void FailedDeleteInOldScopeDoesNotReviveCurrentScopeObject()
		{
			PrepareFile();
			var save = Save();
			var deletion = _service.DeleteContent(_content.Id);
			Exception error = null;
			deletion.Error(e => error = e);
			_service.manifestIdOverride = "other";
			File.Delete(_path);
			Directory.CreateDirectory(_path);
			try
			{
				_commands.Properties[0].Completion.CompleteSuccess();
				Assert.That(error, Is.Not.Null);
				Assert.That(_content.ContentStatus, Is.EqualTo(ContentStatus.Deleted));
				Assert.That(_content.OnEditorChanged, Is.Null);
				Assert.That(_commands.Tags.Count, Is.Zero);
				save.GetResult();
			}
			finally { Directory.Delete(_path); }
		}

		[Test]
		public void LargeCollapsedListCountsAllErrorsAndClearsAfterCorrection()
		{
			_content.rows = Enumerable.Range(0, 500).Select(_ => new SaveRecoveryRow { amount = -1 }).ToList();
			var context = new ValidationContext();
			using (var serialized = new SerializedObject(_content))
			{
				var rows = serialized.FindProperty("rows");
				rows.isExpanded = false;
				Assert.That(ContentObjectEditor.CountListValidationErrors(rows, _content.GetMemberValidationErrors(context)), Is.EqualTo(500));
				foreach (var row in _content.rows) row.amount = 1;
				serialized.Update();
				Assert.That(ContentObjectEditor.CountListValidationErrors(rows, _content.GetMemberValidationErrors(context)), Is.Zero);
			}
		}

		[TestCase(true, SchemaDifference.UnknownFieldsInJson)]
		[TestCase(true, SchemaDifference.MissingFieldsInJson)]
		[TestCase(true, SchemaDifference.UnknownFieldsInJson | SchemaDifference.MissingFieldsInJson)]
		[TestCase(false, SchemaDifference.UnknownFieldsInJson)]
		public void Schema_notice_comes_from_real_deserialization_and_survives_normalization(bool enabled, SchemaDifference difference)
		{
			PrepareFile();
			var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
			try
			{
				config.validateSchemaDifference = enabled;
				Set(_service, "_contentConfiguration", config);
				var properties = ClientContentSerializer.SerializeProperties(_content);
				var mismatchedProperties = (difference & SchemaDifference.MissingFieldsInJson) != 0 ? "{}" : properties;
				if ((difference & SchemaDifference.UnknownFieldsInJson) != 0)
					mismatchedProperties = mismatchedProperties.Insert(1, "\"futureField\":{\"data\":123}" + (mismatchedProperties == "{}" ? "" : ","));
				var json = "{\"properties\":" + mismatchedProperties + ",\"tags\":[]}";
				File.WriteAllText(_path, json);
				var entry = _service.EntriesCache[_content.Id];
				entry.ReferenceManifestUid = "publication-a";
				var loader = typeof(CliContentService).GetMethod("LoadContentObject", Fields);
				loader.Invoke(_service, new object[] { entry, _content });
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(enabled ? difference : SchemaDifference.None));
				Assert.That(_commands.Properties.Count, Is.EqualTo(enabled ? 1 : 0), "Existing schema normalization behavior is unchanged.");
				Assert.That(File.ReadAllText(_path), Is.EqualTo(json), "The controlled save command must not write the test file.");
				// Simulate the watcher report for the properties emitted by the existing automatic save.
				File.WriteAllText(_path, "{\"properties\":" + properties + ",\"tags\":[]}");
				entry.CurrentStatus = (int)ContentStatus.Modified;
				loader.Invoke(_service, new object[] { entry, _content });
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(enabled ? difference : SchemaDifference.None));
				entry.CurrentStatus = (int)ContentStatus.UpToDate;
				loader.Invoke(_service, new object[] { entry, _content });
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.None));
			}
			finally { UnityEngine.Object.DestroyImmediate(config); }
		}

		[Test]
		public void Schema_notice_survives_cache_reload_after_the_loader_normalizes_the_file()
		{
			PrepareFile();
			var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
			try
			{
				config.validateSchemaDifference = true;
				Set(_service, "_contentConfiguration", config);
				var properties = ClientContentSerializer.SerializeProperties(_content);
				File.WriteAllText(_path, "{\"properties\":" + properties.Insert(1, "\"futureField\":{\"data\":123},") + ",\"tags\":[]}");
				var entry = _service.EntriesCache[_content.Id];
				entry.ReferenceManifestUid = "publication-a";
				var loader = typeof(CliContentService).GetMethod("LoadContentObject", Fields);
				loader.Invoke(_service, new object[] { entry, _content });
				Assert.That(_commands.Properties.Count, Is.EqualTo(1));
				File.WriteAllText(_path, "{\"properties\":" + properties + ",\"tags\":[]}");
				entry.CurrentStatus = (int)ContentStatus.Modified;
				typeof(CliContentService).GetMethod("ClearCaches", Fields).Invoke(_service, null);
				loader.Invoke(_service, new object[] { entry, _content });
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.UnknownFieldsInJson));
			}
			finally { UnityEngine.Object.DestroyImmediate(config); }
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Loading_a_realm_version_then_inspecting_does_not_save_but_a_later_edit_does(bool alreadySelected)
		{
			PrepareFile();
			var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
			try
			{
				config.validateSchemaDifference = true;
				Set(_service, "_contentConfiguration", config);
				if (alreadySelected)
				{
					_content.amount = 99;
					_content.CheckForNonDetectedChanges();
					_content.CancelPendingEditorChangeNotification();
				}
				// Realm payload uses a different numeric representation from Unity's serializer.
				const string json = "{\"properties\":{\"amount\":{\"data\":2.0},\"rows\":{\"data\":[]}},\"tags\":[]}";
				File.WriteAllText(_path, json);
				typeof(CliContentService).GetMethod("LoadContentObject", Fields).Invoke(_service,
					new object[] { _service.EntriesCache[_content.Id], _content });
				Assert.That(_content.amount, Is.EqualTo(2));
				_content.CheckForNonDetectedChanges();
				Assert.That(_content.HasPendingEditorChangeNotification, Is.False, "Inspection must not enqueue a normalization save.");
				Assert.That(_commands.Properties, Is.Empty);
				Assert.That(File.ReadAllText(_path), Is.EqualTo(json));
				_content.amount = 3;
				_content.CheckForNonDetectedChanges();
				Assert.That(_content.HasPendingEditorChangeNotification, Is.True, "Real edits must still save.");
			}
			finally { UnityEngine.Object.DestroyImmediate(config); }
		}

		[TestCase("reference")]
		[TestCase("deleted")]
		[TestCase("disabled")]
		public void Schema_notice_does_not_leak_to_replaced_deleted_or_disabled_content(string change)
		{
			PrepareFile();
			var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
			try
			{
				Set(_service, "_contentConfiguration", config);
				config.validateSchemaDifference = true;
				var entry = _service.EntriesCache[_content.Id];
				entry.CurrentStatus = (int)ContentStatus.Modified;
				entry.ReferenceManifestUid = "old";
				_service.UpdateSchemaDifference(entry, SchemaDifference.MissingFieldsInJson);
				Assert.That(_service.GetSchemaDifference("another.item"), Is.EqualTo(SchemaDifference.None));
				if (change == "reference") entry.ReferenceManifestUid = "new";
				if (change == "deleted") entry.CurrentStatus = (int)ContentStatus.Deleted;
				if (change == "disabled")
				{
					config.validateSchemaDifference = false;
					Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.None), "Disabling the check must hide cached warnings without a reload.");
				}
				_service.UpdateSchemaDifference(entry, SchemaDifference.None);
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.None));
			}
			finally { UnityEngine.Object.DestroyImmediate(config); }
		}

		[Test]
		public void Schema_notice_cache_round_trips_and_is_scoped_to_realm_and_namespace()
		{
			PrepareFile();
			var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
			try
			{
				config.validateSchemaDifference = true;
				Set(_service, "_contentConfiguration", config);
				var entry = _service.EntriesCache[_content.Id];
				entry.CurrentStatus = (int)ContentStatus.Modified;
				entry.ReferenceManifestUid = "old";
				_service.UpdateSchemaDifference(entry, SchemaDifference.UnknownFieldsInJson);
				Assert.That(File.Exists(_noticePath), Is.True);
				// Reconstruct the in-memory state from the file written by the actual cache producer.
				((Dictionary<string, (SchemaDifference, string)>)Get(_service, "_schemaDifferences")).Clear();
				Set(_service, "_schemaNoticesLoaded", false);
				_service.UpdateSchemaDifference(entry, SchemaDifference.None);
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.UnknownFieldsInJson));
				_service.manifestIdOverride = "seasonal";
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.None));
				_service.manifestIdOverride = "global";
				var context = (BeamEditorContext)Get(_service, "_beamContext");
				context.BeamCli.CurrentRealm.Pid = "other-realm";
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.None));
				context.BeamCli.CurrentRealm.Pid = "realm";
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.UnknownFieldsInJson));
				entry.ReferenceManifestUid = "new";
				_service.UpdateSchemaDifference(entry, SchemaDifference.None);
				((Dictionary<string, (SchemaDifference, string)>)Get(_service, "_schemaDifferences")).Clear();
				Set(_service, "_schemaNoticesLoaded", false);
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.None), "Clearing is persisted too.");
			}
			finally { UnityEngine.Object.DestroyImmediate(config); }
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Schema_notices_clear_on_replacement_even_while_validation_is_disabled(bool newReference)
		{
			PrepareFile();
			var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
			try
			{
				Set(_service, "_contentConfiguration", config);
				config.validateSchemaDifference = true;
				var entry = _service.EntriesCache[_content.Id];
				entry.CurrentStatus = (int)ContentStatus.Modified;
				entry.ReferenceManifestUid = "old";
				_service.UpdateSchemaDifference(entry, SchemaDifference.UnknownFieldsInJson);
				config.validateSchemaDifference = false;
				_service.UpdateSchemaDifference(entry, SchemaDifference.None);
				config.validateSchemaDifference = true;
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.UnknownFieldsInJson));
				config.validateSchemaDifference = false;
				if (newReference) entry.ReferenceManifestUid = "new";
				else entry.CurrentStatus = (int)ContentStatus.UpToDate;
				_service.UpdateSchemaDifference(entry, SchemaDifference.None);
				config.validateSchemaDifference = true;
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.None));
			}
			finally { UnityEngine.Object.DestroyImmediate(config); }
		}

		[Test]
		public void Authoritative_snapshot_prunes_absent_schema_notices_only_in_its_own_scope()
		{
			PrepareFile();
			var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
			try
			{
				Set(_service, "_contentConfiguration", config);
				config.validateSchemaDifference = true;
				var entry = _service.EntriesCache[_content.Id];
				entry.CurrentStatus = (int)ContentStatus.Created;
				_service.UpdateSchemaDifference(entry, SchemaDifference.UnknownFieldsInJson);
				_service.manifestIdOverride = "seasonal";
				_service.UpdateSchemaDifference(entry, SchemaDifference.MissingFieldsInJson);
				_service.manifestIdOverride = "global";
				var prune = typeof(CliContentService).GetMethod("PruneSchemaNotices", Fields);
				var snapshot = new LocalContentManifest { OwnerCid = "test", OwnerPid = "realm", ManifestId = "global", Entries = new[] { entry } };
				prune.Invoke(_service, new object[] { snapshot });
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.UnknownFieldsInJson));
				snapshot.Entries = Array.Empty<LocalContentManifestEntry>();
				snapshot.OwnerPid = "other";
				prune.Invoke(_service, new object[] { snapshot });
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.UnknownFieldsInJson));
				snapshot.OwnerPid = "realm";
				prune.Invoke(_service, new object[] { snapshot });
				_service.UpdateSchemaDifference(entry, SchemaDifference.None);
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.None));
				_service.manifestIdOverride = "seasonal";
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.MissingFieldsInJson));
			}
			finally { UnityEngine.Object.DestroyImmediate(config); }
		}

		private void PrepareFile()
		{
			_path = Path.Combine(Path.GetTempPath(), "beam-content-" + Guid.NewGuid().ToString("N"));
			File.WriteAllText(_path, "{}");
			_service.EntriesCache[_content.Id] = new LocalContentManifestEntry { FullId = _content.Id, JsonFilePath = _path,
				CurrentStatus = (int)ContentStatus.UpToDate, Tags = _content.Tags };
			((Dictionary<string, ContentObject>)Get(_service, "_contentScriptableCache"))[_content.Id] = _content;
		}
		private Promise Save() => (Promise)typeof(CliContentService).GetMethod("SaveContentAsync", Fields).Invoke(_service, new object[] { _content });
		private int PendingTagCount() => ((System.Collections.IDictionary)Get(_service, "_pendingTagSaves")).Count;
		private static object Get(object obj, string name) => obj.GetType().GetField(name, Fields).GetValue(obj);
		private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Fields).SetValue(obj, value);

		private class HeldCommands : BeamCommands
		{
			public readonly List<HeldCommand> Properties = new();
			public readonly List<HeldCommand> Tags = new();
			public readonly List<string> TagValues = new();
			public HeldCommands() : base(null, null) { }
			public override ContentSaveWrapper ContentSave(ContentSaveArgs args)
			{
				var command = new HeldCommand(); Properties.Add(command);
				return new ContentSaveWrapper { Command = command };
			}
			public override ContentTagSetWrapper ContentTagSet(ContentTagSetArgs args)
			{
				var command = new HeldCommand(); Tags.Add(command); TagValues.Add(args.tag);
				return new ContentTagSetWrapper { Command = command };
			}
		}
		private class HeldCommand : IBeamCommand
		{
			public readonly Promise Completion = new();
			public void SetCommand(string command) { }
			public Promise Run() => Completion;
			public void Cancel() => throw new InvalidOperationException("Unexpected cancellation");
			public IBeamCommand On<T>(string type, Action<ReportDataPoint<T>> callback) => this;
			public IBeamCommand On(Action<ReportDataPointDescription> callback) => this;
			public IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> callback) => this;
			public IBeamCommand OnTerminate(Action<ReportDataPoint<EofOutput>> callback) => this;
		}
	}

	public class SaveRecoveryContent : ContentObject
	{
		public int amount;
		public List<SaveRecoveryRow> rows = new();
	}
	[Serializable]
	public class SaveRecoveryRow
	{
		[MustBePositive] public int amount;
	}
}
