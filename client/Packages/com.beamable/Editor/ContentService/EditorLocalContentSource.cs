using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Content;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Beamable.Editor.ContentService
{
	/// <summary>
	/// Editor-thread-owned snapshot and Unity objects. Only the isolated filesystem loader
	/// runs on worker threads; public methods and disposal must run on the editor thread.
	/// </summary>
	public class EditorLocalContentSource : ILocalContentSource, IDisposable
	{
		private readonly bool _enabled;
		private readonly string _configDirectory;
		private readonly string _cid;
		private readonly string _pid;
		private readonly string _manifestId;

		private Dictionary<string, LocalEntry> _entries;
		private List<ClientContentInfo> _manifestEntries;
		private readonly Dictionary<string, ContentObject> _objects = new Dictionary<string, ContentObject>(StringComparer.Ordinal);
		private readonly Dictionary<string, Promise<IContentObject>> _inFlight = new Dictionary<string, Promise<IContentObject>>(StringComparer.Ordinal);
		private bool _disposed;
		private readonly int _workerCount;
		private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
		private Task<Snapshot> _snapshotTask;

		public EditorLocalContentSource(bool enabled, string configDirectory, string cid, string pid, string manifestId, int workerCount = 4)
		{
			if (workerCount < 1 || workerCount > 4) throw new ArgumentOutOfRangeException(nameof(workerCount));
			_workerCount = workerCount;
			_enabled = enabled;
			_configDirectory = configDirectory;
			_cid = cid;
			_pid = pid;
			_manifestId = manifestId;
		}

		public bool HandlesManifest(string manifestId)
		{
			var canHandleManifest = _enabled && string.Equals(manifestId, _manifestId, StringComparison.Ordinal);
			return canHandleManifest;
		}

		/// <summary>
		/// Returns a manifest describing the captured local content.
		/// </summary>
		public Promise<ClientManifest> GetManifest(string manifestId, ContentQuery query = null)
		{
			try
			{
				if (!HandlesManifest(manifestId))
				{
					throw new InvalidOperationException($"Local content does not handle namespace '{manifestId}'.");
				}

				if (_disposed) throw new ObjectDisposedException(nameof(EditorLocalContentSource));
				if (_entries == null) return LoadManifest(manifestId, query);

				var entries = query == null ? new List<ClientContentInfo>(_manifestEntries.Count) : new List<ClientContentInfo>();

				foreach (var entry in _manifestEntries)
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

				return Promise<ClientManifest>.Successful(new ClientManifest {entries = entries});
			}
			catch (Exception exception)
			{
				return Promise<ClientManifest>.Failed(exception);
			}
		}

		/// <summary>
		/// Creates an independent content object from the captured local JSON.
		/// </summary>
		public Promise<IContentObject> GetContent(string contentId, Type contentType, string manifestId)
		{
			ContentObject instance = null;
			Promise<IContentObject> pending = null;

			try
			{
				if (!HandlesManifest(manifestId))
				{
					throw new InvalidOperationException($"Local content does not handle namespace '{manifestId}'.");
				}

				if (contentType == null) throw new ArgumentNullException(nameof(contentType));

				if (string.IsNullOrWhiteSpace(contentId)) throw new ContentNotFoundException(contentId);

				if (_disposed) throw new ObjectDisposedException(nameof(EditorLocalContentSource));
				if (_entries == null) return LoadContent(contentId, contentType, manifestId);

				if (!_entries.TryGetValue(contentId, out var entry)) throw new ContentNotFoundException(contentId);

				var reflectionCache = BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>();

				var actualType = reflectionCache.GetTypeFromId(contentId);

				if (actualType == null || actualType.IsAbstract ||
				    !typeof(ContentObject).IsAssignableFrom(actualType) || !contentType.IsAssignableFrom(actualType))
				{
					throw new InvalidOperationException($"Local content '{contentId}' cannot resolve as " +
					                                    $"'{contentType.FullName}'. Registered type: " +
					                                    $"'{actualType?.FullName ?? "<unknown>"}'.");
				}

				if (_objects.TryGetValue(contentId, out var cached) && cached != null)
					return Promise<IContentObject>.Successful(cached);
				if (_inFlight.TryGetValue(contentId, out var existing)) return existing;

				// Serialization callbacks and eager ContentLinks can resolve this ID again.
				// Share completion, never a partially initialized ScriptableObject.
				pending = new Promise<IContentObject>();
				// Completion can fail synchronously before the caller receives this promise.
				// Keep the internal completion observed; callers still receive its failure.
				pending.Error(_ => { });
				_inFlight.Add(contentId, pending);

				instance = (ContentObject)ScriptableObject.CreateInstance(actualType);
				instance.hideFlags = HideFlags.HideAndDontSave;

				ClientContentSerializer.DeserializeContentFromCli(entry.Json, instance, entry.Id,
				                                                  out var schemaDifference, disableExceptions: false);
				if (_disposed) throw new ObjectDisposedException(nameof(EditorLocalContentSource));

				instance.SetIdAndVersion(entry.Id, entry.Version);
				instance.SetManifestID(_manifestId);
				instance.Tags = (string[])entry.Tags.Clone();

				if (ContentConfiguration.Instance.validateSchemaDifference && schemaDifference != SchemaDifference.None)
				{
					Debug.LogWarning($"Local content '{entry.Id}' has a schema difference: " +
					                 $"{schemaDifference}. File: '{entry.Path}'.");
				}

				_objects[contentId] = instance;
				_inFlight.Remove(contentId);
				pending.CompleteSuccess(instance);
				return pending;
			}
			catch (Exception exception)
			{
				if (instance != null)
				{
					if (Application.isPlaying)
						UnityEngine.Object.Destroy(instance);
					else
						UnityEngine.Object.DestroyImmediate(instance);
				}

				if (pending == null) return Promise<IContentObject>.Failed(exception);
				_inFlight.Remove(contentId);
				pending.CompleteError(exception);
				return pending;
			}
		}

		private async Promise<ClientManifest> LoadManifest(string manifestId, ContentQuery query)
		{
			await EnsureSnapshot();
			return await GetManifest(manifestId, query);
		}

		private async Promise<IContentObject> LoadContent(string contentId, Type contentType, string manifestId)
		{
			await EnsureSnapshot();
			return await GetContent(contentId, contentType, manifestId);
		}

		private sealed class Snapshot
		{
			public Dictionary<string, LocalEntry> Entries;
			public List<ClientContentInfo> Metadata;
		}

		private class LocalEntry
		{
			public string Id;
			public string Path;
			public string Json;
			public string Version;
			public string[] Tags;
		}

		/// <summary>
		/// Loads local JSON content once, retaining the snapshot only if every file
		/// loads successfully. Does not modify files or synchronize with the realm.
		/// </summary>
		internal async Task EnsureSnapshot()
		{
			if (_disposed) throw new ObjectDisposedException(nameof(EditorLocalContentSource));
			//Reuse the successfully loaded snapshot
			if (_entries != null) return;

			if (string.IsNullOrWhiteSpace(_configDirectory))
				throw new InvalidOperationException("Local content requires a configured Beamable workspace.");

			if (string.IsNullOrWhiteSpace(_cid))
			{
				throw new InvalidOperationException("Local content cannot load because the customer ID (CID) " +
				                                    "is unavailable. Ensure Beamable workspace configuration " +
				                                    "has loaded before resolving local content.");
			}

			if (string.IsNullOrWhiteSpace(_pid))
			{
				throw new InvalidOperationException("Local content cannot load because the realm ID (PID) " +
				                                    "is unavailable. Select a realm and ensure Beamable " +
				                                    "workspace configuration has loaded.");
			}

			if (!ContentConfiguration.IsValidManifestID(_manifestId, out var validationMessage))
			{
				throw new InvalidOperationException(
					$"Invalid local content namespace {_manifestId}: {validationMessage}");
			}

			if (_snapshotTask == null)
			{
				// Capture immutable input on the editor thread; workers never use Unity APIs.
				// Match the CLI's ConfigService.CONTENT_DIR working-copy layout.
				var directory = Path.Combine(_configDirectory, "local", "content", _pid, _manifestId);
				var token = _cancellation.Token;
				_snapshotTask = Task.Run(() => ReadSnapshot(directory, _manifestId, _workerCount, token), token);
			}

			var loading = _snapshotTask;
			try
			{
				// Always hand cold completion back through the editor context, including tiny snapshots
				// that finish before we await them. Warm requests remain synchronous.
				await Task.Yield();
				var snapshot = await loading;
				if (_disposed) throw new ObjectDisposedException(nameof(EditorLocalContentSource));
				_manifestEntries = snapshot.Metadata;
				_entries = snapshot.Entries;
			}
			catch
			{
				// A failed attempt can be retried; an older waiter must not clear a newer attempt.
				if (ReferenceEquals(_snapshotTask, loading)) _snapshotTask = null;
				throw;
			}
		}

		private static Snapshot ReadSnapshot(string directory, string manifestId, int workerCount, CancellationToken token)
		{
			token.ThrowIfCancellationRequested();
			if (!Directory.Exists(directory))
			{
				throw new InvalidOperationException($"Local content directory {directory} does not exist.");
			}

			//Build separately so a failed load never leaves a partial snapshot
			var entries = new Dictionary<string, LocalEntry>(StringComparer.Ordinal);
			var paths = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);

			//Stable Ordering makes manifest output predictable
			Array.Sort(paths, StringComparer.Ordinal);

			var loaded = new LocalEntry[paths.Length];
			var errors = new Exception[paths.Length];
			Parallel.For(0, paths.Length, new ParallelOptions
			{
				MaxDegreeOfParallelism = workerCount, CancellationToken = token
			}, fileIndex =>
			{
				var path = paths[fileIndex];
				try
				{
					var id = Path.GetFileNameWithoutExtension(path);
					var json = File.ReadAllText(path);

					if (!(Json.Deserialize(json) is ArrayDict root))
					{
						throw new FormatException($"The content file must contain a JSON object: {id}");
					}

					if (!root.TryGetValue("properties", out var properties) || !(properties is ArrayDict))
					{
						throw new FormatException($"The content file must contain a 'properties' object: {id}");
					}

					if (!root.TryGetValue("tags", out var tagsValue) || !(tagsValue is List<object> rawTags))
					{
						throw new FormatException($"The content file must contain a 'tags' array: {id}");
					}

					var tags = new string[rawTags.Count];

					for (var index = 0; index < rawTags.Count; index++)
					{
						if (!(rawTags[index] is string tag))
						{
							throw new FormatException($"Tag at index {index} must be a string.");
						}

						tags[index] = tag;
					}

					loaded[fileIndex] = new LocalEntry
					{
						Id = id,
						Path = path,
						Json = json,
						Version = "local:" + ClientContentSerializer.CalculateChecksum(json),
						Tags = tags
					};
				}
				catch (Exception e)
				{
					errors[fileIndex] = new InvalidOperationException($"Failed to load local content file '{path}'.", e);
				}
			});
			token.ThrowIfCancellationRequested();
			// Preserve deterministic ordering and error selection regardless of worker scheduling.
			for (var index = 0; index < loaded.Length; index++)
			{
				if (errors[index] != null) throw errors[index];
				entries.Add(loaded[index].Id, loaded[index]);
			}

			// Build and sort private metadata once. Query results copy only matching entries.
			var metadata = new List<ClientContentInfo>(entries.Count);
			foreach (var entry in entries.Values)
				metadata.Add(new ClientContentInfo
				{
					contentId = entry.Id, manifestID = manifestId, version = entry.Version,
					tags = entry.Tags, visibility = ContentVisibility.Public, uri = string.Empty
				});
			metadata.Sort((left, right) => string.CompareOrdinal(left.contentId, right.contentId));
			return new Snapshot { Entries = entries, Metadata = metadata };
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			_cancellation.Cancel();
			_cancellation.Dispose();
			_snapshotTask = null;
			foreach (var content in _objects.Values)
			{
				if (content == null) continue;
				content.CancelPendingEditorChangeNotification();
				if (Application.isPlaying) UnityEngine.Object.Destroy(content);
				else UnityEngine.Object.DestroyImmediate(content);
			}
			_objects.Clear();
			_entries = null;
			_manifestEntries = null;
		}
	}
}
