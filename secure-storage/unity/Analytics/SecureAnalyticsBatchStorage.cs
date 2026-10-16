#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || (!UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_IOS))
using System;
using System.Collections.Generic;
using System.IO;
using Beamable.Api.Analytics.Batch;
using Beamable.Common;
using Beamable.Common.Dependencies;
using UnityEngine;

namespace Beamable.Security.Analytics
{
	/// <summary>Owns one secure store for the dependency scope; never falls back to PlayerPrefs on failure.</summary>
	public sealed class SecureAnalyticsBatchStorage : IAnalyticsBatchStorage, IBeamableDisposableOrder
	{
		private readonly Func<ISecureStorage> _createStorage;
		private readonly HashSet<string> _legacyToRemove = new HashSet<string>(StringComparer.Ordinal);
		private ISecureStorage _storage;
		private bool _disposed;

		public SecureAnalyticsBatchStorage(string directoryPath, string storeId)
			: this(() => new SecureStorage(directoryPath, storeId))
		{
		}

		/// <summary>Factory is retried only when initialization fails. A successful store stays open until scope disposal.</summary>
		public SecureAnalyticsBatchStorage(Func<ISecureStorage> createStorage)
		{
			_createStorage = createStorage ?? throw new ArgumentNullException(nameof(createStorage));
		}

		public bool TryGetString(string key, out string value)
		{
			ISecureStorage storage = GetStorage(); // Failure propagates; do not read legacy plaintext then.
			bool found = storage.TryGetString(key, out value);
			if (!found && PlayerPrefs.HasKey(key))
			{
				string legacy = PlayerPrefs.GetString(key);
				if (!storage.SetString(key, legacy))
				{
					throw new IOException("Legacy analytics migration could not accept encrypted data.");
				}
				// Track cleanup before committing so a failed commit can also be retried by
				// Flush/OnDispose without requiring another read of the legacy entry.
				_legacyToRemove.Add(key);
				value = legacy;
				found = true;
			}
			if (found && PlayerPrefs.HasKey(key)) { _legacyToRemove.Add(key); }
			if (_legacyToRemove.Count > 0)
			{
				// This also handles an in-memory value from a previously failed migration flush.
				if (!storage.Flush()) { throw new IOException("Encrypted analytics commit failed before legacy cleanup."); }
				RemoveCommittedLegacyEntries();
			}
			return found;
		}

		public bool SetString(string key, string value)
		{
			if (!GetStorage().SetString(key, value)) { return false; }
			if (PlayerPrefs.HasKey(key)) { _legacyToRemove.Add(key); }
			return true;
		}

		public bool Flush()
		{
			if (!GetStorage().Flush()) { return false; }
			RemoveCommittedLegacyEntries();
			return true;
		}

		// AnalyticsTracker stops/saves at order -100; release the store afterwards.
		public int DisposeOrder => 100;

		public Promise OnDispose()
		{
			if (_disposed) { return Promise.Success; }
			try
			{
				if (_storage != null && !Flush())
				{
					Debug.LogWarning("[SecureAnalyticsBatchStorage] Shutdown commit failed; pending changes could not be saved.");
				}
			}
			catch (Exception exception)
			{
				Debug.LogWarning("[SecureAnalyticsBatchStorage] Shutdown commit failed. Error type: " + exception.GetType().Name);
			}
			finally
			{
				_disposed = true;
				_storage?.Dispose();
			}
			return Promise.Success;
		}

		private ISecureStorage GetStorage()
		{
			if (_disposed) { throw new ObjectDisposedException(nameof(SecureAnalyticsBatchStorage)); }
			return _storage ?? (_storage = _createStorage());
		}

		private void RemoveCommittedLegacyEntries()
		{
			if (_legacyToRemove.Count == 0) { return; }
			foreach (string key in _legacyToRemove) { PlayerPrefs.DeleteKey(key); }
			PlayerPrefs.Save();
			_legacyToRemove.Clear();
		}
	}
}
#endif
