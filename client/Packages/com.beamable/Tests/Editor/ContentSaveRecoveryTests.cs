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
			_service.manifestIdOverride = "global";
			_content = ScriptableObject.CreateInstance<SaveRecoveryContent>();
			_content.SetIdAndVersion("saveRecovery.test", "");
			_content.CancelPendingEditorChangeNotification();
			_content.ContentStatus = ContentStatus.UpToDate;
			_content.Tags = new[] { "old" };
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
				Assert.That(_content.OnEditorChanged, Is.SameAs(callback));
				_content.OnEditorChanged();
				Assert.That(_commands.Properties.Count, Is.EqualTo(1));
			}
			finally { Directory.Delete(_path); }
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
