using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Common.Dependencies;
using Beamable.Content;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.ContentService
{
	/// <summary>
	/// Resolves one content namespace for one <see cref="BeamContext"/> from a read-only snapshot of the
	/// CLI working copy, including unpublished items. The realm and namespace are fixed when the context
	/// creates this source: a game that switches realms restarts its contexts, and an editor realm switch
	/// during Play Mode takes effect at the next Play Mode start. The snapshot is read once, off the main
	/// thread; Unity objects are created and destroyed on the main thread.
	/// </summary>
	public sealed class EditorLocalContentSource : ILocalContentSource, IBeamableDisposable
	{
		private const int ReadWorkers = 4;

		private readonly string _manifestId;
		private readonly string _pid;
		private readonly Promise<string> _workspace;
		private readonly float _manifestDelaySeconds;
		private readonly float _contentDelaySeconds;
		private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
		private readonly Dictionary<string, ContentObject> _objects = new Dictionary<string, ContentObject>(StringComparer.Ordinal);
		private readonly Dictionary<string, Promise<IContentObject>> _inFlight = new Dictionary<string, Promise<IContentObject>>(StringComparer.Ordinal);
		private Task<Snapshot> _snapshot;
		private bool _disposed;

		/// <param name="manifestId">The namespace this source resolves.</param>
		/// <param name="pid">The realm whose working copy is read.</param>
		/// <param name="workspace">Resolves to the CLI configuration directory that holds the working copy.</param>
		public EditorLocalContentSource(string manifestId, string pid, Promise<string> workspace,
		                                float manifestDelaySeconds = 0, float contentDelaySeconds = 0)
		{
			_manifestId = manifestId;
			_pid = pid;
			_workspace = workspace;
			_manifestDelaySeconds = manifestDelaySeconds;
			_contentDelaySeconds = contentDelaySeconds;
			// The objects are HideAndDontSave, so they would outlive a domain reload unless destroyed first.
			AssemblyReloadEvents.beforeAssemblyReload += Release;
		}

		public bool HandlesManifest(string manifestId) => string.Equals(manifestId, _manifestId, StringComparison.Ordinal);

		public async Promise<ClientManifest> GetManifest(string manifestId, ContentQuery query = null)
		{
			var snapshot = await Load(manifestId, _manifestDelaySeconds);
			var entries = query == null ? new List<ClientContentInfo>(snapshot.Manifest.Count) : new List<ClientContentInfo>();
			foreach (var entry in snapshot.Manifest)
			{
				if (query != null && !query.Accept(entry)) continue;
				entries.Add(new ClientContentInfo
				{
					contentId = entry.contentId,
					manifestID = entry.manifestID,
					version = entry.version,
					tags = (string[])entry.tags.Clone(),
					visibility = ContentVisibility.Public,
					uri = string.Empty
				});
			}

			return new ClientManifest { entries = entries };
		}

		public async Promise<IContentObject> GetContent(string contentId, Type contentType, string manifestId)
		{
			var snapshot = await Load(manifestId, _contentDelaySeconds);
			return await Materialize(snapshot, contentId, contentType);
		}

		public Promise OnDispose()
		{
			Release();
			return Promise.Success;
		}

		private async Task<Snapshot> Load(string manifestId, float delaySeconds)
		{
			if (!HandlesManifest(manifestId))
				throw new InvalidOperationException($"Local content does not handle namespace '{manifestId}'.");
			ThrowIfDisposed();
			if (delaySeconds > 0 && !float.IsInfinity(delaySeconds) && !float.IsNaN(delaySeconds))
				await Task.Delay(TimeSpan.FromSeconds(Math.Min(delaySeconds, 86400)), _cancellation.Token);

			// Concurrent requests share one read; a failed read is retried by the next request.
			if (_snapshot == null || _snapshot.IsFaulted || _snapshot.IsCanceled) _snapshot = ReadSnapshot();
			var snapshot = await _snapshot;
			ThrowIfDisposed();
			return snapshot;
		}

		private async Task<Snapshot> ReadSnapshot()
		{
			var workspace = await WaitForWorkspace();
			if (string.IsNullOrWhiteSpace(workspace))
				throw new InvalidOperationException("Local content requires a configured Beamable workspace.");
			if (string.IsNullOrWhiteSpace(_pid))
				throw new InvalidOperationException("Local content cannot load because the realm ID (PID) is unavailable.");
			if (!ContentConfiguration.IsValidManifestID(_manifestId, out var message))
				throw new InvalidOperationException($"Invalid local content namespace {_manifestId}: {message}");

			// Match the CLI's ConfigService.CONTENT_DIR working-copy layout.
			var directory = Path.Combine(workspace, "local", "content", _pid, _manifestId);
			var token = _cancellation.Token;
			return await Task.Run(() => ReadFiles(directory, _manifestId, token), token);
		}

		private async Task<string> WaitForWorkspace()
		{
			// Cancel this source's wait without completing or cancelling the shared editor readiness promise.
			var completion = new TaskCompletionSource<string>();
			using (_cancellation.Token.Register(() => completion.TrySetCanceled()))
			{
				_ = _workspace.Error(error => completion.TrySetException(error))
					.Then(path => completion.TrySetResult(path));
				return await completion.Task;
			}
		}

		private static Snapshot ReadFiles(string directory, string manifestId, CancellationToken token)
		{
			if (!Directory.Exists(directory))
				throw new InvalidOperationException($"Local content directory {directory} does not exist.");

			var paths = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
			Array.Sort(paths, StringComparer.Ordinal);
			var loaded = new LocalEntry[paths.Length];
			var errors = new Exception[paths.Length];
			Parallel.For(0, paths.Length, new ParallelOptions { MaxDegreeOfParallelism = ReadWorkers, CancellationToken = token }, index =>
			{
				try
				{
					loaded[index] = ReadFile(paths[index]);
				}
				catch (Exception e)
				{
					errors[index] = new InvalidOperationException($"Failed to load local content file '{paths[index]}'.", e);
				}
			});

			// Report the first failure in path order, so the error does not depend on worker scheduling.
			var snapshot = new Snapshot();
			for (var index = 0; index < loaded.Length; index++)
			{
				if (errors[index] != null) throw errors[index];
				var entry = loaded[index];
				snapshot.Entries.Add(entry.Id, entry);
				snapshot.Manifest.Add(new ClientContentInfo
				{
					contentId = entry.Id, manifestID = manifestId, version = entry.Version,
					tags = entry.Tags, visibility = ContentVisibility.Public, uri = string.Empty
				});
			}

			// Path order differs from ID order ("a.b.json" precedes "a.json"); keep the manifest in ID order.
			snapshot.Manifest.Sort((left, right) => string.CompareOrdinal(left.contentId, right.contentId));
			return snapshot;
		}

		private static LocalEntry ReadFile(string path)
		{
			var id = Path.GetFileNameWithoutExtension(path);
			var json = File.ReadAllText(path);
			if (!(Json.Deserialize(json) is ArrayDict root))
				throw new FormatException($"The content file must contain a JSON object: {id}");
			if (!root.TryGetValue("properties", out var properties) || !(properties is ArrayDict))
				throw new FormatException($"The content file must contain a 'properties' object: {id}");
			if (!root.TryGetValue("tags", out var tagsValue) || !(tagsValue is List<object> rawTags))
				throw new FormatException($"The content file must contain a 'tags' array: {id}");

			var tags = new string[rawTags.Count];
			for (var index = 0; index < rawTags.Count; index++)
			{
				if (!(rawTags[index] is string tag)) throw new FormatException($"Tag at index {index} must be a string.");
				tags[index] = tag;
			}

			return new LocalEntry
			{
				Id = id, Path = path, Json = json, Tags = tags,
				Version = "local:" + ClientContentSerializer.CalculateChecksum(json)
			};
		}

		private Promise<IContentObject> Materialize(Snapshot snapshot, string contentId, Type contentType)
		{
			ContentObject instance = null;
			Promise<IContentObject> pending = null;
			try
			{
				if (contentType == null) throw new ArgumentNullException(nameof(contentType));
				if (string.IsNullOrWhiteSpace(contentId) || !snapshot.Entries.TryGetValue(contentId, out var entry))
					throw new ContentNotFoundException(contentId);

				var actualType = BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>().GetTypeFromId(contentId);
				if (actualType == null || actualType.IsAbstract ||
				    !typeof(ContentObject).IsAssignableFrom(actualType) || !contentType.IsAssignableFrom(actualType))
				{
					throw new InvalidOperationException($"Local content '{contentId}' cannot resolve as '{contentType.FullName}'. " +
					                                    $"Registered type: '{actualType?.FullName ?? "<unknown>"}'.");
				}

				if (_objects.TryGetValue(contentId, out var cached) && cached != null)
					return Promise<IContentObject>.Successful(cached);
				// Serialization callbacks and eager ContentLinks can resolve this ID again while it loads.
				// They share its completion and never see a partially initialized object.
				if (_inFlight.TryGetValue(contentId, out var existing)) return existing;

				pending = new Promise<IContentObject>();
				// The completion can fail before any caller observes it; callers still receive the failure.
				pending.Error(_ => { });
				_inFlight.Add(contentId, pending);

				instance = (ContentObject)ScriptableObject.CreateInstance(actualType);
				instance.hideFlags = HideFlags.HideAndDontSave;
				ClientContentSerializer.DeserializeContentFromCli(entry.Json, instance, entry.Id, out var schemaDifference, disableExceptions: false);
				ThrowIfDisposed();

				instance.SetIdAndVersion(entry.Id, entry.Version);
				instance.SetManifestID(_manifestId);
				instance.Tags = (string[])entry.Tags.Clone();
				if (ContentConfiguration.Instance.validateSchemaDifference && schemaDifference != SchemaDifference.None)
					Debug.LogWarning($"Local content '{entry.Id}' has a schema difference: {schemaDifference}. File: '{entry.Path}'.");

				_objects[contentId] = instance;
				_inFlight.Remove(contentId);
				pending.CompleteSuccess(instance);
				return pending;
			}
			catch (Exception exception)
			{
				if (instance != null) Destroy(instance);
				if (pending == null) return Promise<IContentObject>.Failed(exception);
				_inFlight.Remove(contentId);
				pending.CompleteError(exception);
				return pending;
			}
		}

		private void ThrowIfDisposed()
		{
			if (_disposed) throw new ObjectDisposedException(nameof(EditorLocalContentSource));
		}

		private void Release()
		{
			if (_disposed) return;
			_disposed = true;
			AssemblyReloadEvents.beforeAssemblyReload -= Release;
			_cancellation.Cancel();
			_cancellation.Dispose();
			foreach (var content in _objects.Values)
			{
				if (content == null) continue;
				content.CancelPendingEditorChangeNotification();
				Destroy(content);
			}
			_objects.Clear();
			_snapshot = null;
		}

		private static void Destroy(ContentObject content)
		{
			if (Application.isPlaying) UnityEngine.Object.Destroy(content);
			else UnityEngine.Object.DestroyImmediate(content);
		}

		private sealed class Snapshot
		{
			public readonly Dictionary<string, LocalEntry> Entries = new Dictionary<string, LocalEntry>(StringComparer.Ordinal);
			public readonly List<ClientContentInfo> Manifest = new List<ClientContentInfo>();
		}

		private sealed class LocalEntry
		{
			public string Id;
			public string Path;
			public string Json;
			public string Version;
			public string[] Tags;
		}
	}
}
