using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Beamable.Api.Analytics;
using Beamable.Api.Analytics.Batch;
using Beamable.Common.Dependencies;
using Beamable.Coroutines;
using Beamable.Security;
using Beamable.Security.Analytics;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using UnityEngine;

internal static class Program
{
	private const string Key = "analytics_event_batch";

	private static int Main()
	{
		var checks = new List<(string Name, Action Run)>
		{
			("SDK default interface and concrete registration share one tracker", DefaultRegistration),
			("Secure dispatch, settings and disposal use one queue and one store", SecureRegistration),
			("PlayerPrefs restore precedes the immediate first heartbeat", () => Startup(false, false)),
			("Injected restore precedes the immediate first heartbeat", () => Startup(true, false)),
			("PlayerPrefs Flush-before-Start restores before expiring", () => Startup(false, true)),
			("Injected Flush-before-Start restores before expiring", () => Startup(true, true)),
			("Heartbeat restarts never restore the same events twice", RestartDoesNotReplay),
			("SetString=false retains dirty flag and retries", () => BackupFailure("set")),
			("Flush=false retains dirty flag and retries", () => BackupFailure("flush")),
			("Write exception retains dirty flag and retries without logging data", () => BackupFailure("throw")),
			("Failed initialization blocks writes and retries the storage factory", InitializationRetry),
			("Corrupted JSON blocks overwrite until explicit recovery", CorruptedJson),
			("Legacy migration removes plaintext only after encrypted commit", () => Migration(null)),
			("Failed migration update preserves plaintext and retries", () => Migration("set")),
			("Failed migration commit preserves plaintext and retries", () => Migration("flush")),
			("Secure value wins over legacy data; cleanup is retryable", ExistingSecureValue),
			("Key access failure never falls back to legacy plaintext", NoPlaintextFallback),
			("Legacy cleanup save failure remains retryable", LegacyCleanupRetry),
			("Migration commit retried during shutdown also removes plaintext", MigrationShutdownRetry),
			("Explicit batch Flush persists events added by its callback", FlushCallback)
		};
		int failures = 0;
		foreach ((string name, Action run) in checks)
		{
			PlayerPrefs.Reset(); Debug.Warnings.Clear();
			try { run(); Console.WriteLine("PASS " + name); }
			catch (Exception exception) { failures++; Console.Error.WriteLine("FAIL " + name + ": " + exception); }
		}
		Console.WriteLine($"{checks.Count - failures}/{checks.Count} integration checks passed.");
		return failures == 0 ? 0 : 1;
	}

	private static DependencyBuilder Builder(CoroutineService coroutines, FakeAnalytics service)
	{
		var builder = new DependencyBuilder();
		builder.AddSingleton(coroutines);
		builder.AddSingleton<IBeamAnalyticsService>(service);
		AnalyticsDependencyRegistration.Register(builder); // Same registration invoked by Beam.cs.
		return builder;
	}

	private static void DefaultRegistration()
	{
		var coroutines = new CoroutineService();
		IDependencyProviderScope provider = Builder(coroutines, new FakeAnalytics()).Build();
		try
		{
			var tracker = provider.GetService<AnalyticsTracker>();
			Assert(ReferenceEquals(tracker, provider.GetService<IAnalyticsTracker>()), "Interface must alias the concrete singleton");
			Assert(coroutines.Active.Count == 1 && coroutines.Starts == 1, "One live manager");
			Assert(PlayerPrefs.Writes == 0, "Startup must not overwrite a snapshot before restore");
		}
		finally { provider.Dispose().GetAwaiter().GetResult(); }
	}

	private static void SecureRegistration()
	{
		var coroutines = new CoroutineService(); var analytics = new FakeAnalytics(); var store = new FakeStore();
		var builder = Builder(coroutines, analytics);
		SecureAnalyticsRegistration.Register(builder); // Exercise the actual add-on registration.
		Assert(builder.SingletonServices.Count(d => d.Interface == typeof(IAnalyticsTracker)) == 1, "No duplicate interface registration");
		Assert(builder.SingletonServices.Count(d => d.Interface == typeof(AnalyticsTracker)) == 1, "No hidden replacement tracker");
		int opens = 0;
		// Replace only the native factory; keep the real SDK builder/provider and shared adapter.
		builder.SingletonServices.Single(d => d.Interface == typeof(IAnalyticsBatchStorage)).Factory =
			provider => new SecureAnalyticsBatchStorage(() => { opens++; return store; });
		IDependencyProviderScope scope = builder.Build();
		try
		{
			IAnalyticsTracker throughInterface = scope.GetService<IAnalyticsTracker>();
			AnalyticsTracker throughConcrete = scope.GetService<AnalyticsTracker>();
			Assert(ReferenceEquals(throughInterface, throughConcrete), "Both access paths use the same tracker");
			throughInterface.TrackEvent(new Event("interface"), false);
			throughConcrete.TrackEvent(new Event("concrete"));
			coroutines.Tick();
			Assert(Count(store.Committed[Key]) == 2 && PlayerPrefs.Writes == 0, "Both calls use secure storage only");
			throughInterface.UpdateBatchSettings(new BatchSettings(600, 100, 2), false);
			throughConcrete.RevertBatchSettings(false);
			Assert(coroutines.Active.Count == 1 && opens == 1, "Settings retain one manager and one storage instance");
			throughInterface.TrackEvent(new Event("immediate"), true);
			Assert(analytics.Immediate.Count == 1 && Count(store.Committed[Key]) == 2, "Immediate sending bypasses the queue");
			store.BeforeDispose = () => Assert(coroutines.Active.Count == 0, "Stop the manager before disposing its store");
		}
		finally { scope.Dispose().GetAwaiter().GetResult(); }
		Assert(store.Disposals == 1, "Dispose shared storage once");
	}

	private static void Startup(bool injected, bool flushBeforeStart)
	{
		var coroutines = new CoroutineService();
		IAnalyticsBatchStorage storage = injected ? (IAnalyticsBatchStorage)new FakeStore() : new PlayerPrefsBatchStorage();
		storage.SetString(Key, Snapshot(1)); Assert(storage.Flush(), "Seed storage");
		var manager = new ProbeManager(coroutines, storage);
		int delivered = 0;
		manager.OnBatchExpired += events => delivered += events.Count;
		if (flushBeforeStart) { manager.Flush(); }
		manager.Start();
		Assert(flushBeforeStart ? delivered == 1 && manager.Count == 0 : delivered == 0 && manager.Count == 1, "Restore before writing/expiring");
		manager.Stop();
	}

	private static void RestartDoesNotReplay()
	{
		var coroutines = new CoroutineService(); var storage = new FakeStore();
		storage.Seed(Key, Snapshot(1));
		var manager = new ProbeManager(coroutines, storage); manager.Start();
		manager.SetHeartbeat(2); manager.Stop(); manager.Start();
		Assert(manager.Count == 1 && coroutines.Active.Count == 1 && storage.Reads == 1, "Restored once despite restarts");
		manager.Stop();
	}

	private static void BackupFailure(string failure)
	{
		var coroutines = new CoroutineService(); var storage = new FakeStore();
		var manager = new ProbeManager(coroutines, storage); manager.Start(); manager.Add(new AnalyticsEventRequest("sensitive-payload"));
		storage.FailSet = failure == "set"; storage.FailFlush = failure == "flush"; storage.ThrowSet = failure == "throw";
		coroutines.Tick();
		Assert(manager.Pending && !storage.Committed.ContainsKey(Key) && Debug.Warnings.Count > 0, "Failure is reported and remains dirty");
		Assert(Debug.Warnings.All(message => !message.Contains("sensitive-payload")), "Diagnostics contain no payload");
		storage.FailSet = storage.FailFlush = storage.ThrowSet = false;
		coroutines.Tick();
		Assert(!manager.Pending && Count(storage.Committed[Key]) == 1, "Unchanged queue retried successfully");
		manager.Stop();
	}

	private static void InitializationRetry()
	{
		bool unavailable = true; int opens = 0; var storage = new FakeStore(); var coroutines = new CoroutineService();
		var adapter = new SecureAnalyticsBatchStorage(() =>
		{
			opens++; if (unavailable) { throw new CryptographicException("sensitive-payload"); } return storage;
		});
		var manager = new ProbeManager(coroutines, adapter); manager.Start(); manager.Add(new AnalyticsEventRequest("pending")); coroutines.Tick();
		Assert(storage.Sets == 0 && storage.Reads == 0, "No writes/read fallback on failed initialization");
		unavailable = false; coroutines.Tick();
		Assert(Count(storage.Committed[Key]) == 1 && !manager.Pending && opens > 1, "Initialization can retry");
		coroutines.Tick(); Assert(storage.Disposals == 0, "Do not dispose the shared store per heartbeat");
		manager.Stop(); adapter.OnDispose();
	}

	private static void CorruptedJson()
	{
		var coroutines = new CoroutineService(); var storage = new FakeStore(); storage.Seed(Key, "corrupted");
		var manager = new ProbeManager(coroutines, storage); manager.Start(); manager.Add(new AnalyticsEventRequest("new")); coroutines.Tick();
		Assert(storage.Sets == 0 && storage.Committed[Key] == "corrupted", "Unreadable state must remain untouched");
		storage.Seed(Key, Snapshot(1)); coroutines.Tick();
		Assert(Count(storage.Committed[Key]) == 2, "Explicit recovery restores both existing and new items"); manager.Stop();
	}

	private static void Migration(string failure)
	{
		PlayerPrefs.SetString(Key, Snapshot(1)); var storage = new FakeStore();
		storage.FailSet = failure == "set"; storage.FailFlush = failure == "flush";
		var adapter = new SecureAnalyticsBatchStorage(() => storage);
		if (failure != null)
		{
			Throws<IOException>(() => adapter.TryGetString(Key, out _));
			Assert(PlayerPrefs.HasKey(Key) && !storage.Committed.ContainsKey(Key), "Keep plaintext until encrypted commit");
			storage.FailSet = storage.FailFlush = false;
		}
		Assert(adapter.TryGetString(Key, out string restored) && Count(restored) == 1, "Read migrated events");
		Assert(!PlayerPrefs.HasKey(Key) && Count(storage.Committed[Key]) == 1, "Remove only after durable encrypted write");
		adapter.OnDispose();
	}

	private static void ExistingSecureValue()
	{
		PlayerPrefs.SetString(Key, Snapshot(2)); var storage = new FakeStore(); storage.Seed(Key, Snapshot(1)); storage.FailFlush = true;
		var adapter = new SecureAnalyticsBatchStorage(() => storage);
		Throws<IOException>(() => adapter.TryGetString(Key, out _)); Assert(PlayerPrefs.HasKey(Key), "Keep legacy while confirmation fails");
		storage.FailFlush = false;
		Assert(adapter.TryGetString(Key, out string restored) && Count(restored) == 1 && !PlayerPrefs.HasKey(Key), "Existing secure value is authoritative");
		adapter.OnDispose();
	}

	private static void NoPlaintextFallback()
	{
		PlayerPrefs.SetString(Key, Snapshot(1));
		var adapter = new SecureAnalyticsBatchStorage(() => throw new CryptographicException());
		Throws<CryptographicException>(() => adapter.TryGetString(Key, out _));
		Assert(PlayerPrefs.HasKey(Key), "No legacy deletion on inaccessible secure storage"); adapter.OnDispose();
	}

	private static void LegacyCleanupRetry()
	{
		PlayerPrefs.SetString(Key, Snapshot(1)); PlayerPrefs.FailSave = true; var storage = new FakeStore();
		var adapter = new SecureAnalyticsBatchStorage(() => storage);
		Throws<IOException>(() => adapter.TryGetString(Key, out _));
		Assert(storage.Committed.ContainsKey(Key), "Secure commit happens before cleanup");
		int attempts = PlayerPrefs.Saves;
		Throws<IOException>(() => adapter.TryGetString(Key, out _)); Assert(PlayerPrefs.Saves > attempts, "Retry cleanup even if in-memory legacy key is absent");
		PlayerPrefs.FailSave = false; Assert(adapter.TryGetString(Key, out _), "Cleanup can finish"); adapter.OnDispose();
	}

	private static void FlushCallback()
	{
		var coroutines = new CoroutineService(); var storage = new FakeStore(); var manager = new ProbeManager(coroutines, storage);
		manager.Start(); manager.Add(new AnalyticsEventRequest("first"));
		manager.OnBatchExpired += events => manager.Add(new AnalyticsEventRequest("added-in-callback"));
		manager.Flush();
		Assert(Count(storage.Committed[Key]) == 1 && storage.Committed[Key].Contains("added-in-callback"), "Persist the queue after rotation and callback");
		manager.Stop();
	}

	private static void MigrationShutdownRetry()
	{
		PlayerPrefs.SetString(Key, Snapshot(1)); var storage = new FakeStore { FailFlush = true };
		var adapter = new SecureAnalyticsBatchStorage(() => storage);
		Throws<IOException>(() => adapter.TryGetString(Key, out _));
		Assert(PlayerPrefs.HasKey(Key), "Keep plaintext on failed commit");
		storage.FailFlush = false;
		adapter.OnDispose();
		Assert(!PlayerPrefs.HasKey(Key) && Count(storage.Committed[Key]) == 1 && storage.Disposals == 1, "Commit, cleanup and dispose in order");
	}

	private static string Snapshot(int count)
	{
		var batch = new SerializableBatch<AnalyticsEventRequest>(100, 600);
		for (int i = 0; i < count; i++) { batch.Add(new AnalyticsEventRequest("saved-" + i)); }
		return Json.Serialize(JsonSerializable.Serialize(batch), new System.Text.StringBuilder());
	}
	private static int Count(string snapshot) => ((List<object>)((Dictionary<string, object>)Json.Deserialize(snapshot))["items"]).Count;
	private static void Assert(bool condition, string message) { if (!condition) { throw new Exception(message); } }
	private static void Throws<T>(Action action) where T : Exception
	{
		try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name);
	}

	private sealed class ProbeManager : PersistentBatchManager<AnalyticsEventRequest>
	{
		internal ProbeManager(CoroutineService coroutines, IAnalyticsBatchStorage storage) : base(coroutines, Key, 100, 600, storage) { }
		internal bool Pending => _backupBatchNow;
		internal int Count => _currentBatch.Count;
	}
	private sealed class FakeStore : ISecureStorage, IAnalyticsBatchStorage
	{
		internal readonly Dictionary<string, string> Committed = new Dictionary<string, string>();
		private readonly Dictionary<string, string> _pending = new Dictionary<string, string>();
		internal bool FailSet, FailFlush, ThrowSet;
		internal int Sets, Reads, Disposals;
		internal Action BeforeDispose;
		internal void Seed(string key, string value) { Committed[key] = value; _pending[key] = value; }
		public bool TryGetString(string key, out string value) { Reads++; return _pending.TryGetValue(key, out value); }
		public bool SetString(string key, string value)
		{
			Sets++; if (ThrowSet) { throw new IOException("sensitive-payload"); } if (FailSet) { return false; } _pending[key] = value; return true;
		}
		public bool DeleteKey(string key) { _pending.Remove(key); return true; }
		public bool Flush()
		{
			if (FailFlush) { return false; } Committed.Clear(); foreach (var pair in _pending) { Committed[pair.Key] = pair.Value; } return true;
		}
		public void Dispose() { BeforeDispose?.Invoke(); Disposals++; }
	}
	private sealed class Event : IAnalyticsEvent
	{
		internal readonly string Payload;
		internal Event(string payload) { Payload = payload; }
		public string OpCode => "test";
		public void Serialize(JsonSerializable.IStreamSerializer stream) { }
	}
	private sealed class FakeAnalytics : IBeamAnalyticsService
	{
		internal readonly List<AnalyticsEventRequest> Immediate = new List<AnalyticsEventRequest>();
		public void SendAnalyticsEvent(AnalyticsEventRequest request) { Immediate.Add(request); }
		public void SendAnalyticsEventBatch(List<AnalyticsEventRequest> events) { }
		public AnalyticsEventRequest BuildRequest(IAnalyticsEvent analyticsEvent) => new AnalyticsEventRequest(((Event)analyticsEvent).Payload);
	}
}
