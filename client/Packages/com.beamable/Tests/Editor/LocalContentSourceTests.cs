using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Editor.ContentService;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Editor.Tests
{
	public class LocalContentSourceTests
	{
		private const string Namespace = "local-test";
		private const string ContentId = "localSourceRegression.unpublished";
		private string _root;
		private string _directory;
		private readonly List<EditorLocalContentSource> _sources = new List<EditorLocalContentSource>();
		private readonly List<ContentObject> _callbackObjects = new List<ContentObject>();

		[SetUp]
		public void SetUp()
		{
			_root = Path.Combine(Path.GetTempPath(), "beamable-local-content-" + Guid.NewGuid().ToString("N"));
			// Match ConfigService.CONTENT_DIR: the CLI working copy lives in local/content.
			_directory = Path.Combine(_root, "local", "content", "realm", Namespace);
			Directory.CreateDirectory(_directory);
		}

		[TearDown]
		public void TearDown()
		{
			LocalReentrantRegressionContent.AfterDeserialize = null;
			foreach (var source in _sources) source.Dispose();
			foreach (var content in _callbackObjects)
				if (content != null) UnityEngine.Object.DestroyImmediate(content);
			_callbackObjects.Clear();
			_sources.Clear();
			if (Directory.Exists(_root)) Directory.Delete(_root, true);
		}

		[Test]
		public void RoutingRequiresEnabledSettingAndExactNamespaceEvenWhenDirectoryIsMissing()
		{
			Directory.Delete(_directory);
			var source = Source();
			Assert.That(source.HandlesManifest(Namespace), Is.True);
			Assert.That(source.HandlesManifest("global"), Is.False);
			Assert.That(source.HandlesManifest(Namespace.ToUpperInvariant()), Is.False);
			Assert.That(source.HandlesManifest(null), Is.False);
			Assert.That(Source(false).HandlesManifest(Namespace), Is.False);
			Assert.That(Failure(source.GetManifest("global")), Is.InstanceOf<InvalidOperationException>());
		}

		[Test]
		public void ManifestIncludesUnpublishedFilesAndSupportsTagQueriesWithoutSharingMutableMetadata()
		{
			Write(ContentId, Json(17));
			Write("localSourceRegression.other", Json(4, "other"));
			var source = Source();
			var manifest = source.GetManifest(Namespace).GetResult();
			Assert.That(manifest.entries.Select(e => e.contentId),
				Is.EqualTo(new[] { "localSourceRegression.other", ContentId }));
			var selected = manifest.Filter("tag:draft").entries.Single();
			Assert.That(selected.contentId, Is.EqualTo(ContentId));
			Assert.That(selected.manifestID, Is.EqualTo(Namespace));
			Assert.That(selected.version, Does.StartWith("local:"));
			Assert.That(selected.uri, Is.Empty);
			selected.tags[0] = "mutated";
			manifest.entries.Clear();
			Assert.That(source.GetManifest(Namespace).GetResult().Filter("tag:draft").entries.Count, Is.EqualTo(1));
		}

		[Test]
		public void ResolvesCliWorkingCopyInsteadOfContentAtWorkspaceRoot()
		{
			Write(ContentId, Json(17));
			var otherDirectory = Path.Combine(_root, "content", "realm", Namespace);
			Directory.CreateDirectory(otherDirectory);
			File.WriteAllText(Path.Combine(otherDirectory, ContentId + ".json"), Json(99));
			var source = Source();
			var content = (LocalSourceRegressionContent)source.GetContent(ContentId, typeof(ContentObject), Namespace).GetResult();
			Assert.That(content.amount, Is.EqualTo(17));
		}

		[Test]
		public void ReentrantResolutionWaitsForOneFullyInitializedOwnedObject()
		{
			const string id = "localReentrantRegression.self";
			Write(id, Json(17));
			var source = Source();
			Promise<IContentObject> nested = null;
			LocalReentrantRegressionContent.AfterDeserialize = content =>
			{
				_callbackObjects.Add(content);
				if (_callbackObjects.Count == 1)
				{
					nested = source.GetContent(id, typeof(ContentObject), Namespace);
					Assert.That(nested.IsCompleted, Is.False, "Do not expose a partially deserialized object.");
				}
			};
			var outer = source.GetContent(id, typeof(ContentObject), Namespace).GetResult();
			Assert.That(nested.GetResult(), Is.SameAs(outer));
			Assert.That(_callbackObjects.Count, Is.EqualTo(1));
			Assert.That(outer.Version, Does.StartWith("local:"));
			source.Dispose();
			Assert.That((ContentObject)outer == null, Is.True);
		}

		[Test]
		public void ReentrantFailureSettlesWaitersAndAllowsRetry()
		{
			const string id = "localReentrantRegression.self";
			Write(id, Json(17));
			var source = Source();
			Promise<IContentObject> nested = null;
			var expected = new InvalidOperationException("callback failed");
			LocalReentrantRegressionContent.AfterDeserialize = content =>
			{
				_callbackObjects.Add(content);
				if (_callbackObjects.Count == 1)
				{
					nested = source.GetContent(id, typeof(ContentObject), Namespace);
					nested.Error(_ => { });
					throw expected;
				}
			};
			Assert.That(Failure(source.GetContent(id, typeof(ContentObject), Namespace)), Is.SameAs(expected));
			Assert.That(Failure(nested), Is.SameAs(expected));
			Assert.That(_callbackObjects.Single() == null, Is.True);
			LocalReentrantRegressionContent.AfterDeserialize = null;
			Assert.That(source.GetContent(id, typeof(ContentObject), Namespace).GetResult(), Is.Not.Null);
		}

		[Test]
		public void CyclicDeserializationCompletesBothObjectsWithoutReconstruction()
		{
			const string first = "localReentrantRegression.first";
			const string second = "localReentrantRegression.second";
			Write(first, Json(1));
			Write(second, Json(2));
			var source = Source();
			var resolutions = new List<Promise<IContentObject>>();
			LocalReentrantRegressionContent.AfterDeserialize = content =>
			{
				_callbackObjects.Add(content);
				Assert.That(_callbackObjects.Count, Is.LessThanOrEqualTo(2), "Cycle must not construct another object.");
				resolutions.Add(source.GetContent(content.Id == first ? second : first, typeof(ContentObject), Namespace));
			};
			var result = source.GetContent(first, typeof(ContentObject), Namespace).GetResult();
			Assert.That(_callbackObjects.Count, Is.EqualTo(2));
			Assert.That(resolutions.All(p => p.IsCompleted), Is.True);
			Assert.That(resolutions.Single(p => p.GetResult().Id == first).GetResult(), Is.SameAs(result));
			source.Dispose();
			Assert.That(_callbackObjects.All(c => c == null), Is.True);
		}

		[TestCase("")]
		[TestCase("tag:draft")]
		[TestCase("tag:missing")]
		[TestCase("t:localSourceRegression")]
		[TestCase("id:other")]
		[TestCase("t:localSourceRegression tag:draft id:unpublished")]
		public void FilteredSnapshotsMatchManifestQuerySemanticsAndIsolateMutations(string filter)
		{
			Write(ContentId, Json(17));
			Write("localSourceRegression.other", Json(4, "other"));
			Write("localSourceRegression.other.child", Json(5, "other"));
			var source = Source();
			var query = ContentQuery.Parse(filter);
			var expected = source.GetManifest(Namespace).GetResult().Filter(query);
			var result = source.GetManifest(Namespace, query).GetResult();
			Assert.That(result.entries.Select(e => e.contentId), Is.EqualTo(expected.entries.Select(e => e.contentId)));
			foreach (var entry in result.entries)
			{
				entry.contentId = "mutated";
				entry.tags[0] = "mutated";
			}
			result.entries.Clear();
			var repeated = source.GetManifest(Namespace, query).GetResult();
			Assert.That(repeated.entries.Select(e => e.contentId), Is.EqualTo(expected.entries.Select(e => e.contentId)));
			Assert.That(source.GetManifest(Namespace).GetResult().entries.Count, Is.EqualTo(3));
		}

		[Test]
		public void EmptyDirectoryIsAnEmptyManifest()
		{
			Assert.That(Source().GetManifest(Namespace).GetResult().entries, Is.Empty);
		}

		[Test]
		public void MissingDirectoryFailsLocallyWithoutCreatingItAndCanRetry()
		{
			Directory.Delete(_directory);
			var source = Source();
			Assert.That(Failure(source.GetManifest(Namespace)).Message, Does.Contain(_directory));
			Assert.That(Directory.Exists(_directory), Is.False);
			Directory.CreateDirectory(_directory);
			Write(ContentId, Json(17));
			Assert.That(source.GetManifest(Namespace).GetResult().entries.Count, Is.EqualTo(1));
		}

		[TestCase("[]")]
		[TestCase("{\"properties\":[],\"tags\":[]}")]
		[TestCase("{\"properties\":{},\"tags\":[12]}")]
		[TestCase("{\"properties\":{}}")]
		public void MalformedFileReportsPathAndDoesNotPublishPartialSnapshot(string malformed)
		{
			Write("localSourceRegression.a", Json(1));
			var path = Write(ContentId, malformed);
			var source = Source();
			Assert.That(Failure(source.GetManifest(Namespace)).Message, Does.Contain(path));
			Write(ContentId, Json(17));
			Assert.That(source.GetManifest(Namespace).GetResult().entries.Count, Is.EqualTo(2));
		}

		[Test]
		public void TypedResolutionUsesRegisteredConcreteTypeAndLocalIdentity()
		{
			Write(ContentId, Json(17));
			var source = Source();
			var content = (LocalSourceRegressionContent)source.GetContent(ContentId, typeof(ContentObject), Namespace).GetResult();
			Assert.That(content.amount, Is.EqualTo(17));
			Assert.That(content.Id, Is.EqualTo(ContentId));
			Assert.That(content.ManifestID, Is.EqualTo(Namespace));
			Assert.That(content.Tags, Is.EqualTo(new[] { "draft" }));
			Assert.That(content.Version, Is.EqualTo(source.GetManifest(Namespace).GetResult().entries.Single().version));
			Assert.That(content.OnEditorChanged, Is.Null);
			Assert.That(source.GetContent(ContentId, typeof(LocalSourceRegressionContent), Namespace).GetResult(), Is.SameAs(content));
			Assert.That(Failure(source.GetContent(ContentId, typeof(string), Namespace)), Is.InstanceOf<InvalidOperationException>());
		}

		[Test]
		public void MissingItemFailsInsteadOfFallingBack()
		{
			Assert.That(Failure(Source().GetContent(ContentId, typeof(ContentObject), Namespace)),
				Is.InstanceOf<ContentNotFoundException>());
		}

		[Test]
		public void SnapshotKeepsManifestAndItemConsistentAndNewSourceObservesEdits()
		{
			Write(ContentId, Json(17));
			var source = Source();
			var version = source.GetManifest(Namespace).GetResult().entries.Single().version;
			Write(ContentId, Json(99));
			var original = (LocalSourceRegressionContent)source.GetContent(ContentId, typeof(ContentObject), Namespace).GetResult();
			Assert.That(original.amount, Is.EqualTo(17));
			Assert.That(original.Version, Is.EqualTo(version));
			var fresh = (LocalSourceRegressionContent)Source().GetContent(ContentId, typeof(ContentObject), Namespace).GetResult();
			Assert.That(fresh.amount, Is.EqualTo(99));
			Assert.That(fresh.Version, Is.Not.EqualTo(version));
		}

		[Test]
		public void ResolvingAndMutatingRuntimeObjectDoesNotWriteAuthoringFile()
		{
			var path = Write(ContentId, Json(17));
			var before = File.ReadAllBytes(path);
			var lastWrite = File.GetLastWriteTimeUtc(path);
			var source = Source();
			source.GetManifest(Namespace).GetResult();
			var content = (LocalSourceRegressionContent)source.GetContent(ContentId, typeof(ContentObject), Namespace).GetResult();
			content.amount = 90;
			content.Tags = new[] { "runtime-only" };
			Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
			Assert.That(File.GetLastWriteTimeUtc(path), Is.EqualTo(lastWrite));
			Assert.That(Directory.GetFiles(_directory).Length, Is.EqualTo(1));
		}

		[Test]
		public void DisposeDestroysOwnedObjectsAndRejectsFurtherRequests()
		{
			Write(ContentId, Json(17));
			var source = Source();
			var content = (ContentObject)source.GetContent(ContentId, typeof(ContentObject), Namespace).GetResult();
			source.Dispose();
			Assert.That(content == null, Is.True, "The source must release its native Unity objects.");
			Assert.That(Failure(source.GetManifest(Namespace)), Is.InstanceOf<ObjectDisposedException>());
			Assert.That(Failure(source.GetContent(ContentId, typeof(ContentObject), Namespace)), Is.InstanceOf<ObjectDisposedException>());
			Assert.DoesNotThrow(() => source.Dispose());
		}

		private EditorLocalContentSource Source(bool enabled = true)
		{
			var source = new EditorLocalContentSource(enabled, _root, "customer", "realm", Namespace);
			_sources.Add(source);
			return source;
		}

		private string Write(string id, string json)
		{
			var path = Path.Combine(_directory, id + ".json");
			File.WriteAllText(path, json);
			return path;
		}

		private static string Json(int amount, string tag = "draft") =>
			"{\"properties\":{\"amount\":{\"data\":" + amount + "}},\"tags\":[\"" + tag + "\"]}";

		private static Exception Failure<T>(Promise<T> promise)
		{
			Exception observed = null;
			promise.Error(error => observed = error);
			Assert.That(observed, Is.Not.Null, "Expected a settled failure, not a pending or successful promise.");
			return observed;
		}
	}

	[ContentType("localSourceRegression")]
	public class LocalSourceRegressionContent : ContentObject
	{
		public int amount;
	}

	[ContentType("localReentrantRegression")]
	public class LocalReentrantRegressionContent : ContentObject, ISerializationCallbackReceiver
	{
		public int amount;
		public static Action<LocalReentrantRegressionContent> AfterDeserialize;
		public void OnBeforeSerialize() { }
		public void OnAfterDeserialize() => AfterDeserialize?.Invoke(this);
	}
}
