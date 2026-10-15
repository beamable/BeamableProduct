using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Content;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Beamable.Editor.ContentService
{
	public class EditorLocalContentSource : ILocalContentSource, IDisposable
	{
		private readonly bool _enabled;
		private readonly string _configDirectory;
		private readonly string _cid;
		private readonly string _pid;
		private readonly string _manifestId;

		private Dictionary<string, LocalEntry> _entries;
		private readonly Dictionary<string, ContentObject> _objects = new Dictionary<string, ContentObject>(StringComparer.Ordinal);
		private bool _disposed;

		public EditorLocalContentSource(bool enabled, string configDirectory, string cid, string pid, string manifestId)
		{
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
		public Promise<ClientManifest> GetManifest(string manifestId)
		{
			try
			{
				if (!HandlesManifest(manifestId))
				{
					throw new InvalidOperationException($"Local content does not handle namespace '{manifestId}'.");
				}

				EnsureSnapshot();

				var entries = new List<ClientContentInfo>(_entries.Count);

				foreach (var entry in _entries.Values)
				{
					entries.Add(new ClientContentInfo
					{
						contentId = entry.Id,
						manifestID = _manifestId,
						version = entry.Version,
						tags = (string[])entry.Tags.Clone(),
						visibility = ContentVisibility.Public,
						uri = string.Empty
					});
				}

				entries.Sort((left, right) => string.CompareOrdinal(left.contentId, right.contentId));

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

			try
			{
				if (!HandlesManifest(manifestId))
				{
					throw new InvalidOperationException($"Local content does not handle namespace '{manifestId}'.");
				}

				if (contentType == null) throw new ArgumentNullException(nameof(contentType));

				if (string.IsNullOrWhiteSpace(contentId)) throw new ContentNotFoundException(contentId);

				EnsureSnapshot();

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

				instance = (ContentObject)ScriptableObject.CreateInstance(actualType);
				instance.hideFlags = HideFlags.HideAndDontSave;

				ClientContentSerializer.DeserializeContentFromCli(entry.Json, instance, entry.Id,
				                                                  out var schemaDifference, disableExceptions: false);

				instance.SetIdAndVersion(entry.Id, entry.Version);
				instance.SetManifestID(_manifestId);
				instance.Tags = (string[])entry.Tags.Clone();

				if (ContentConfiguration.Instance.validateSchemaDifference && schemaDifference != SchemaDifference.None)
				{
					Debug.LogWarning($"Local content '{entry.Id}' has a schema difference: " +
					                 $"{schemaDifference}. File: '{entry.Path}'.");
				}

				_objects[contentId] = instance;
				return Promise<IContentObject>.Successful(instance);
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

				return Promise<IContentObject>.Failed(exception);
			}
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
		private void EnsureSnapshot()
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

			// Match the CLI's ConfigService.CONTENT_DIR working-copy layout.
			var directory = Path.Combine(_configDirectory, "local", "content", _pid, _manifestId);

			if (!Directory.Exists(directory))
			{
				throw new InvalidOperationException($"Local content directory {directory} does not exist.");
			}

			//Build separately so a failed load never leaves a partial snapshot
			var entries = new Dictionary<string, LocalEntry>(StringComparer.Ordinal);
			var paths = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);

			//Stable Ordering makes manifest output predictable
			Array.Sort(paths, StringComparer.Ordinal);

			foreach (string path in paths)
			{
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

					entries.Add(id, new LocalEntry
					{
						Id = id,
						Path = path,
						Json = json,
						Version = "local:" + ClientContentSerializer.CalculateChecksum(json),
						Tags = tags
					});
				}
				catch (Exception e)
				{
					throw new InvalidOperationException($"Failed to load local content file '{path}'.", e);
				}
			}

			// Publish only after every file has loaded successfully.
			_entries = entries;
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			foreach (var content in _objects.Values)
			{
				if (content == null) continue;
				content.CancelPendingEditorChangeNotification();
				if (Application.isPlaying) UnityEngine.Object.Destroy(content);
				else UnityEngine.Object.DestroyImmediate(content);
			}
			_objects.Clear();
			_entries = null;
		}
	}
}
