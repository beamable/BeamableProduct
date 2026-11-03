using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Editor.ContentService;
using Beamable.Platform.Tests.Content;
using Beamable.Tests.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests
{
	/// <summary>
	/// Which local content source a new context gets, given the setting and the Content window's namespace.
	/// </summary>
	public class LocalContentRegistrationTests
	{
		private ContentConfiguration _config;
		private readonly List<ILocalContentSource> _created = new List<ILocalContentSource>();

		[SetUp]
		public void SetUp()
		{
			// A transient configuration, so the tests never read or change the project's settings.
			_config = ScriptableObject.CreateInstance<ContentConfiguration>();
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var source in _created)
				if (source is EditorLocalContentSource local) local.OnDispose();
			_created.Clear();
			UnityEngine.Object.DestroyImmediate(_config);
		}

		[Test]
		public void LocalModeResolvesTheNamespaceSelectedInTheContentWindow()
		{
			var source = Create("seasonal");
			Assert.That(source.HandlesManifest("seasonal"), Is.True);
			Assert.That(source.HandlesManifest(LocalWorkingCopy.Namespace), Is.False,
			            "Only the selected namespace resolves locally.");
		}

		[TestCase(null)]
		[TestCase("")]
		public void NoContentWindowSelectionMeansTheDefaultNamespace(string selection)
		{
			Assert.That(Create(selection).HandlesManifest("global"), Is.True);
		}

		[Test]
		public void DisabledSettingResolvesNothingLocallyAndNeverConsultsTheEditor()
		{
			var source = EditorLocalContentRegistration.Create(new ContentParameterProvider(), _config,
				() => throw new AssertionException("The Content window was consulted."),
				() => throw new AssertionException("The workspace was consulted."));
			Assert.That(source.HandlesManifest("global"), Is.False);
		}

		private ILocalContentSource Create(string windowNamespace)
		{
			var source = EditorLocalContentRegistration.Create(
				new ContentParameterProvider { EnableLocalContentInEditor = true }, _config,
				() => windowNamespace, () => Promise<string>.Successful(string.Empty));
			_created.Add(source);
			return source;
		}
	}

	/// <summary>
	/// The editor's local content source over a temporary CLI working copy, through its public API only.
	/// </summary>
	public class LocalContentSourceTests
	{
		private LocalWorkingCopy _copy;
		private readonly List<ContentObject> _callbackObjects = new List<ContentObject>();

		[SetUp]
		public void SetUp() => _copy = new LocalWorkingCopy();

		[TearDown]
		public void TearDown()
		{
			LocalReentrantRegressionContent.AfterDeserialize = null;
			_copy.Dispose();
			foreach (var content in _callbackObjects)
				if (content != null) UnityEngine.Object.DestroyImmediate(content);
			_callbackObjects.Clear();
		}

		[UnityTest]
		public IEnumerator UnpublishedItemResolvesFromTheCliWorkingCopy()
		{
			_copy.Write(LocalWorkingCopy.ItemId, LocalWorkingCopy.Json(17, "draft"));
			// A same-named file outside local/content must be ignored.
			_copy.WriteOutsideWorkingCopy(LocalWorkingCopy.ItemId, LocalWorkingCopy.Json(99));
			var source = _copy.Source();

			var manifest = source.GetManifest(LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(manifest);
			var item = source.GetContent(LocalWorkingCopy.ItemId, typeof(ContentObject), LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(item);

			var entry = manifest.GetResult().entries.Single();
			var content = (LocalSourceRegressionContent)item.GetResult();
			Assert.That(entry.contentId, Is.EqualTo(LocalWorkingCopy.ItemId));
			Assert.That(entry.tags, Is.EqualTo(new[] { "draft" }));
			Assert.That(content.amount, Is.EqualTo(17));
			Assert.That(content.Id, Is.EqualTo(LocalWorkingCopy.ItemId));
			Assert.That(content.ManifestID, Is.EqualTo(LocalWorkingCopy.Namespace));
			Assert.That(content.Tags, Is.EqualTo(new[] { "draft" }));
			Assert.That(content.Version, Is.EqualTo(entry.version));
		}

		[UnityTest]
		public IEnumerator RequestingAnIncompatibleTypeFails()
		{
			_copy.Write(LocalWorkingCopy.ItemId, LocalWorkingCopy.Json(17));
			var request = _copy.Source().GetContent(LocalWorkingCopy.ItemId, typeof(LocalReentrantRegressionContent), LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(request, expectError: true);
			Assert.That(LocalWorkingCopy.Failure(request), Is.InstanceOf<InvalidOperationException>());
		}

		[UnityTest]
		public IEnumerator EditsAfterTheFirstReadAppearOnlyInANewSource()
		{
			_copy.Write(LocalWorkingCopy.ItemId, LocalWorkingCopy.Json(17));
			var source = _copy.Source();
			yield return LocalWorkingCopy.Await(source.GetManifest(LocalWorkingCopy.Namespace));

			_copy.Write(LocalWorkingCopy.ItemId, LocalWorkingCopy.Json(99));
			var original = source.GetContent(LocalWorkingCopy.ItemId, typeof(ContentObject), LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(original);
			var fresh = _copy.Source().GetContent(LocalWorkingCopy.ItemId, typeof(ContentObject), LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(fresh);

			Assert.That(((LocalSourceRegressionContent)original.GetResult()).amount, Is.EqualTo(17));
			Assert.That(((LocalSourceRegressionContent)fresh.GetResult()).amount, Is.EqualTo(99));
		}

		[UnityTest]
		public IEnumerator ChangingARuntimeObjectDoesNotWriteTheWorkingCopy()
		{
			var path = _copy.Write(LocalWorkingCopy.ItemId, LocalWorkingCopy.Json(17));
			var before = File.ReadAllBytes(path);
			var request = _copy.Source().GetContent(LocalWorkingCopy.ItemId, typeof(ContentObject), LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(request);

			var content = (LocalSourceRegressionContent)request.GetResult();
			content.amount = 90;
			content.Tags = new[] { "runtime-only" };

			Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
			Assert.That(Directory.GetFiles(Path.GetDirectoryName(path)), Has.Length.EqualTo(1));
		}

		private static readonly string[] MalformedFiles = { "[]", "{\"properties\":{},\"tags\":[12]}" };

		[UnityTest]
		public IEnumerator MalformedFileFailsWithItsPathAndRecoversOnceFixed([ValueSource(nameof(MalformedFiles))] string malformed)
		{
			_copy.Write("localSourceRegression.valid", LocalWorkingCopy.Json(1));
			var path = _copy.Write(LocalWorkingCopy.ItemId, malformed);
			var source = _copy.Source();

			var failed = source.GetManifest(LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(failed, expectError: true);
			Assert.That(LocalWorkingCopy.Failure(failed).Message, Does.Contain(path));

			_copy.Write(LocalWorkingCopy.ItemId, LocalWorkingCopy.Json(17));
			var retry = source.GetManifest(LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(retry);
			Assert.That(retry.GetResult().entries, Has.Count.EqualTo(2));
		}

		[UnityTest]
		public IEnumerator MissingDirectoryFailsWithoutCreatingIt()
		{
			var directory = _copy.ContentDirectory(BeamableTest.DEFAULT_PID);
			Directory.Delete(directory);

			var request = _copy.Source().GetManifest(LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(request, expectError: true);

			Assert.That(LocalWorkingCopy.Failure(request).Message, Does.Contain(directory));
			Assert.That(Directory.Exists(directory), Is.False);
		}

		[UnityTest]
		public IEnumerator ItemThatResolvesItselfWhileLoadingYieldsOneCompleteObject()
		{
			const string id = "localReentrantRegression.self";
			_copy.Write(id, LocalWorkingCopy.Json(17));
			var source = _copy.Source();
			yield return LocalWorkingCopy.Await(source.GetManifest(LocalWorkingCopy.Namespace));
			Promise<IContentObject> nested = null;
			LocalReentrantRegressionContent.AfterDeserialize = content =>
			{
				_callbackObjects.Add(content);
				if (_callbackObjects.Count == 1)
				{
					nested = source.GetContent(id, typeof(ContentObject), LocalWorkingCopy.Namespace);
					Assert.That(nested.IsCompleted, Is.False, "A partially deserialized object must not be exposed.");
				}
			};

			var outer = source.GetContent(id, typeof(ContentObject), LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(outer);
			yield return LocalWorkingCopy.Await(nested);

			Assert.That(nested.GetResult(), Is.SameAs(outer.GetResult()));
			Assert.That(_callbackObjects, Has.Count.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator ItemsThatResolveEachOtherWhileLoadingYieldOneObjectEach()
		{
			const string first = "localReentrantRegression.first";
			const string second = "localReentrantRegression.second";
			_copy.Write(first, LocalWorkingCopy.Json(1));
			_copy.Write(second, LocalWorkingCopy.Json(2));
			var source = _copy.Source();
			yield return LocalWorkingCopy.Await(source.GetManifest(LocalWorkingCopy.Namespace));
			var resolutions = new List<Promise<IContentObject>>();
			LocalReentrantRegressionContent.AfterDeserialize = content =>
			{
				_callbackObjects.Add(content);
				resolutions.Add(source.GetContent(content.Id == first ? second : first, typeof(ContentObject), LocalWorkingCopy.Namespace));
			};

			var result = source.GetContent(first, typeof(ContentObject), LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(result);
			foreach (var resolution in resolutions.ToList()) yield return LocalWorkingCopy.Await(resolution);

			Assert.That(_callbackObjects, Has.Count.EqualTo(2));
			Assert.That(resolutions.Single(p => p.GetResult().Id == first).GetResult(), Is.SameAs(result.GetResult()));
		}

		[UnityTest]
		public IEnumerator SimultaneousFirstRequestsShareOneObjectBuiltOnTheMainThread()
		{
			const string id = "localReentrantRegression.concurrent";
			_copy.Write(id, LocalWorkingCopy.Json(17));
			var source = _copy.Source();
			var mainThread = Thread.CurrentThread.ManagedThreadId;
			var callbackThreads = new List<int>();
			LocalReentrantRegressionContent.AfterDeserialize = content =>
			{
				_callbackObjects.Add(content);
				callbackThreads.Add(Thread.CurrentThread.ManagedThreadId);
			};

			var first = source.GetContent(id, typeof(ContentObject), LocalWorkingCopy.Namespace);
			var second = source.GetContent(id, typeof(LocalReentrantRegressionContent), LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(first);
			yield return LocalWorkingCopy.Await(second);

			Assert.That(first.GetResult(), Is.SameAs(second.GetResult()));
			Assert.That(callbackThreads, Is.EqualTo(new[] { mainThread }));
		}

		[UnityTest]
		public IEnumerator DisposingReleasesObjectsAndFailsLaterRequests()
		{
			_copy.Write(LocalWorkingCopy.ItemId, LocalWorkingCopy.Json(17));
			var source = _copy.Source();
			var request = source.GetContent(LocalWorkingCopy.ItemId, typeof(ContentObject), LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(request);
			var content = (ContentObject)request.GetResult();

			LocalWorkingCopy.DisposeSource(source);
			var later = source.GetContent(LocalWorkingCopy.ItemId, typeof(ContentObject), LocalWorkingCopy.Namespace);
			yield return LocalWorkingCopy.Await(later, expectError: true);

			Assert.That(content == null, Is.True, "Disposal must destroy the Unity objects the source created.");
			Assert.That(LocalWorkingCopy.Failure(later), Is.InstanceOf<ObjectDisposedException>());
		}

		[UnityTest]
		public IEnumerator DisposingDuringTheFirstReadFailsPendingRequests()
		{
			_copy.Write(LocalWorkingCopy.ItemId, LocalWorkingCopy.Json(17));
			var source = _copy.Source();
			var manifest = source.GetManifest(LocalWorkingCopy.Namespace);
			var content = source.GetContent(LocalWorkingCopy.ItemId, typeof(ContentObject), LocalWorkingCopy.Namespace);

			LocalWorkingCopy.DisposeSource(source);
			yield return LocalWorkingCopy.Await(manifest, expectError: true);
			yield return LocalWorkingCopy.Await(content, expectError: true);

			Assert.That(LocalWorkingCopy.Failure(manifest), Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<ObjectDisposedException>());
			Assert.That(LocalWorkingCopy.Failure(content), Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<ObjectDisposedException>());
		}
	}
}
