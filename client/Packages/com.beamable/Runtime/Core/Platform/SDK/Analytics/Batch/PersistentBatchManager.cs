using Beamable.Coroutines;
using Beamable.Pooling;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Api.Analytics.Batch
{
	/// <summary>A single batch queue with injectable persistence and retryable backups.</summary>
	public class PersistentBatchManager<T> : BatchManager<T> where T : class, JsonSerializable.ISerializable
	{
		protected string _storageKey;
		protected bool _backupBatchNow;
		private readonly IAnalyticsBatchStorage _storage;
		private bool _restored;

		public PersistentBatchManager(CoroutineService coroutineService, string storageKey, int batchCapacity,
			double batchTimeoutSeconds, float heartbeatInterval = 1f)
			: this(coroutineService, storageKey, batchCapacity, batchTimeoutSeconds, new PlayerPrefsBatchStorage(), heartbeatInterval)
		{
		}

		/// <summary>The caller owns storage and must keep it alive until this manager is stopped.</summary>
		public PersistentBatchManager(CoroutineService coroutineService, string storageKey, int batchCapacity,
			double batchTimeoutSeconds, IAnalyticsBatchStorage storage, float heartbeatInterval = 1f)
			: base(coroutineService, batchCapacity, batchTimeoutSeconds, heartbeatInterval)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
			_storageKey = storageKey;
			_backupBatchNow = false;
		}

		protected override void OnStart() { RestoreEventBatch(); }

		public override void Add(T item)
		{
			base.Add(item);
			_backupBatchNow = true;
		}

		protected override void RotateBatch()
		{
			_currentBatch = new SerializableBatch<T>(_batchCapacity, _batchTimeoutSeconds);
			_currentBatch.OnExpired += OnExpired;
			_backupBatchNow = true;
		}

		protected override void OnHeartbeat()
		{
			// Failed restoration must never be followed by an empty overwrite or legacy fallback.
			if (!EnsureRestored()) { return; }
			base.OnHeartbeat();
			if (_backupBatchNow) { BackupEventBatch(); }
		}

		public override void Flush()
		{
			// Safe before Start too: restore, expire/send, then persist the rotated queue.
			if (!EnsureRestored()) { return; }
			try { base.Flush(); }
			finally { SavePendingBatch(); }
		}

		/// <summary>Saves without expiring/sending the queue. False preserves the retry flag.</summary>
		public bool SavePendingBatch()
		{
			if (!EnsureRestored()) { return false; }
			if (!_backupBatchNow) { return true; }
			try
			{
				Dictionary<string, object> batchDictionary = JsonSerializable.Serialize((SerializableBatch<T>)_currentBatch);
				using (var pooledBuilder = StringBuilderPool.StaticPool.Spawn())
				{
					string batchJson = Json.Serialize(batchDictionary, pooledBuilder.Builder);
					if (!_storage.SetString(_storageKey, batchJson))
					{
						Debug.LogWarning("[PersistentBatchManager] Storage rejected the batch update; backup remains pending.");
						return false;
					}
					if (!_storage.Flush())
					{
						Debug.LogWarning("[PersistentBatchManager] Storage commit failed; backup remains pending.");
						return false;
					}
				}
				_backupBatchNow = false;
				return true;
			}
			catch (Exception exception)
			{
				Debug.LogWarning("[PersistentBatchManager] Backup failed; will retry. Error type: " + exception.GetType().Name);
				return false;
			}
		}

		protected void BackupEventBatch() { SavePendingBatch(); }
		protected void RestoreEventBatch() { EnsureRestored(); }

		private bool EnsureRestored()
		{
			if (_restored) { return true; }
			try
			{
				SerializableBatch<T> eventBatch = null;
				if (_storage.TryGetString(_storageKey, out string batchJson) && !string.IsNullOrEmpty(batchJson))
				{
					var parsedDictionary = Json.Deserialize(batchJson) as Dictionary<string, object>;
					if (parsedDictionary == null) { throw new FormatException("Invalid analytics batch."); }
					eventBatch = new SerializableBatch<T>(_batchCapacity, _batchTimeoutSeconds);
					JsonSerializable.Deserialize(eventBatch, parsedDictionary);
				}

				_restored = true;
				if (eventBatch?.Items != null)
				{
					for (int i = 0; i < eventBatch.Count; i++)
					{
						if (eventBatch.Items[i] != null) { Add(eventBatch.Items[i]); }
					}
				}
				return true;
			}
			catch (Exception exception)
			{
				Debug.LogWarning("[PersistentBatchManager] Restore failed; persistence is blocked until restore succeeds. Error type: " + exception.GetType().Name);
				return false;
			}
		}
	}
}
