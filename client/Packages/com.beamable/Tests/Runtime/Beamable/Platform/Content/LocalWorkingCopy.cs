using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Editor.ContentService;
using Beamable.Tests.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Platform.Tests.Content
{
	/// <summary>
	/// A temporary CLI workspace, laid out like ConfigService.CONTENT_DIR, plus the sources created over it.
	/// </summary>
	public sealed class LocalWorkingCopy : IDisposable
	{
		public const string Namespace = "global";
		public const string ItemId = "localSourceRegression.unpublished";

		public readonly string Workspace = Path.Combine(Path.GetTempPath(), "beamable-local-content-" + Guid.NewGuid().ToString("N"));
		private readonly List<EditorLocalContentSource> _sources = new List<EditorLocalContentSource>();

		public LocalWorkingCopy() => Directory.CreateDirectory(ContentDirectory(BeamableTest.DEFAULT_PID));

		public EditorLocalContentSource Source(string pid = BeamableTest.DEFAULT_PID) =>
			Track(new EditorLocalContentSource(Namespace, pid, Promise<string>.Successful(Workspace)));

		public EditorLocalContentSource Track(EditorLocalContentSource source)
		{
			_sources.Add(source);
			return source;
		}

		public static void DisposeSource(EditorLocalContentSource source) => source.OnDispose();

		public string ContentDirectory(string pid) => Path.Combine(Workspace, "local", "content", pid, Namespace);

		public string Write(string id, string json, string pid = BeamableTest.DEFAULT_PID)
		{
			var directory = ContentDirectory(pid);
			Directory.CreateDirectory(directory);
			var path = Path.Combine(directory, id + ".json");
			File.WriteAllText(path, json);
			return path;
		}

		public void WriteOutsideWorkingCopy(string id, string json)
		{
			var directory = Path.Combine(Workspace, "content", BeamableTest.DEFAULT_PID, Namespace);
			Directory.CreateDirectory(directory);
			File.WriteAllText(Path.Combine(directory, id + ".json"), json);
		}

		public static string Json(int amount, string tag = "draft") =>
			"{\"properties\":{\"amount\":{\"data\":" + amount + "}},\"tags\":[\"" + tag + "\"]}";

		public static IEnumerator Await<T>(Promise<T> promise, bool expectError = false)
		{
			Exception failure = null;
			promise.Error(error => failure = error);
			yield return WaitUntil(() => promise.IsCompleted);
			Assert.That(promise.IsCompleted, Is.True, "The request did not settle within the bounded wait.");
			if (expectError) Assert.That(failure, Is.Not.Null, "Expected the request to fail.");
			else Assert.That(failure, Is.Null, failure?.ToString());
		}

		public static IEnumerator WaitUntil(Func<bool> condition)
		{
			var timer = System.Diagnostics.Stopwatch.StartNew();
			while (!condition() && timer.Elapsed.TotalSeconds < 15) yield return null;
		}

		public static Exception Failure<T>(Promise<T> promise)
		{
			Exception failure = null;
			promise.Error(error => failure = error);
			Assert.That(failure, Is.Not.Null, "Expected a settled failure.");
			return failure;
		}

		public void Dispose()
		{
			foreach (var source in _sources) DisposeSource(source);
			_sources.Clear();
			if (Directory.Exists(Workspace)) Directory.Delete(Workspace, true);
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
