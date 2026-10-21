using System;
using System.Collections;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine.TestTools;
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

		[UnityTest]
		public IEnumerator ManifestIncludesUnpublishedFilesAndSupportsTagQueriesWithoutSharingMutableMetadata()
		{
			Write(ContentId, Json(17));
			Write("localSourceRegression.other", Json(4, "other"));
			var source = Source();
			yield return Await(source.GetManifest(Namespace));
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

		[UnityTest]
		public IEnumerator ResolvesCliWorkingCopyInsteadOfContentAtWorkspaceRoot()
		{
			Write(ContentId, Json(17));
			var otherDirectory = Path.Combine(_root, "content", "realm", Namespace);
			Directory.CreateDirectory(otherDirectory);
			File.WriteAllText(Path.Combine(otherDirectory, ContentId + ".json"), Json(99));
			var source = Source();
			yield return Await(source.GetManifest(Namespace));
			var content = (LocalSourceRegressionContent)source.GetContent(ContentId, typeof(ContentObject), Namespace).GetResult();
			Assert.That(content.amount, Is.EqualTo(17));
		}

		[UnityTest]
		public IEnumerator ReentrantResolutionWaitsForOneFullyInitializedOwnedObject()
		{
			const string id = "localReentrantRegression.self";
			Write(id, Json(17));
			var source = Source();
			yield return Await(source.GetManifest(Namespace));
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

		[UnityTest]
		public IEnumerator ReentrantFailureSettlesWaitersAndAllowsRetry()
		{
			const string id = "localReentrantRegression.self";
			Write(id, Json(17));
			var source = Source();
			yield return Await(source.GetManifest(Namespace));
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

		[UnityTest]
		public IEnumerator CyclicDeserializationCompletesBothObjectsWithoutReconstruction()
		{
			const string first = "localReentrantRegression.first";
			const string second = "localReentrantRegression.second";
			Write(first, Json(1));
			Write(second, Json(2));
			var source = Source();
			yield return Await(source.GetManifest(Namespace));
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

		private static readonly string[] QueryCases = { "", "tag:draft", "tag:missing", "t:localSourceRegression", "id:other", "t:localSourceRegression tag:draft id:unpublished" };
		[UnityTest]
		public IEnumerator FilteredSnapshotsMatchManifestQuerySemanticsAndIsolateMutations([ValueSource(nameof(QueryCases))] string filter)
		{
			Write(ContentId, Json(17));
			Write("localSourceRegression.other", Json(4, "other"));
			Write("localSourceRegression.other.child", Json(5, "other"));
			var source = Source();
			yield return Await(source.GetManifest(Namespace));
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

		[UnityTest]
		public IEnumerator EmptyDirectoryIsAnEmptyManifest()
		{
			var request = Source().GetManifest(Namespace);
			yield return Await(request);
			Assert.That(request.GetResult().entries, Is.Empty);
		}

		[UnityTest]
		public IEnumerator MissingDirectoryFailsLocallyWithoutCreatingItAndCanRetry()
		{
			Directory.Delete(_directory);
			var source = Source();
			var failed = source.GetManifest(Namespace);
			yield return Await(failed, expectError: true);
			Assert.That(Failure(failed).Message, Does.Contain(_directory));
			Assert.That(Directory.Exists(_directory), Is.False);
			Directory.CreateDirectory(_directory);
			Write(ContentId, Json(17));
			var retry = source.GetManifest(Namespace);
			yield return Await(retry);
			Assert.That(retry.GetResult().entries.Count, Is.EqualTo(1));
		}

		private static readonly string[] MalformedCases = { "[]", "{\"properties\":[],\"tags\":[]}", "{\"properties\":{},\"tags\":[12]}", "{\"properties\":{}}" };
		[UnityTest]
		public IEnumerator MalformedFileReportsPathAndDoesNotPublishPartialSnapshot([ValueSource(nameof(MalformedCases))] string malformed)
		{
			Write("localSourceRegression.a", Json(1));
			var path = Write(ContentId, malformed);
			var source = Source();
			var failed = source.GetManifest(Namespace);
			yield return Await(failed, expectError: true);
			Assert.That(Failure(failed).Message, Does.Contain(path));
			Write(ContentId, Json(17));
			var retry = source.GetManifest(Namespace);
			yield return Await(retry);
			Assert.That(retry.GetResult().entries.Count, Is.EqualTo(2));
		}

		[UnityTest]
		public IEnumerator TypedResolutionUsesRegisteredConcreteTypeAndLocalIdentity()
		{
			Write(ContentId, Json(17));
			var source = Source();
			yield return Await(source.GetManifest(Namespace));
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

		[UnityTest]
		public IEnumerator MissingItemFailsInsteadOfFallingBack()
		{
			var missing = Source().GetContent(ContentId, typeof(ContentObject), Namespace);
			yield return Await(missing, expectError: true);
			Assert.That(Failure(missing),
				Is.InstanceOf<ContentNotFoundException>());
		}

		[UnityTest]
		public IEnumerator SnapshotKeepsManifestAndItemConsistentAndNewSourceObservesEdits()
		{
			Write(ContentId, Json(17));
			var source = Source();
			yield return Await(source.GetManifest(Namespace));
			var version = source.GetManifest(Namespace).GetResult().entries.Single().version;
			Write(ContentId, Json(99));
			var original = (LocalSourceRegressionContent)source.GetContent(ContentId, typeof(ContentObject), Namespace).GetResult();
			Assert.That(original.amount, Is.EqualTo(17));
			Assert.That(original.Version, Is.EqualTo(version));
			var freshRequest = Source().GetContent(ContentId, typeof(ContentObject), Namespace);
			yield return Await(freshRequest);
			var fresh = (LocalSourceRegressionContent)freshRequest.GetResult();
			Assert.That(fresh.amount, Is.EqualTo(99));
			Assert.That(fresh.Version, Is.Not.EqualTo(version));
		}

		[UnityTest]
		public IEnumerator ResolvingAndMutatingRuntimeObjectDoesNotWriteAuthoringFile()
		{
			var path = Write(ContentId, Json(17));
			var before = File.ReadAllBytes(path);
			var lastWrite = File.GetLastWriteTimeUtc(path);
			var source = Source();
			yield return Await(source.GetManifest(Namespace));
			source.GetManifest(Namespace).GetResult();
			var content = (LocalSourceRegressionContent)source.GetContent(ContentId, typeof(ContentObject), Namespace).GetResult();
			content.amount = 90;
			content.Tags = new[] { "runtime-only" };
			Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
			Assert.That(File.GetLastWriteTimeUtc(path), Is.EqualTo(lastWrite));
			Assert.That(Directory.GetFiles(_directory).Length, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator DisposeDestroysOwnedObjectsAndRejectsFurtherRequests()
		{
			Write(ContentId, Json(17));
			var source = Source();
			yield return Await(source.GetManifest(Namespace));
			var content = (ContentObject)source.GetContent(ContentId, typeof(ContentObject), Namespace).GetResult();
			source.Dispose();
			Assert.That(content == null, Is.True, "The source must release its native Unity objects.");
			Assert.That(SnapshotTask(source), Is.Null, "Disposal must release the task's retained JSON snapshot.");
			Assert.That(Failure(source.GetManifest(Namespace)), Is.InstanceOf<ObjectDisposedException>());
			Assert.That(Failure(source.GetContent(ContentId, typeof(ContentObject), Namespace)), Is.InstanceOf<ObjectDisposedException>());
			Assert.DoesNotThrow(() => source.Dispose());
		}

		[UnityTest]
		public IEnumerator ColdSnapshotReturnsPendingBeforeMainThreadCompletion()
		{
			Write(ContentId, Json(17));
			var request = Source().GetManifest(Namespace);
			Assert.That(request.IsCompleted, Is.False, "A cold snapshot must not read and parse the working copy on the caller thread.");
			yield return Await(request);
			Assert.That(request.GetResult().entries.Single().contentId, Is.EqualTo(ContentId));
		}

		private static IEnumerator Await<T>(Promise<T> promise, bool expectError = false)
		{
			Exception failure = null;
			promise.Error(error => failure = error);
			var timer = System.Diagnostics.Stopwatch.StartNew();
			for (var frame = 0; !promise.IsCompleted && frame < 10000 && timer.Elapsed.TotalSeconds < 15; frame++)
				yield return null;
			Assert.That(promise.IsCompleted, Is.True, "Local content request did not settle within the bounded wait.");
			if (expectError) Assert.That(failure, Is.Not.Null, "Expected a failed request.");
			else Assert.That(failure, Is.Null, failure?.ToString());
		}

		[UnityTest]
		public IEnumerator ConcurrentColdRequestsShareSnapshotAndMaterializeOnMainThread()
		{
			const string id = "localReentrantRegression.concurrent";
			Write(id, Json(17));
			var source = Source();
			var mainThread = Thread.CurrentThread.ManagedThreadId;
			var callbackThread = -1;
			var callbackCount = 0;
			LocalReentrantRegressionContent.AfterDeserialize = content =>
			{
				_callbackObjects.Add(content);
				callbackThread = Thread.CurrentThread.ManagedThreadId;
				callbackCount++;
			};
			var manifest = source.GetManifest(Namespace);
			var snapshotTask = SnapshotTask(source);
			Assert.That(snapshotTask, Is.Not.Null);
			var first = source.GetContent(id, typeof(ContentObject), Namespace);
			var second = source.GetContent(id, typeof(LocalReentrantRegressionContent), Namespace);
			Assert.That(SnapshotTask(source), Is.SameAs(snapshotTask), "Concurrent requests must share one filesystem scan.");
			var manifestThread = -1;
			var contentThread = -1;
			manifest.Then(_ => manifestThread = Thread.CurrentThread.ManagedThreadId);
			first.Then(_ => contentThread = Thread.CurrentThread.ManagedThreadId);
			yield return Await(manifest);
			yield return Await(first);
			yield return Await(second);
			Assert.That(first.GetResult(), Is.SameAs(second.GetResult()));
			Assert.That(first.GetResult().Version, Is.EqualTo(manifest.GetResult().entries.Single().version));
			Assert.That(callbackCount, Is.EqualTo(1));
			Assert.That(callbackThread, Is.EqualTo(mainThread));
			Assert.That(manifestThread, Is.EqualTo(mainThread));
			Assert.That(contentThread, Is.EqualTo(mainThread));
			Assert.That(source.GetContent(id, typeof(ContentObject), Namespace).IsCompleted, Is.True, "Warm item lookup remains synchronous.");
		}

		[UnityTest]
		public IEnumerator DisposalDuringColdLoadFailsAllWaitersWithoutPublishingSnapshot()
		{
			Write(ContentId, Json(17));
			var source = Source();
			var manifest = source.GetManifest(Namespace);
			var content = source.GetContent(ContentId, typeof(ContentObject), Namespace);
			manifest.Error(_ => { });
			content.Error(_ => { });
			var background = SnapshotTask(source);
			Assert.That(manifest.IsCompleted, Is.False);
			source.Dispose();
			yield return Await(manifest, expectError: true);
			yield return Await(content, expectError: true);
			Assert.That(Failure(manifest), Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<ObjectDisposedException>());
			Assert.That(Failure(content), Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<ObjectDisposedException>());
			Assert.That(typeof(EditorLocalContentSource).GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(source), Is.Null);
			// Ensure the background reader has exited before TearDown removes its temporary directory.
			var timer = System.Diagnostics.Stopwatch.StartNew();
			for (var frame = 0; !background.IsCompleted && frame < 10000 && timer.Elapsed.TotalSeconds < 15; frame++)
				yield return null;
			Assert.That(background.IsCompleted, Is.True);
			if (background.IsFaulted) _ = background.Exception;
		}

		[UnityTest]
		public IEnumerator FailedConcurrentWaiterCannotClearRetryStartedByEarlierWaiter()
		{
			Write(ContentId, "[]");
			var source = Source();
			var first = source.GetManifest(Namespace);
			var second = source.GetManifest(Namespace);
			second.Error(_ => { });
			Promise<ClientManifest> retry = null;
			Task retryTask = null;
			first.Error(_ =>
			{
				Write(ContentId, Json(17));
				retry = source.GetManifest(Namespace);
				retry.Error(__ => { });
				retryTask = SnapshotTask(source);
			});
			yield return Await(first, expectError: true);
			yield return Await(second, expectError: true);
			Assert.That(retry, Is.Not.Null);
			Assert.That(SnapshotTask(source), Is.SameAs(retryTask));
			yield return Await(retry);
			Assert.That(retry.GetResult().entries.Single().contentId, Is.EqualTo(ContentId));
		}

		[UnityTest]
		public IEnumerator WorkerCountsProduceIdenticalDeterministicMetadata()
		{
			for (var index = 23; index >= 0; index--)
				Write("localSourceRegression.item" + index.ToString("D2"), Json(index, index % 2 == 0 ? "even" : "odd"));
			string[] expected = null;
			foreach (var workers in new[] { 1, 2, 4 })
			{
				var source = Source(workerCount: workers);
				var request = source.GetManifest(Namespace);
				yield return Await(request);
				var entries = request.GetResult().entries;
				Assert.That(entries.Select(e => e.contentId), Is.Ordered.Using<string>(StringComparer.Ordinal));
				var metadata = entries.Select(e => string.Join("|", e.contentId, e.manifestID, e.version, e.uri,
					e.visibility.ToString(), string.Join(",", e.tags))).ToArray();
				if (expected == null) expected = metadata;
				else Assert.That(metadata, Is.EqualTo(expected), "Metadata changed with worker count " + workers);
			}
		}

		private static Task SnapshotTask(EditorLocalContentSource source) =>
			(Task)typeof(EditorLocalContentSource).GetField("_snapshotTask", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(source);

		private EditorLocalContentSource Source(bool enabled = true, int workerCount = 4)
		{
			var source = new EditorLocalContentSource(enabled, _root, "customer", "realm", Namespace, workerCount);
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
