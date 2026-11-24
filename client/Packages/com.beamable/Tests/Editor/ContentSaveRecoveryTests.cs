using System;
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
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Common.Content.Validation;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Content.UI;
using Beamable.Editor.ContentService;
using Beamable.Editor.UI.ContentWindow;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

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

		[TestCase(false, false, null)]
		[TestCase(false, true, null)]
		[TestCase(true, true, null)]
		[TestCase(false, false, "selection")]
		[TestCase(false, true, "scope")]
		public void Create_or_duplicate_selects_the_watchers_editable_item(bool duplicate, bool watcherFirst, string interrupt)
		{
			WithWatcher(() =>
			{
				const string type = "issue4866Old";
				var reflection = (ContentTypeReflectionCache)Get(_service, "_contentTypeReflectionCache");
				((Dictionary<string, Type>)Get(reflection, "_contentTypeToClass"))[type] = typeof(Issue4866OldContent);
				var windowType = typeof(ContentWindow);
				var window = (ContentWindow)FormatterServices.GetUninitializedObject(windowType);
				Set(window, "_contentService", _service);
				Set(window, "_contentTypeReflectionCache", reflection);
				Set(window, "_itemsExpandedStates", new Dictionary<string, bool>());
				var updateSelection = windowType.GetMethod("SelectCreatedItemWhenReady", Fields);
				var source = ScriptableObject.CreateInstance<Issue4866OldContent>();
				var previousSelection = Selection.objects;
				ContentObject canonical = null;
				var createdPath = _path + "-created";
				try
				{
					Selection.activeObject = _content;
					source.SetContentName("source");
					source.damage = 17;
					var entry = _service.EntriesCache[_content.Id];
					entry.FullId = source.Id;
					entry.TypeName = type;
					_service.EntriesCache[source.Id] = entry;
					((Dictionary<string, ContentObject>)Get(_service, "_contentScriptableCache"))[source.Id] = source;
					if (duplicate) windowType.GetMethod("DuplicateContent", Fields).Invoke(window, new object[] { entry });
					else windowType.GetMethod("CreateNewItem", Fields).Invoke(window, new object[] { type });

					var id = _commands.PropertyArgs.Single().contentIds.Single();
					Assert.That(_service.EntriesCache.ContainsKey(id), Is.False, "The new item has no watcher entry yet.");
					updateSelection.Invoke(window, null);
					Assert.That(Selection.activeObject, Is.SameAs(_content));

					void WriteSavedFile(int command) => File.WriteAllText(createdPath,
						"{\"properties\":" + _commands.PropertyArgs[command].contentProperties[0] + ",\"tags\":[\"old\"]}");
					void ReportCreation()
					{
						entry.FullId = id;
						entry.JsonFilePath = createdPath;
						entry.CurrentStatus = (int)ContentStatus.Created;
						_service.EntriesCache[id] = entry;
						typeof(CliContentService).GetMethod("CacheScriptableContent", Fields).Invoke(_service, new object[] { entry });
						Assert.That(_service.TryGetContentObject(id, out canonical), Is.True);
					}
					WriteSavedFile(0);
					if (watcherFirst) ReportCreation();
					if (interrupt == "selection") Selection.activeObject = source;
					if (interrupt == "scope") _service.manifestIdOverride = "other";
					updateSelection.Invoke(window, null);
					_commands.Properties[0].Completion.CompleteSuccess();
					foreach (var command in _commands.Tags.ToArray()) command.Completion.CompleteSuccess();
					if (!watcherFirst) ReportCreation();
					updateSelection.Invoke(window, null);
					if (interrupt != null)
					{
						Assert.That(Selection.activeObject, Is.SameAs(interrupt == "selection" ? (ContentObject)source : _content));
						return;
					}

					Assert.That(Selection.activeObject, Is.SameAs(canonical));
					var selected = (Issue4866OldContent)canonical;
					Assert.That(selected.damage, Is.EqualTo(duplicate ? 17 : 0));
					selected.damage = 41;
					selected.OnEditorChanged();
					Assert.That(_commands.PropertyArgs[1].originalFiles, Is.Not.Empty, "The first edit uses the loaded file baseline.");
					WriteSavedFile(1);
					_commands.Properties[1].Completion.CompleteSuccess();
					Assert.That(_service.TryGetSaveFailure(id, out _), Is.False);
				}
				finally
				{
					Selection.objects = previousSelection;
					source.CancelPendingEditorChangeNotification();
					UnityEngine.Object.DestroyImmediate(source);
					if (canonical != null)
					{
						canonical.CancelPendingEditorChangeNotification();
						UnityEngine.Object.DestroyImmediate(canonical);
					}
					if (File.Exists(createdPath)) File.Delete(createdPath);
				}
			});
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
			config.validateSchemaDifference = true;
			_service.UpdateSchemaDifference(_service.EntriesCache[_content.Id], SchemaDifference.UnknownFieldsInJson);
			var revision = _service.ManifestChangedCount;
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
		public void A_new_draft_can_reuse_a_deleted_local_items_id()
		{
			WithWatcher(() =>
			{
				LoadWithAmount(42);
				var id = _content.Id;
				var entry = _service.EntriesCache[id];
				entry.CurrentStatus = (int)ContentStatus.Created;
				_service.EntriesCache[id] = entry;
				_service.DeleteContent(id).GetResult();
				var draft = ScriptableObject.CreateInstance<SaveRecoveryContent>();
				try
				{
					draft.SetIdAndVersion(id, "");
					var create = _service.SaveNewContent(draft);
					Assert.That(_commands.PropertyArgs.Single().originalFiles, Is.Null);
					_commands.Properties[0].Completion.CompleteSuccess();
					foreach (var tag in _commands.Tags.ToArray()) tag.Completion.CompleteSuccess();
					create.GetResult();
				}
				finally
				{
					draft.CancelPendingEditorChangeNotification();
					UnityEngine.Object.DestroyImmediate(draft);
				}
			});
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
        [TestCase(false, SchemaDifference.MissingFieldsInJson)]
        [TestCase(false, SchemaDifference.UnknownFieldsInJson | SchemaDifference.MissingFieldsInJson)]
		public void Schema_notice_tracks_current_file_without_normalization(bool enabled, SchemaDifference difference)
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
				Assert.That(_commands.Properties, Is.Empty);
				Assert.That(File.ReadAllText(_path), Is.EqualTo(json), "The controlled save command must not write the test file.");
				// An external schema alignment clears the notice, even for modified content.
				File.WriteAllText(_path, "{\"properties\":" + properties + ",\"tags\":[]}");
				entry.CurrentStatus = (int)ContentStatus.Modified;
				loader.Invoke(_service, new object[] { entry, _content });
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.None));
				entry.CurrentStatus = (int)ContentStatus.UpToDate;
				loader.Invoke(_service, new object[] { entry, _content });
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.None));
			}
			finally { UnityEngine.Object.DestroyImmediate(config); }
		}

		[Test]
		public void Schema_notice_survives_cache_reload_without_writing_the_file()
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
				Assert.That(_commands.Properties, Is.Empty);
				var original = File.ReadAllText(_path);
				entry.CurrentStatus = (int)ContentStatus.Modified;
				typeof(CliContentService).GetMethod("ClearCaches", Fields).Invoke(_service, null);
				loader.Invoke(_service, new object[] { entry, _content });
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.UnknownFieldsInJson));
                Assert.That(File.ReadAllText(_path), Is.EqualTo(original));
                Assert.That(_commands.Properties, Is.Empty);
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
				Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.None));
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

        [TestCase(false)]
        [TestCase(true)]
        public void Loaded_content_edits_send_lossless_baselines_regardless_of_notice_setting(bool enabled)
        {
            PrepareFile();
            var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
            try
            {
                config.validateSchemaDifference = enabled; Set(_service, "_contentConfiguration", config);
                var original = "{\"properties\":{\"amount\":{\"data\":2},\"future\":{\"data\":37}},\"tags\":[\"old\"]}";
                File.WriteAllText(_path, original);
                typeof(CliContentService).GetMethod("LoadContentObject", Fields).Invoke(_service, new object[] { _service.EntriesCache[_content.Id], _content });
                Save().GetResult(); // A delayed validation callback with no edit is harmless.
                Assert.That(_commands.Properties, Is.Empty);
                Assert.That(_commands.Tags, Is.Empty);
                _content.amount = 3;
                Save();
                var args = _commands.PropertyArgs.Single();
                Assert.That(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(args.originalFiles.Single())), Is.EqualTo(original));
                Assert.That(args.modelProperties.Single(), Does.Contain("2"));
                Assert.That(args.contentProperties.Single(), Does.Contain("3"));
                Assert.That(args.expectedFileHashes, Is.Null);
                Assert.That(File.ReadAllText(_path), Is.EqualTo(original));
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public void Loaded_tag_only_edit_does_not_submit_properties()
        {
            PrepareFile();
            var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
            try
            {
                Set(_service, "_contentConfiguration", config);
                File.WriteAllText(_path, ClientContentSerializer.SerializeContent(_content));
                typeof(CliContentService).GetMethod("LoadContentObject", Fields).Invoke(_service, new object[] { _service.EntriesCache[_content.Id], _content });
                _content.Tags = new[] { "changed" };
                Save();
                Assert.That(_commands.Properties, Is.Empty);
                Assert.That(_commands.TagValues, Is.EqualTo(new[] { "changed" }));
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [TestCase("file")]
        [TestCase("scope")]
        [TestCase("pending")]
        public void Migration_preview_is_read_only_and_rejects_stale_or_pending_changes(string change)
        {
            PrepareFile();
            var original = ClientContentSerializer.SerializeContent(_content).Replace("\"properties\":{", "\"properties\":{\"future\":{\"data\":37},");
            File.WriteAllText(_path, original);
            var preview = _service.PreviewSchemaMigration(_content.Id);
            Assert.That(preview.OriginalJson, Is.EqualTo(original));
            Assert.That(preview.ProposedProperties, Does.Not.Contain("future"));
            Assert.That(_commands.Properties, Is.Empty);
            Assert.That(File.ReadAllText(_path), Is.EqualTo(original), "Closing/cancelling a preview requires no write.");
            if (change == "file") File.AppendAllText(_path, " ");
            if (change == "scope") _service.manifestIdOverride = "other";
            if (change == "pending") { _content.amount++; _content.CheckForNonDetectedChanges(); }
            Exception error = null;
            _service.ApplySchemaMigration(preview).Error(e => error = e);
            Assert.That(error, Is.Not.Null);
            Assert.That(_commands.Properties, Is.Empty);
        }

        [Test]
        public void Migration_submits_only_reviewed_item_and_hash_without_publishing()
        {
            PrepareFile();
            var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
            try
            {
                Set(_service, "_contentConfiguration", config);
                var original = ClientContentSerializer.SerializeContent(_content).Replace("\"properties\":{", "\"properties\":{\"future\":{\"data\":37},");
                File.WriteAllText(_path, original);
                var preview = _service.PreviewSchemaMigration(_content.Id);
                var completion = _service.ApplySchemaMigration(preview);
                var args = _commands.PropertyArgs.Single();
                Assert.That(args.contentIds, Is.EqualTo(new[] { _content.Id }));
                Assert.That(args.contentProperties.Single(), Is.EqualTo(preview.ProposedProperties));
                Assert.That(args.expectedFileHashes.Single(), Has.Length.EqualTo(64));
                Assert.That(args.originalFiles, Is.Null);
                File.WriteAllText(_path, "{\"properties\":" + preview.ProposedProperties + ",\"tags\":[\"old\"]}");
                _commands.Properties.Single().Completion.CompleteSuccess();
                completion.GetResult();
                Assert.That(_commands.Tags, Is.Empty);
                Assert.That(_service.GetSchemaDifference(_content.Id), Is.EqualTo(SchemaDifference.None));
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public void Migration_blocks_Inspector_edits_and_reports_one_that_slips_through()
        {
            WithWatcher(() =>
            {
                LoadWithAmount(2);
                File.WriteAllText(_path, File.ReadAllText(_path).Replace("\"properties\":{", "\"properties\":{\"future\":{\"data\":37},"));
                var completion = _service.ApplySchemaMigration(_service.PreviewSchemaMigration(_content.Id));
                Assert.That(_service.CurrentProgress.IsActive, Is.True, "The Inspector fields are disabled while JSON changes are applied.");
                // An edit that does not come through the disabled fields, such as Undo or a script.
                _content.amount = 4;
                _content.CheckForNonDetectedChanges();
                Assert.That(_content.HasPendingEditorChangeNotification, Is.True);
                WriteAsCli(0);
                LogAssert.Expect(LogType.Error, new Regex("Content edit was not saved"));
                _commands.Properties.Single().Completion.CompleteSuccess();
                completion.GetResult();
                Assert.That(File.ReadAllText(_path), Does.Not.Contain("\"future\""));
                Assert.That(_content.amount, Is.EqualTo(2), "The Inspector shows the migrated file.");
                Assert.That(_service.TryGetSaveFailure(_content.Id, out var failure), Is.True, "The discarded edit must be reported.");
                Assert.That(failure.Message, Does.Contain("JSON changes were applied"));
                Assert.That(_service.CurrentProgress.IsActive, Is.False);
            });
        }

        [Test]
        public void Schema_comparison_uses_serializer_payload_and_shows_preserved_metadata_separately()
        {
            PrepareFile();
            var original = ClientContentSerializer.SerializeContent(_content)
                .Replace("\"properties\":{", "\"properties\":{\"future\":{\"data\":37},");
            File.WriteAllText(_path, original);
            var preview = _service.PreviewSchemaMigration(_content.Id);
            var comparison = new ContentSchemaPreview(preview.OriginalJson, preview.ProposedProperties);
            var removed = comparison.Rows.Single(r => r.Path == "/future");
            Assert.That(removed.Kind, Is.EqualTo(ContentSchemaPreview.Change.Removed));
            Assert.That(removed.Before, Is.EqualTo("37"));
            Assert.That(removed.After, Is.EqualTo("Removed"));
            Assert.That(comparison.Rows.Single(r => r.Path == "/amount").Kind, Is.EqualTo(ContentSchemaPreview.Change.Unchanged));
            Assert.That(comparison.CurrentJson, Does.Contain("future"));
            Assert.That(comparison.ProposedJson, Does.Not.Contain("future"));
            Assert.That(File.ReadAllText(_path), Is.EqualTo(original));
            Assert.That(_commands.Properties, Is.Empty);
        }

        [Test]
        public void Schema_review_loads_an_unselected_item_without_writing()
        {
            PrepareFile();
            var original = ClientContentSerializer.SerializeContent(_content);
            File.WriteAllText(_path, original);
            var entry = _service.EntriesCache[_content.Id];
            entry.TypeName = "saveRecovery";
            _service.EntriesCache[_content.Id] = entry;
            var cache = (Dictionary<string, ContentObject>)Get(_service, "_contentScriptableCache");
            cache.Clear();
            var reflection = new ContentTypeReflectionCache();
            reflection.OnSetupForCacheGeneration();
            ((Dictionary<string, Type>)Get(reflection, "_contentTypeToClass"))["saveRecovery"] = typeof(SaveRecoveryContent);
            Set(_service, "_contentTypeReflectionCache", reflection);
            var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
            Set(_service, "_contentConfiguration", config);
            try
            {
                var preview = _service.PreviewSchemaMigration(_content.Id);
                Assert.That(preview.OriginalJson, Is.EqualTo(original));
                Assert.That(File.ReadAllText(_path), Is.EqualTo(original));
                Assert.That(_commands.Properties, Is.Empty);
                Assert.That(cache.ContainsKey(_content.Id), Is.True);
            }
            finally
            {
                if (cache.TryGetValue(_content.Id, out var loaded)) UnityEngine.Object.DestroyImmediate(loaded);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Schema_popup_cancel_does_not_write_and_failed_apply_requires_new_review()
        {
            PrepareFile();
            var original = ClientContentSerializer.SerializeContent(_content)
                .Replace("\"properties\":{", "\"properties\":{\"future\":{\"data\":37},");
            File.WriteAllText(_path, original);
            var window = ContentSchemaWindow.Open(_service, _content.Id);
            window.Close();
            Assert.That(_commands.Properties, Is.Empty);
            Assert.That(File.ReadAllText(_path), Is.EqualTo(original));
            window = ContentSchemaWindow.Open(_service, _content.Id);
            try
            {
                File.AppendAllText(_path, " ");
                typeof(ContentSchemaWindow).GetMethod("Apply", Fields).Invoke(window, null);
                Assert.That(Get(window, "_preview"), Is.Null);
                Assert.That(Get(window, "_error"), Is.Not.Null);
                Assert.That(_commands.Properties, Is.Empty);
            }
            finally { window.Close(); }
        }

        [Test]
        public void Schema_popup_applies_once_and_closes_only_after_write_completes()
        {
            PrepareFile();
            var original = ClientContentSerializer.SerializeContent(_content)
                .Replace("\"properties\":{", "\"properties\":{\"future\":{\"data\":37},");
            File.WriteAllText(_path, original);
            var window = ContentSchemaWindow.Open(_service, _content.Id);
            try
            {
                var preview = (CliContentService.SchemaMigrationPreview)Get(window, "_preview");
                var apply = typeof(ContentSchemaWindow).GetMethod("Apply", Fields);
                apply.Invoke(window, null);
                apply.Invoke(window, null);
                Assert.That(_commands.Properties.Count, Is.EqualTo(1));
                Assert.That(window != null, Is.True);
                Assert.That((bool)Get(window, "_applying"), Is.True);
                File.WriteAllText(_path, "{\"properties\":" + preview.ProposedProperties + ",\"tags\":[\"old\"]}");
                _commands.Properties.Single().Completion.CompleteSuccess();
                Assert.That(window == null, Is.True);
                Assert.That(_commands.Tags, Is.Empty);
            }
            finally { if (window != null) window.Close(); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Schema_popup_reports_a_refused_apply_even_after_close(bool closeBeforeRefusal)
        {
            PrepareFile();
            var original = ClientContentSerializer.SerializeContent(_content)
                .Replace("\"properties\":{", "\"properties\":{\"future\":{\"data\":37},");
            File.WriteAllText(_path, original);
            var window = ContentSchemaWindow.Open(_service, _content.Id);
            try
            {
                typeof(ContentSchemaWindow).GetMethod("Apply", Fields).Invoke(window, null);
                var command = _commands.Properties.Single();
                if (closeBeforeRefusal)
                {
                    window.Close();
                    Assert.That(window == null, Is.True);
                    LogAssert.Expect(LogType.Error, $"Could not apply JSON changes to '{_content.Id}'. See the Beam CLI command history for details.");
                }

                var outside = original.Replace("\"future\":{\"data\":37}", "\"future\":{\"data\":38}");
                File.WriteAllText(_path, outside);
                const string refusal = "Content changed after migration preview. Review the item again.";
                command.Reject(refusal, 1);
                if (!closeBeforeRefusal)
                {
                    Assert.That(window != null, Is.True);
                    Assert.That(Get(window, "_error"), Is.EqualTo(refusal));
                    Assert.That(Get(window, "_preview"), Is.Null, "A refused preview must be reviewed again.");
                }
                Assert.That(File.ReadAllText(_path), Is.EqualTo(outside));
            }
            finally { if (window != null) window.Close(); }
        }

        [TestCase(720, 500, 0)]
        [TestCase(720, 500, 1)]
        [TestCase(1000, 700, 0)]
        [TestCase(1000, 700, 1)]
        public void Schema_popup_tabs_layout_with_actions_inside_window(int width, int height, int tab)
        {
            PrepareFile();
            var window = ContentSchemaWindow.Open(_service, _content.Id);
            var previousEvent = Event.current;
            const BindingFlags statics = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            try
            {
                window.position = new Rect(0, 0, width, height);
                Set(window, "_tab", tab);
                foreach (var eventType in new[] { EventType.Layout, EventType.Repaint })
                {
                    typeof(GUIUtility).GetMethod("BeginContainerFromOwner", statics).Invoke(null, new object[] { window });
                    try
                    {
                        Event.current = new Event { type = eventType };
                        typeof(GUIUtility).GetMethod("BeginGUI", statics).Invoke(null, new object[] { 1, window.GetInstanceID(), 1 });
                        typeof(ContentSchemaWindow).GetMethod("OnGUI", Fields).Invoke(window, null);
                        if (eventType == EventType.Layout)
                            typeof(GUILayoutUtility).GetMethod("LayoutFromContainer", statics).Invoke(null, new object[] { (float)width, (float)height });
                        else Assert.That(GUILayoutUtility.GetLastRect().yMax, Is.LessThanOrEqualTo(height));
                    }
                    finally { typeof(GUIUtility).GetMethod("EndContainer", statics).Invoke(null, null); }
                }
                Assert.That(_commands.Properties, Is.Empty);
            }
            finally { Event.current = previousEvent; window.Close(); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Reload_missing_field_uses_defaults_and_stale_objects_cannot_save(bool changeScope)
        {
            PrepareFile();
            var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
            try
            {
                Set(_service, "_contentConfiguration", config);
                _content.amount = 99;
                File.WriteAllText(_path, "{\"properties\":{},\"tags\":[]}");
                typeof(CliContentService).GetMethod("LoadContentObject", Fields).Invoke(_service, new object[] { _service.EntriesCache[_content.Id], _content });
                Assert.That(_content.amount, Is.Zero);
                Assert.That(_content.HasPendingEditorChangeNotification, Is.False);
                if (changeScope) _service.manifestIdOverride = "other";
                else typeof(CliContentService).GetMethod("ClearCaches", Fields).Invoke(_service, null);
                _content.amount = 5;
                Exception error = null;
                Save().Error(e => error = e);
                Assert.That(error, Is.Not.Null);
                Assert.That(_commands.Properties, Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [TestCase("[]")]
        [TestCase("{\"properties\":{\"amount\":{\"data\":99},\"rows\":{\"$links\":\"invalid\"}}}")]
        public void Failed_reload_preserves_values_and_repairing_the_same_file_restores_editing(string brokenJson)
        {
            WithWatcher(() =>
            {
                LoadWithAmount(42);
                var goodJson = File.ReadAllText(_path);
                var id = _content.Id;
                File.WriteAllText(_path, brokenJson);
                LogAssert.Expect(LogType.Error, new Regex("Could not load content from json file"));
                Assert.Throws<TargetInvocationException>(Watch);
                Assert.That(_content.amount, Is.EqualTo(42));
                Assert.That(_content.OnEditorChanged, Is.Null);
                Assert.That(_service.TryGetLoadError(id, out var error), Is.True);
                Assert.That(error, Does.Contain(id));
                Exception refused = null;
                Save().Error(ex => refused = ex);
                Assert.That(refused?.Message, Is.EqualTo(error));
                var editor = UnityEditor.Editor.CreateEditor(_content, typeof(ContentObjectEditor));
                try
                {
                    typeof(ContentObjectEditor).GetMethod("ObservePendingEdits", Fields).Invoke(editor, new object[] { _service });
                    Set(editor, "_pendingNameEdit", (Action)(() => Assert.Fail("A delayed rename ran after a failed reload.")));
                    Set(editor, "_pendingTagEdit", (Action)(() => Assert.Fail("A delayed tag edit ran after a failed reload.")));
                    var complete = typeof(ContentObjectEditor).GetMethod("CompleteHeaderEdit", Fields);
                    complete.Invoke(editor, new object[] { true });
                    complete.Invoke(editor, new object[] { false });
                }
                finally { UnityEngine.Object.DestroyImmediate(editor); }
                Assert.That(_commands.Properties, Is.Empty);
                Assert.That(File.ReadAllText(_path), Is.EqualTo(brokenJson));

                File.WriteAllText(_path, goodJson);
                Watch();
                Assert.That(_service.TryGetLoadError(id, out _), Is.False);
                Assert.That(_content.HasPendingEditorChangeNotification, Is.False);
                _content.amount = 43;
                _content.OnEditorChanged();
                Assert.That(_commands.Properties.Count, Is.EqualTo(1));
                WriteAsCli(0);
                _commands.Properties[0].Completion.CompleteSuccess();
                Assert.That(_content.amount, Is.EqualTo(43));
                Assert.That(_service.TryGetSaveFailure(id, out _), Is.False);
            });
        }

        [TestCase(4)]
        [TestCase(2)]
        public void Overlapping_loaded_edits_use_the_last_completed_baseline(int lastValue)
        {
            PrepareFile();
            var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
            try
            {
                Set(_service, "_contentConfiguration", config);
                _content.amount = 2;
                File.WriteAllText(_path, ClientContentSerializer.SerializeContent(_content));
                typeof(CliContentService).GetMethod("LoadContentObject", Fields).Invoke(_service, new object[] { _service.EntriesCache[_content.Id], _content });
                _content.amount = 3;
                var first = Save();
                _content.amount = lastValue;
                var second = Save();
                Assert.That(_commands.Properties.Count, Is.EqualTo(1));
                File.WriteAllText(_path, "{\"properties\":" + _commands.PropertyArgs[0].contentProperties[0] + ",\"tags\":[\"old\"]}");
                _commands.Properties[0].Completion.CompleteSuccess();
                first.GetResult();
                Assert.That(_commands.Properties.Count, Is.EqualTo(2));
                Assert.That(_commands.PropertyArgs[1].modelProperties[0], Is.EqualTo(_commands.PropertyArgs[0].contentProperties[0]));
                Assert.That(_commands.PropertyArgs[1].contentProperties[0], Is.EqualTo(ClientContentSerializer.SerializeProperties(_content)));
                File.WriteAllText(_path, "{\"properties\":" + _commands.PropertyArgs[1].contentProperties[0] + ",\"tags\":[\"old\"]}");
                _commands.Properties[1].Completion.CompleteSuccess();
                second.GetResult();
                Assert.That(_commands.Tags, Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Reload_rejects_queued_edits_even_when_it_reuses_the_same_object(bool firstSaveFails)
        {
            PrepareFile();
            var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
            try
            {
                Set(_service, "_contentConfiguration", config);
                var loader = typeof(CliContentService).GetMethod("LoadContentObject", Fields);
                var entry = _service.EntriesCache[_content.Id];
                _content.amount = 2;
                File.WriteAllText(_path, ClientContentSerializer.SerializeContent(_content));
                loader.Invoke(_service, new object[] { entry, _content });
                _content.amount = 3;
                Exception firstError = null, queuedError = null;
                Save().Error(error => firstError = error);
                _content.amount = 4;
                Save().Error(error => queuedError = error);
                Assert.That(_commands.Properties.Count, Is.EqualTo(1));

                // An external write and watcher reload arrive before the first CLI completion.
                _content.amount = 99;
                var externalJson = ClientContentSerializer.SerializeContent(_content);
                File.WriteAllText(_path, externalJson);
                loader.Invoke(_service, new object[] { entry, _content });
                if (firstSaveFails) _commands.Properties[0].Completion.CompleteError(new InvalidOperationException("Save failed"));
                else _commands.Properties[0].Completion.CompleteSuccess();

                Assert.That(firstError != null, Is.EqualTo(firstSaveFails));
                Assert.That(queuedError, Is.TypeOf<InvalidOperationException>());
                Assert.That(queuedError.Message, Does.Contain("Content was reloaded"));
                Assert.That(_commands.Properties.Count, Is.EqualTo(1), "Do not rebase the queued 2->4 edit onto 99.");
                Assert.That(_commands.Tags, Is.Empty);
                Assert.That(File.ReadAllText(_path), Is.EqualTo(externalJson));
                Assert.That(_content.amount, Is.EqualTo(99));

                // A fresh edit after the reload must still work and use the new baseline.
                _content.amount = 100;
                var fresh = Save();
                Assert.That(_commands.Properties.Count, Is.EqualTo(2));
                Assert.That(_commands.PropertyArgs[1].modelProperties[0], Does.Contain("99"));
                File.WriteAllText(_path, "{\"properties\":" + _commands.PropertyArgs[1].contentProperties[0] + ",\"tags\":[\"old\"]}");
                _commands.Properties[1].Completion.CompleteSuccess();
                fresh.GetResult();
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public void Failed_loaded_tag_edit_is_retried_and_successful_repeat_is_a_no_op()
        {
            PrepareFile();
            var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
            try
            {
                Set(_service, "_contentConfiguration", config);
                File.WriteAllText(_path, ClientContentSerializer.SerializeContent(_content));
                typeof(CliContentService).GetMethod("LoadContentObject", Fields).Invoke(_service, new object[] { _service.EntriesCache[_content.Id], _content });
                _content.Tags = new[] { "changed" };
                Exception failure = null;
                Save().Error(e => failure = e);
                _commands.Tags[0].Completion.CompleteError(new InvalidOperationException("Tag write failed"));
                Assert.That(failure, Is.Not.Null);
                var retry = Save();
                Assert.That(_commands.Tags.Count, Is.EqualTo(2));
                _commands.Tags[1].Completion.CompleteSuccess();
                retry.GetResult();
                Save().GetResult();
                Assert.That(_commands.Tags.Count, Is.EqualTo(2));
                Assert.That(_commands.Properties, Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [TestCase(CliContentService.UnsafeEditExitCode, true)]
        [TestCase(CliContentService.StaleEditExitCode, false)]
        public void Refused_edit_stays_visible_until_a_later_save_succeeds(int exitCode, bool canReview)
        {
            PrepareFile();
            var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
            try
            {
                // Notices are hidden, yet a refusal for unsupported data must still offer the review.
                config.validateSchemaDifference = false; Set(_service, "_contentConfiguration", config);
                LoadWithAmount(2);
                _content.amount = 3;
                LogAssert.Expect(LogType.Error, new Regex("Content edit was not saved"));
                _service.SaveContent(_content);
                _commands.Properties[0].Reject("Cannot safely edit properties.rows", exitCode);
                Assert.That(_service.TryGetSaveFailure(_content.Id, out var failure), Is.True);
                Assert.That(failure.CanReviewSchema, Is.EqualTo(canReview));
                Assert.That(failure.Message, Does.Contain("Cannot safely edit"));
                Assert.That(_content.amount, Is.EqualTo(3), "Keep the refused value until the user discards it.");

                _content.amount = 4;
                _service.SaveContent(_content);
                _commands.Properties[1].Completion.CompleteSuccess();
                Assert.That(_service.TryGetSaveFailure(_content.Id, out _), Is.False, "A later successful save clears the failure.");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public void Discarding_a_refused_edit_reloads_the_file_without_writing()
        {
            PrepareFile();
            var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
            try
            {
                Set(_service, "_contentConfiguration", config);
                LoadWithAmount(2);
                _content.amount = 3;
                LogAssert.Expect(LogType.Error, new Regex("Content edit was not saved"));
                _service.SaveContent(_content);
                _commands.Properties[0].Reject("Cannot safely edit properties.rows", CliContentService.UnsafeEditExitCode);
                Assert.That(_service.TryGetSaveFailure(_content.Id, out var failure), Is.True);
                _service.DiscardRejectedEdit(_content.Id, failure);
                Assert.That(_content.amount, Is.EqualTo(2));
                Assert.That(_service.TryGetSaveFailure(_content.Id, out _), Is.False);
                Assert.That(_commands.Properties.Count, Is.EqualTo(1), "Discarding writes nothing.");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Delayed_discard_during_a_tag_write_reports_busy_only_for_the_displayed_refusal(bool reloaded)
        {
            WithWatcher(() =>
            {
                LoadWithAmount(2);
                var id = _content.Id;
                _content.amount = 3;
                LogAssert.Expect(LogType.Error, new Regex("Content edit was not saved"));
                _service.SaveContent(_content);
                _commands.Properties.Single().Reject("Cannot safely edit properties.rows", CliContentService.UnsafeEditExitCode);
                Assert.That(_service.TryGetSaveFailure(id, out var clicked), Is.True);
                Assert.That(_service.CanDiscardEdit(id, clicked), Is.True);

                // The Inspector already scheduled the click. A reload can retire it before the tag write starts.
                if (reloaded)
                {
                    WriteOutsideAmount(50);
                    Watch();
                }
                var disk = File.ReadAllText(_path);
                _service.SetContentTags(id, new[] { "changed" });
                Assert.That(_service.CanDiscardEdit(id, clicked), Is.False);
                if (reloaded)
                    Assert.DoesNotThrow(() => _service.DiscardRejectedEdit(id, clicked));
                else
                {
                    var busy = Assert.Throws<InvalidOperationException>(() => _service.DiscardRejectedEdit(id, clicked));
                    Assert.That(busy.Message, Does.Contain("Wait for them to finish, then discard"));
                }
                Assert.That(_content.amount, Is.EqualTo(reloaded ? 50 : 3));
                Assert.That(File.ReadAllText(_path), Is.EqualTo(disk));
                Assert.That(_service.TryGetSaveFailure(id, out var retained), Is.True);
                Assert.That(retained.Edit, Is.EqualTo(clicked.Edit));

                _commands.Tags.Single().Completion.CompleteSuccess();
                Assert.That(_service.CanDiscardEdit(id, clicked), Is.EqualTo(!reloaded));
                _service.DiscardRejectedEdit(id, clicked);
                Assert.That(_content.amount, Is.EqualTo(reloaded ? 50 : 2));
                Assert.That(_service.TryGetSaveFailure(id, out _), Is.EqualTo(reloaded));
                Assert.That(File.ReadAllText(_path), Is.EqualTo(disk));
                Assert.That(_commands.Properties.Count, Is.EqualTo(1));
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Reload_replaces_the_refused_value_and_retires_only_its_discard_action(bool fullRefresh)
        {
            WithWatcher(() =>
            {
                LoadWithAmount(2);
                var id = _content.Id;
                var entry = _service.EntriesCache[id];
                var original = File.ReadAllText(_path);
                _content.amount = 3;
                LogAssert.Expect(LogType.Error, new Regex("Content edit was not saved"));
                _service.SaveContent(_content);
                _commands.Properties.Single().Reject("Cannot safely edit properties.rows", CliContentService.UnsafeEditExitCode);
                Assert.That(_service.TryGetSaveFailure(id, out var refused), Is.True);
                Assert.That(_service.CanDiscardEdit(id, refused), Is.True);
                Assert.That(_content.amount, Is.EqualTo(3));

                // A failed discard leaves the refused value on screen and the action available for a retry.
                File.WriteAllText(_path, "[]");
                LogAssert.Expect(LogType.Error, new Regex("Could not load content from json file"));
                Assert.Catch(() => _service.DiscardRejectedEdit(id, refused));
                Assert.That(_content.amount, Is.EqualTo(3));
                Assert.That(_service.CanDiscardEdit(id, refused), Is.True);
                File.WriteAllText(_path, original);

                if (fullRefresh)
                {
                    typeof(CliContentService).GetMethod("ClearCaches", Fields).Invoke(_service, null);
                    _service.EntriesCache[id] = entry;
                }
                var expected = fullRefresh ? original : WriteOutsideAmount(50);
                Watch();
                Assert.That(_content.amount, Is.EqualTo(fullRefresh ? 2 : 50));
                Assert.That(_service.TryGetSaveFailure(id, out var historical), Is.True);
                Assert.That(historical.Message, Is.EqualTo(refused.Message));
                Assert.That(_service.CanDiscardEdit(id, historical), Is.False);

                _content.amount = 77;
                _content.CheckForNonDetectedChanges();
                _service.DiscardRejectedEdit(id, refused);
                Assert.That(_content.amount, Is.EqualTo(77));
                Assert.That(_content.HasPendingEditorChangeNotification, Is.True);
                Assert.That(File.ReadAllText(_path), Is.EqualTo(expected));
                Assert.That(_commands.Properties.Count, Is.EqualTo(1));
            });
        }

        [Test]
        public void Completed_migration_clears_only_its_original_realm_failure_after_a_switch()
        {
            WithWatcher(() =>
            {
                var cli = ((BeamEditorContext)Get(_service, "_beamContext")).BeamCli;
                cli.pidToRealm["realm-b"] = new RealmView { Cid = "test", Pid = "realm-b" };
                var id = _content.Id;
                var entryA = _service.EntriesCache[id];
                var entryB = entryA;
                entryB.JsonFilePath = Path.GetTempFileName();
                var itemB = ScriptableObject.CreateInstance<SaveRecoveryContent>();
                try
                {
                    void SelectRealm(string pid, LocalContentManifestEntry entry, SaveRecoveryContent item)
                    {
                        cli.latestConfig.pid = pid;
                        typeof(CliContentService).GetMethod("ClearCaches", Fields).Invoke(_service, null);
                        _service.EntriesCache[id] = entry;
                        ((Dictionary<string, ContentObject>)Get(_service, "_contentScriptableCache"))[id] = item;
                        Watch();
                    }

                    void RefuseEdit(SaveRecoveryContent item, string message)
                    {
                        item.amount++;
                        LogAssert.Expect(LogType.Error, new Regex("Content edit was not saved"));
                        _service.SaveContent(item);
                        _commands.Properties.Last().Reject(message, CliContentService.UnsafeEditExitCode);
                        Assert.That(_service.TryGetSaveFailure(id, out var refusal), Is.True);
                        Assert.That(_service.CanDiscardEdit(id, refusal), Is.True);
                    }

                    const string jsonB = "{\"properties\":{\"amount\":{\"data\":10},\"rows\":{\"data\":[]}},\"tags\":[\"old\"]}";
                    File.WriteAllText(entryB.JsonFilePath, jsonB);
                    SelectRealm("realm-b", entryB, itemB);
                    RefuseEdit(itemB, "Realm B refusal");

                    const string jsonA = "{\"properties\":{\"amount\":{\"data\":2},\"rows\":{\"data\":[]},\"future\":{\"data\":37}},\"tags\":[\"old\"]}";
                    File.WriteAllText(_path, jsonA);
                    SelectRealm("realm", entryA, _content);
                    RefuseEdit(_content, "Realm A refusal");
                    var completion = _service.ApplySchemaMigration(_service.PreviewSchemaMigration(id));
                    Assert.That(_commands.Properties.Count, Is.EqualTo(3));
                    SelectRealm("realm-b", entryB, itemB);
                    Assert.That(_service.TryGetSaveFailure(id, out var before), Is.True);
                    Assert.That(before.Message, Is.EqualTo("Realm B refusal"));
                    itemB.amount = 20;
                    itemB.CheckForNonDetectedChanges();

                    WriteAsCli(2);
                    _commands.Properties[2].Completion.CompleteSuccess();
                    completion.GetResult();
                    Assert.That(_service.TryGetSaveFailure(id, out var after), Is.True);
                    Assert.That(after.Message, Is.EqualTo(before.Message));
                    Assert.That(itemB.amount, Is.EqualTo(20));
                    Assert.That(itemB.HasPendingEditorChangeNotification, Is.True);
                    Assert.That(File.ReadAllText(entryB.JsonFilePath), Is.EqualTo(jsonB));
                    Assert.That(File.ReadAllText(_path), Does.Not.Contain("\"future\""));

                    SelectRealm("realm", entryA, _content);
                    Assert.That(_service.TryGetSaveFailure(id, out _), Is.False);
                    Assert.That(_content.amount, Is.EqualTo(2));
                    Assert.That(_commands.Properties.Count, Is.EqualTo(3));
                }
                finally
                {
                    itemB.CancelPendingEditorChangeNotification();
                    UnityEngine.Object.DestroyImmediate(itemB);
                    File.Delete(entryB.JsonFilePath);
                }
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Migration_settles_a_watcher_event_with_the_latest_entry(bool refused)
        {
            WithWatcher(() =>
            {
                LoadWithAmount(2);
                var id = _content.Id;
                Exception failure = null;
                _service.ApplySchemaMigration(_service.PreviewSchemaMigration(id)).Error(error => failure = error);
                var entry = _service.EntriesCache[id];
                entry.Tags = new[] { "outside" };
                _service.EntriesCache[id] = entry;
                var amount = refused ? 99 : 2;
                var file = "{\"properties\":{\"amount\":{\"data\":" + amount + "},\"rows\":{\"data\":[]}},\"tags\":[\"outside\"]}";
                File.WriteAllText(_path, file);
                Watch();
                Assert.That(_content.amount, Is.EqualTo(2));
                if (refused) _commands.Properties.Single().Reject("Content changed after migration preview", 1);
                else _commands.Properties.Single().Completion.CompleteSuccess();
                Assert.That(failure != null, Is.EqualTo(refused));
                Assert.That(_content.amount, Is.EqualTo(amount));
                Assert.That(_content.Tags, Is.EqualTo(entry.Tags));
                Assert.That(File.ReadAllText(_path), Is.EqualTo(file));
                Assert.That(_commands.Properties.Count, Is.EqualTo(1));
            });
        }

        [Test]
        public void Migration_counts_as_applied_when_the_reload_after_the_write_fails()
        {
            PrepareFile();
            var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
            try
            {
                Set(_service, "_contentConfiguration", config);
                var original = ClientContentSerializer.SerializeContent(_content).Replace("\"properties\":{", "\"properties\":{\"future\":{\"data\":37},");
                File.WriteAllText(_path, original);
                var completion = _service.ApplySchemaMigration(_service.PreviewSchemaMigration(_content.Id));
                // The write landed, then the file vanished before the reload could read it.
                File.Delete(_path);
                LogAssert.Expect(LogType.Error, new Regex("Could not load content from json file"));
                LogAssert.Expect(LogType.Exception, new Regex("FileNotFoundException"));
                _commands.Properties.Single().Completion.CompleteSuccess();
                Assert.DoesNotThrow(() => completion.GetResult());
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Tag_completion_replays_the_deferred_outside_change(bool refused)
        {
            WithWatcher(() =>
            {
                LoadWithAmount(2);
                _service.SetContentTags(_content.Id, new[] { "changed" });
                var outside = WriteOutsideAmount(99);
                Watch();
                Assert.That(_content.amount, Is.EqualTo(2), "The watcher must wait for the held tag command.");
                if (refused)
                {
                    LogAssert.Expect(LogType.Exception, new Regex("tag failed"));
                    _commands.Tags.Single().Completion.CompleteError(new InvalidOperationException("tag failed"));
                }
                else _commands.Tags.Single().Completion.CompleteSuccess();
                Assert.That(_content.amount, Is.EqualTo(99));
                Assert.That(File.ReadAllText(_path), Is.EqualTo(outside));
                Assert.That(_commands.Properties, Is.Empty);
                Assert.That(_content.HasPendingEditorChangeNotification, Is.False);
            });
        }

        [UnityTest]
        public System.Collections.IEnumerator Tag_echo_keeps_the_debounced_property_edit_and_both_reach_disk()
        {
            var config = SetUpWatcher();
            try
            {
                // CLI tag writes sort the properties, indent the file and preserve unsupported fields.
                const string before = "{\"properties\":{\"rows\":{\"data\":[]},\"future\":{\"data\":12345678901234567890},\"amount\":{\"data\":2}},\"tags\":[\"old\"]}";
                // Serialized with ContentService.GetContentFileSerializationOptions on the parent CLI.
                const string tagged = @"{
  ""properties"": {
    ""amount"": {
      ""data"": 2
    },
    ""future"": {
      ""data"": 12345678901234567890
    },
    ""rows"": {
      ""data"": []
    }
  },
  ""tags"": [
    ""changed""
  ],
  ""referenceManifestId"": ""EmptyManifest""
}";
                File.WriteAllText(_path, before);
                Watch();
                _service.SetContentTags(_content.Id, new[] { "changed" });
                File.WriteAllText(_path, tagged);
                _commands.Tags.Single().Completion.CompleteSuccess();

                _content.amount = 7;
                _content.CheckForNonDetectedChanges();
                Watch();
                Assert.That(_content.amount, Is.EqualTo(7));
                Assert.That(_content.Tags, Is.EqualTo(new[] { "changed" }));
                Assert.That(_content.HasPendingEditorChangeNotification, Is.True);
                Assert.That(_commands.Properties, Is.Empty);

                var timeout = EditorApplication.timeSinceStartup + 5;
                while (_commands.Properties.Count == 0 && EditorApplication.timeSinceStartup < timeout) yield return null;
                var args = _commands.PropertyArgs.Single();
                Assert.That(args.contentProperties.Single(), Does.Contain("\"amount\":{\"data\":7}"));
                Assert.That(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(args.originalFiles.Single())), Is.EqualTo(before),
                    "Only the watcher may recognize the tag echo. The CLI still gets the file the Inspector loaded.");
                var saved = tagged.Replace("\"data\": 2", "\"data\": 7");
                File.WriteAllText(_path, saved);
                _commands.Properties.Single().Completion.CompleteSuccess();
                Watch();
                Assert.That(File.ReadAllText(_path), Is.EqualTo(saved));
                Assert.That(_content.amount, Is.EqualTo(7));
                Assert.That(_content.HasPendingEditorChangeNotification, Is.False);
                Assert.That(_service.TryGetSaveFailure(_content.Id, out _), Is.False);
                Assert.That(_commands.Tags.Count, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Tag_write_preserves_unseen_raw_property_changes_for_the_CLI_conflict_check(bool sameField)
        {
            WithWatcher(() =>
            {
                const string before = "{\"properties\":{\"rows\":{\"data\":[]},\"amount\":{\"data\":2},\"future\":{\"data\":12345678901234567890}},\"tags\":[\"old\"]}";
                File.WriteAllText(_path, before);
                Watch();
                _content.Tags = new[] { "changed" };
                _service.SetContentTags(_content.Id, _content.Tags);
                var tagged = before.Replace("\"old\"", "\"changed\"");
                var outside = sameField ? tagged.Replace("\"data\":2", "\"data\":9") :
                    tagged.Replace("12345678901234567890", "12345678901234567891");
                File.WriteAllText(_path, outside);
                _commands.Tags.Single().Completion.CompleteSuccess();
                _content.amount = 3;
                _service.SaveContent(_content);
                var args = _commands.PropertyArgs.Single();
                Assert.That(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(args.originalFiles.Single())), Is.EqualTo(before));
                Assert.That(File.ReadAllText(_path), Is.EqualTo(outside));
                if (sameField)
                {
                    LogAssert.Expect(LogType.Error, new Regex("Content edit was not saved"));
                    _commands.Properties.Single().Reject("Content changed on disk", CliContentService.StaleEditExitCode);
                    Assert.That(File.ReadAllText(_path), Is.EqualTo(outside));
                    Assert.That(_service.TryGetSaveFailure(_content.Id, out var failure), Is.True);
                    Assert.That(_service.CanDiscardEdit(_content.Id, failure), Is.True);
                }
                else
                {
                    var merged = outside.Replace("\"data\":2", "\"data\":3");
                    File.WriteAllText(_path, merged);
                    _commands.Properties.Single().Completion.CompleteSuccess();
                    Assert.That(File.ReadAllText(_path), Is.EqualTo(merged));
                    Assert.That(_service.TryGetSaveFailure(_content.Id, out _), Is.False);
                }
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Saving_a_missing_field_updates_schema_feedback_from_disk_without_disturbing_the_next_edit(bool otherDifference)
        {
            WithWatcher(() =>
            {
                var config = (global::Beamable.Content.ContentConfiguration)Get(_service, "_contentConfiguration");
                config.validateSchemaDifference = true;
                var extra = otherDifference ? "\"future\":{\"data\":17}," : "";
                var before = "{\"properties\":{" + extra + "\"rows\":{\"data\":[]}},\"tags\":[\"old\"]}";
                File.WriteAllText(_path, before);
                Watch();
                var initial = _service.GetSchemaDifference(_content.Id);
                Assert.That(initial & SchemaDifference.MissingFieldsInJson, Is.EqualTo(SchemaDifference.MissingFieldsInJson));
                _content.amount = 3;
                _service.SaveContent(_content);
                _content.amount = 4;
                _content.CheckForNonDetectedChanges();
                var saved = before.Replace("\"rows\":", "\"amount\":{\"data\":3},\"rows\":");
                File.WriteAllText(_path, saved);
                File.SetLastWriteTimeUtc(_path, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                var stamp = File.GetLastWriteTimeUtc(_path);
                var revision = _service.ManifestChangedCount;
                _commands.Properties.Single().Completion.CompleteSuccess();

                var difference = _service.GetSchemaDifference(_content.Id);
                Assert.That(difference, Is.EqualTo(otherDifference ? SchemaDifference.UnknownFieldsInJson : SchemaDifference.None));
                Assert.That(_service.ManifestChangedCount, Is.GreaterThan(revision), "Wake the Inspector and table to repaint.");
                var table = new ContentWindow_TooltipsHelper().GetContentStatusBadge(_service, _service.EntriesCache[_content.Id]);
                Assert.That(table.tooltip.Contains(ContentSchemaNotice.Title), Is.EqualTo(otherDifference));
                Assert.That(_content.amount, Is.EqualTo(4));
                Assert.That(_content.HasPendingEditorChangeNotification, Is.True);
                Assert.That(File.ReadAllText(_path), Is.EqualTo(saved));
                Assert.That(File.GetLastWriteTimeUtc(_path), Is.EqualTo(stamp));
                Assert.That(_commands.Properties.Count, Is.EqualTo(1));
                Assert.That(_commands.Tags, Is.Empty);
            });
        }

        [Test]
        public void Echo_of_our_own_save_keeps_a_newer_pending_edit()
        {
            WithWatcher(() =>
            {
                LoadWithAmount(2);
                _content.amount = 3;
                var save = Save();
                WriteAsCli(0);
                _commands.Properties[0].Completion.CompleteSuccess();
                save.GetResult();
                var identity = LoadIdentity();
                _content.amount = 4;
                _content.CheckForNonDetectedChanges(); // the user keeps editing before the watcher reports the save
                Watch();
                Assert.That(_content.amount, Is.EqualTo(4), "The echo of our own save must not overwrite a newer edit.");
                Assert.That(_content.HasPendingEditorChangeNotification, Is.True, "The newer edit must still be saved.");
                Assert.That(LoadIdentity(), Is.SameAs(identity));
            });
        }

        [Test]
        public void Watcher_reload_during_a_write_waits_and_the_queued_edit_is_saved()
        {
            WithWatcher(() =>
            {
                LoadWithAmount(2);
                Exception firstError = null, queuedError = null;
                _content.amount = 3;
                Save().Error(error => firstError = error);
                _content.amount = 4;
                Save().Error(error => queuedError = error);
                WriteAsCli(0);
                Watch(); // the watcher reports our write before the CLI reply arrives
                Assert.That(_content.amount, Is.EqualTo(4));
                _commands.Properties[0].Completion.CompleteSuccess();
                Assert.That(_commands.Properties.Count, Is.EqualTo(2), "The queued edit is sent after the first save.");
                Assert.That(_commands.PropertyArgs[1].modelProperties[0], Is.EqualTo(_commands.PropertyArgs[0].contentProperties[0]));
                WriteAsCli(1);
                _commands.Properties[1].Completion.CompleteSuccess();
                Assert.That(firstError, Is.Null);
                Assert.That(queuedError, Is.Null);
                Assert.That(_content.amount, Is.EqualTo(4));
                Assert.That(File.ReadAllText(_path), Does.Contain("4"));
            });
        }

        [Test]
        public void Save_with_an_unreadable_field_keeps_the_next_Inspector_edit_and_its_raw_baseline()
        {
            WithWatcher(() =>
            {
                // This file has a numeric list element where the model expects a SaveRecoveryRow.
                string FileWithAmount(int amount) =>
                    "{\"properties\":{\"rows\":{\"data\":[17]},\"amount\":{\"data\":" + amount + "}},\"tags\":[\"old\"]}";
                var fieldError = new Regex("file is corrupted");
                File.WriteAllText(_path, FileWithAmount(2));
                LogAssert.Expect(LogType.Error, fieldError);
                Watch();
                Assert.That(_content.amount, Is.EqualTo(2));
                Assert.That(_content.rows, Is.Empty);
                var identity = LoadIdentity();

                _content.amount = 3;
                _service.SaveContent(_content);
                _content.amount = 4;
                _content.CheckForNonDetectedChanges();
                var afterFirstSave = FileWithAmount(3);
                File.WriteAllText(_path, afterFirstSave);
                LogAssert.Expect(LogType.Error, fieldError);
                _commands.Properties[0].Completion.CompleteSuccess();
                Assert.That(LoadIdentity(), Is.SameAs(identity));
                Assert.That(_content.HasPendingEditorChangeNotification, Is.True);
                Assert.That(_content.amount, Is.EqualTo(4));
                Assert.That(_service.TryGetSaveFailure(_content.Id, out _), Is.False);

                _content.CancelPendingEditorChangeNotification();
                _content.OnEditorChanged();
                var next = _commands.PropertyArgs[1];
                Assert.That(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(next.originalFiles.Single())), Is.EqualTo(afterFirstSave));
                Assert.That(next.modelProperties.Single(), Is.EqualTo(_commands.PropertyArgs[0].contentProperties.Single()));
                Assert.That(next.contentProperties.Single(), Is.EqualTo(ClientContentSerializer.SerializeProperties(_content)));
                File.WriteAllText(_path, FileWithAmount(4));
                LogAssert.Expect(LogType.Error, fieldError);
                _commands.Properties[1].Completion.CompleteSuccess();
                Assert.That(LoadIdentity(), Is.SameAs(identity));
            });
        }

        [Test]
        public void Outside_change_of_a_shown_field_reloads_and_refuses_the_queued_edit()
        {
            WithWatcher(() =>
            {
                LoadWithAmount(2);
                Exception firstError = null, queuedError = null;
                _content.amount = 3;
                Save().Error(error => firstError = error);
                _content.amount = 4;
                Save().Error(error => queuedError = error);
                // Another tool rewrites the field after our write and before Unity reads the file back.
                var outside = WriteOutsideAmount(99);
                _commands.Properties[0].Completion.CompleteSuccess();
                Assert.That(firstError, Is.Null);
                Assert.That(queuedError?.Message, Does.Contain("changed on disk"));
                Assert.That(_commands.Properties.Count, Is.EqualTo(1), "The queued 4 must not overwrite 99 unseen.");
                Assert.That(_content.amount, Is.EqualTo(99), "The Inspector shows the outside change.");
                Assert.That(File.ReadAllText(_path), Is.EqualTo(outside));
            });
        }

        [Test]
        public void Outside_change_keeps_the_warning_for_an_edit_still_waiting_to_save()
        {
            WithWatcher(() =>
            {
                LoadWithAmount(2);
                _content.amount = 3;
                _service.SaveContent(_content);
                // The next edit is still inside the save delay when another tool rewrites the field.
                _content.amount = 4;
                _content.CheckForNonDetectedChanges();
                Assert.That(_content.HasPendingEditorChangeNotification, Is.True);
                var outside = WriteOutsideAmount(99);
                LogAssert.Expect(LogType.Error, new Regex("Content edit was not saved"));
                _commands.Properties[0].Completion.CompleteSuccess();
                Assert.That(_content.amount, Is.EqualTo(99), "The Inspector shows the outside change.");
                Assert.That(File.ReadAllText(_path), Is.EqualTo(outside));
                Assert.That(_content.HasPendingEditorChangeNotification, Is.False);
                Assert.That(_service.TryGetSaveFailure(_content.Id, out var failure), Is.True,
                    "The save that found the change must not clear the warning about the newer, lost edit.");
                Assert.That(failure.Message, Does.Contain("changed on disk"));

                _content.amount = 5;
                _service.SaveContent(_content);
                WriteAsCli(1);
                _commands.Properties[1].Completion.CompleteSuccess();
                Assert.That(_content.amount, Is.EqualTo(5));
                Assert.That(_service.TryGetSaveFailure(_content.Id, out _), Is.False, "A newer successful save clears it.");
            });
        }

        [Test]
        public void Outside_write_while_idle_still_reloads()
        {
            WithWatcher(() =>
            {
                LoadWithAmount(2);
                var identity = LoadIdentity();
                WriteOutsideAmount(50);
                Watch();
                Assert.That(_content.amount, Is.EqualTo(50));
                Assert.That(LoadIdentity(), Is.Not.SameAs(identity));
            });
        }

        private void WithWatcher(Action test)
        {
            var config = SetUpWatcher();
            try { test(); }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        private global::Beamable.Content.ContentConfiguration SetUpWatcher()
        {
            PrepareFile();
            var entry = _service.EntriesCache[_content.Id];
            entry.TypeName = "saveRecovery";
            _service.EntriesCache[_content.Id] = entry;
            var reflection = new ContentTypeReflectionCache();
            reflection.OnSetupForCacheGeneration();
            ((Dictionary<string, Type>)Get(reflection, "_contentTypeToClass"))["saveRecovery"] = typeof(SaveRecoveryContent);
            ((Dictionary<Type, string>)Get(reflection, "_classToContentType"))[typeof(SaveRecoveryContent)] = "saveRecovery";
            Set(_service, "_contentTypeReflectionCache", reflection);
            var config = ScriptableObject.CreateInstance<global::Beamable.Content.ContentConfiguration>();
            Set(_service, "_contentConfiguration", config);
            return config;
        }

        private void Watch() => typeof(CliContentService).GetMethod("CacheScriptableContent", Fields)
            .Invoke(_service, new object[] { _service.EntriesCache[_content.Id] });

        private void WriteAsCli(int command) =>
            File.WriteAllText(_path, "{\"properties\":" + _commands.PropertyArgs[command].contentProperties[0] + ",\"tags\":[\"old\"]}");

        private string WriteOutsideAmount(int amount)
        {
            var other = ScriptableObject.CreateInstance<SaveRecoveryContent>();
            try
            {
                other.SetIdAndVersion(_content.Id, "");
                other.amount = amount;
                var json = ClientContentSerializer.SerializeContent(other);
                File.WriteAllText(_path, json);
                return json;
            }
            finally
            {
                other.CancelPendingEditorChangeNotification();
                UnityEngine.Object.DestroyImmediate(other);
            }
        }

        private object LoadIdentity()
        {
            var key = (string)typeof(CliContentService).GetMethod("GetContentWriteKey", Fields).Invoke(_service, new object[] { _content.Id });
            var loaded = ((System.Collections.IDictionary)Get(_service, "_loadedContent"))[key];
            return loaded.GetType().GetField("LoadIdentity").GetValue(loaded);
        }

        private void LoadWithAmount(int amount)
        {
            _content.amount = amount;
            File.WriteAllText(_path, ClientContentSerializer.SerializeContent(_content));
            typeof(CliContentService).GetMethod("LoadContentObject", Fields).Invoke(_service, new object[] { _service.EntriesCache[_content.Id], _content });
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
            public readonly List<ContentSaveArgs> PropertyArgs = new();
			public readonly List<HeldCommand> Tags = new();
			public readonly List<string> TagValues = new();
			public HeldCommands() : base(null, null) { }
			public override ContentSaveWrapper ContentSave(ContentSaveArgs args)
			{
				var command = new HeldCommand(); Properties.Add(command); PropertyArgs.Add(args);
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
			public IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> callback) { _onError += callback; return this; }
			public IBeamCommand OnTerminate(Action<ReportDataPoint<EofOutput>> callback) => this;
			private Action<ReportDataPoint<ErrorOutput>> _onError;
			/// <summary>Like the CLI: the request completes, carrying an error report with the command's exit code.</summary>
			public void Reject(string message, int exitCode)
			{
				_onError?.Invoke(new ReportDataPoint<ErrorOutput> { data = new ErrorOutput { message = message, exitCode = exitCode } });
				Completion.CompleteSuccess();
			}
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
