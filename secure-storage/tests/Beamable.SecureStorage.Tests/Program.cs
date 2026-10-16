using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Beamable.Security;
using Beamable.Security.Internal;

internal static class Program
{
	private static int Main(string[] args)
	{
		var checks = new List<(string Name, Action Run)>
		{
			("Buffered values, missing keys, empty and Unicode strings", BufferedValues),
			("Flush survives reopening without plaintext on disk", Persistence),
			("Dispose discards unflushed changes", DisposeDoesNotFlush),
			("Delete is buffered, idempotent, and removes the empty snapshot", Deletion),
			("Failed write retains changes and can be retried", FailedWrite),
			("Failed replacement preserves the previous snapshot", FailedReplacement),
			("Failed delete retains changes and can be retried", FailedDelete),
			("Failed constructor releases its directory lock", ConstructorReleasesLock),
			("Concurrent owners of a directory are rejected", ExclusiveOwner),
			("Concurrent calls on a shared instance survive reopening", ConcurrentCalls),
			("Arguments and disposed instances fail explicitly", InvalidUsage),
			("Size limit rejects updates without losing previous values", SizeLimit),
			("Header, IV, ciphertext, tag, wrong key and truncation are rejected", Authentication),
			("Corrupted snapshots are never silently replaced", CorruptedSnapshot),
			("Missing keys never regenerate over existing snapshots", MissingKey),
			("Repeated encryption uses a fresh IV", RandomizedEncryption),
			("Authenticated malformed snapshots are rejected", MalformedSnapshot)
		};
		if (args.Contains("--native")) { checks.Add(("Native platform key store round trip and deletion", NativeRoundTrip)); }
		if (args.Contains("--native") && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			checks.Add(("Stable Apple identity survives moving the application directory", NativeStableIdentity));
			checks.Add(("Legacy path-derived Apple key is promoted without regenerating", NativeLegacyPromotion));
		}
		int failures = 0;
		foreach ((string name, Action run) in checks)
		{
			try { run(); Console.WriteLine("PASS " + name); }
			catch (Exception exception) { failures++; Console.Error.WriteLine("FAIL " + name + ": " + exception); }
		}
		Console.WriteLine($"{checks.Count - failures}/{checks.Count} checks passed.");
		return failures == 0 ? 0 : 1;
	}

	private static void BufferedValues()
	{
		using var directory = new TestDirectory();
		using var storage = directory.Open();
		Assert(!storage.TryGetString("missing", out string? missing) && missing == null, "Missing key contract");
		Assert(storage.Flush(), "Empty flush");
		Assert(storage.SetString("../nested/🔑", "Zażółć gęślą jaźń 🦊\0"), "Accept Unicode");
		Assert(storage.TryGetString("../nested/🔑", out string? value) && value == "Zażółć gęślą jaźń 🦊\0", "Read pending value");
		Assert(storage.SetString("empty", "") && storage.TryGetString("empty", out value) && value == "", "Empty is not missing");
		Assert(storage.HasPendingChanges && !File.Exists(directory.Snapshot), "No snapshot before flush");
		Assert(Directory.GetFiles(storage.StorageDirectory).Length == 1, "Only ownership lock exists before flush");
	}

	private static void Persistence()
	{
		using var directory = new TestDirectory();
		string largeValue = new string('x', 150000);
		using (var storage = directory.Open())
		{
			Assert(storage.SetString("private-key-name", "private-value-content"), "Set");
			storage.SetString("large", largeValue);
			storage.SetString("empty", "");
			storage.SetString("unicode", "日本語 🦊");
			Assert(storage.Flush() && !storage.HasPendingChanges, "Persist");
			string disk = Encoding.UTF8.GetString(File.ReadAllBytes(directory.Snapshot));
			Assert(!disk.Contains("private-key-name") && !disk.Contains("private-value-content"), "Keys and values encrypted");
			byte[] before = File.ReadAllBytes(directory.Snapshot);
			Assert(storage.Flush() && before.SequenceEqual(File.ReadAllBytes(directory.Snapshot)), "Clean flush does not rewrite");
		}
		using var reopened = directory.Open();
		Assert(reopened.TryGetString("private-key-name", out string? value) && value == "private-value-content", "Restore");
		Assert(reopened.TryGetString("large", out value) && value == largeValue, "Large value");
		Assert(reopened.TryGetString("empty", out value) && value == "", "Empty value");
		Assert(reopened.TryGetString("unicode", out value) && value == "日本語 🦊", "Unicode value");
	}

	private static void DisposeDoesNotFlush()
	{
		using var directory = new TestDirectory();
		using (var storage = directory.Open()) { storage.SetString("key", "original"); Assert(storage.Flush(), "Initial flush"); storage.SetString("key", "unsaved"); }
		using var reopened = directory.Open();
		Assert(reopened.TryGetString("key", out string? value) && value == "original", "Unflushed update discarded");
	}

	private static void Deletion()
	{
		using var directory = new TestDirectory();
		using (var storage = directory.Open())
		{
			storage.SetString("one", "1"); storage.SetString("two", "2"); Assert(storage.Flush(), "Initial flush");
			Assert(storage.DeleteKey("one") && !storage.TryGetString("one", out _), "Pending deletion visible");
			Assert(storage.DeleteKey("absent"), "Absent deletion succeeds");
			Assert(storage.Flush(), "Delete flush");
		}
		using (var storage = directory.Open())
		{
			Assert(!storage.TryGetString("one", out _) && storage.TryGetString("two", out string? value) && value == "2", "Deletion persisted without affecting other keys");
			Assert(storage.DeleteKey("two") && storage.Flush() && !File.Exists(directory.Snapshot), "Remove final snapshot");
		}
		using var empty = directory.Open();
		Assert(!empty.TryGetString("two", out _), "No resurrection");
	}

	private static void FailedWrite()
	{
		using var directory = new TestDirectory();
		using var storage = directory.Open();
		storage.SetString("key", "value");
		Directory.CreateDirectory(directory.Snapshot);
		Assert(!storage.Flush() && storage.HasPendingChanges && storage.LastError != null, "Failed write retained");
		Directory.Delete(directory.Snapshot);
		Assert(storage.Flush() && !storage.HasPendingChanges && storage.LastError == null, "Retry succeeds");
		Assert(!Directory.GetFiles(storage.StorageDirectory, "*.tmp").Any(), "Temporary files cleaned up");
	}

	private static void FailedReplacement()
	{
		using var directory = new TestDirectory();
		using var storage = directory.Open();
		storage.SetString("key", "original"); Assert(storage.Flush(), "Initial save");
		byte[] original = File.ReadAllBytes(directory.Snapshot);
		storage.SetString("key", "new");
		// Deterministically fail at the replace boundary, after the new file was fully written.
		Throws<IOException>(() => AtomicFile.Write(directory.Snapshot, new byte[] { 1 }, (temporary, destination) =>
		{
			Assert(File.ReadAllBytes(temporary).SequenceEqual(new byte[] { 1 }), "Replacement staged");
			Assert(destination == directory.Snapshot, "Replace correct destination");
			throw new IOException("Simulated replacement failure.");
		}));
		Assert(original.SequenceEqual(File.ReadAllBytes(directory.Snapshot)), "Previous file preserved on failed replacement");
		Assert(!Directory.GetFiles(storage.StorageDirectory, "*.tmp").Any(), "Failed replacement cleaned up");
		Assert(storage.Flush(), "Pending update can commit");
	}

	private static void FailedDelete()
	{
		using var directory = new TestDirectory();
		using var storage = directory.Open();
		storage.SetString("key", "value"); Assert(storage.Flush(), "Initial save"); storage.DeleteKey("key");
		File.Delete(directory.Snapshot); Directory.CreateDirectory(directory.Snapshot);
		Assert(!storage.Flush() && storage.HasPendingChanges, "Failed deletion retained");
		Directory.Delete(directory.Snapshot);
		Assert(storage.Flush() && !storage.HasPendingChanges, "Deletion retry");
	}

	private static void ConstructorReleasesLock()
	{
		using var directory = new TestDirectory();
		directory.Keys.Fail = true;
		Throws<CryptographicException>(() => directory.Open());
		directory.Keys.Fail = false;
		using var storage = directory.Open();
		Assert(storage.Flush(), "Reopen after failed initialization");
	}

	private static void ExclusiveOwner()
	{
		using var directory = new TestDirectory();
		using (var first = directory.Open()) { Throws<IOException>(() => directory.Open()); }
		using var next = directory.Open();
		Assert(next.Flush(), "Lock released on dispose");
	}

	private static void InvalidUsage()
	{
		using var directory = new TestDirectory();
		Throws<ArgumentException>(() => new SecureStorage("", directory.Keys));
		Throws<ArgumentException>(() => new SecureStorage(directory.Path, ""));
		Throws<ArgumentException>(() => new SecureStorage(directory.Path, new string('x', 1025)));
		Throws<ArgumentOutOfRangeException>(() => new SecureStorage(directory.Path, (SecureStoragePlatform)100));
		var storage = directory.Open();
		Throws<ArgumentException>(() => storage.SetString("", "value"));
		Throws<ArgumentException>(() => storage.SetString(new string('x', 1025), "value"));
		Throws<ArgumentNullException>(() => storage.SetString("key", null!));
		storage.Dispose(); storage.Dispose();
		Throws<ObjectDisposedException>(() => storage.Flush());
		Throws<ObjectDisposedException>(() => storage.SetString("key", "value"));
		Throws<ObjectDisposedException>(() => storage.TryGetString("key", out _));
		Throws<ObjectDisposedException>(() => storage.DeleteKey("key"));
	}

	private static void ConcurrentCalls()
	{
		using var directory = new TestDirectory();
		using (var storage = directory.Open())
		{
			Parallel.For(0, 64, i =>
			{
				Assert(storage.SetString("key-" + i, "value-" + i), "Concurrent set");
				Assert(storage.TryGetString("key-" + i, out string? value) && value == "value-" + i, "Concurrent read");
				if (i % 2 == 0) { Assert(storage.DeleteKey("key-" + i), "Concurrent delete"); }
			});
			Assert(storage.Flush(), "Concurrent updates flush");
		}
		using var reopened = directory.Open();
		for (int i = 0; i < 64; i++)
		{
			bool found = reopened.TryGetString("key-" + i, out string? value);
			Assert(i % 2 == 0 ? !found : found && value == "value-" + i, "Concurrent result persisted");
		}
	}

	private static void SizeLimit()
	{
		using var directory = new TestDirectory();
		using var storage = directory.Open();
		storage.SetString("key", "original");
		Assert(!storage.SetString("key", new string('x', SnapshotCipher.MaximumPlaintextLength)), "Oversize rejected");
		Assert(storage.LastError is IOException && storage.TryGetString("key", out string? value) && value == "original", "Old value retained");
		Assert(storage.Flush(), "Existing changes still save");
	}

	private static void Authentication()
	{
		byte[] key = NewKey();
		byte[] envelope = SnapshotCipher.Encrypt(Encoding.UTF8.GetBytes("confidential payload"), key);
		foreach (int index in new[] { 0, 8, 24, envelope.Length - 1 })
		{
			byte[] changed = (byte[])envelope.Clone(); changed[index] ^= 1;
			Throws<CryptographicException>(() => SnapshotCipher.Decrypt(changed, key));
		}
		Throws<CryptographicException>(() => SnapshotCipher.Decrypt(envelope, NewKey()));
		Throws<CryptographicException>(() => SnapshotCipher.Decrypt(envelope.Take(envelope.Length - 1).ToArray(), key));
		Throws<CryptographicException>(() => SnapshotCipher.Decrypt(Array.Empty<byte>(), key));
	}

	private static void CorruptedSnapshot()
	{
		using var directory = new TestDirectory();
		using (var storage = directory.Open()) { storage.SetString("key", "value"); Assert(storage.Flush(), "Initial save"); }
		byte[] corrupted = File.ReadAllBytes(directory.Snapshot); corrupted[30] ^= 1; File.WriteAllBytes(directory.Snapshot, corrupted);
		Throws<CryptographicException>(() => directory.Open());
		Assert(corrupted.SequenceEqual(File.ReadAllBytes(directory.Snapshot)), "Corrupted file left for explicit recovery");
	}

	private static void MissingKey()
	{
		using var directory = new TestDirectory();
		using (var storage = directory.Open()) { storage.SetString("key", "value"); Assert(storage.Flush(), "Initial save"); }
		directory.Keys.Missing = true;
		Throws<CryptographicException>(() => directory.Open());
		Assert(!directory.Keys.LastAllowCreate, "Key creation forbidden for existing ciphertext");
	}

	private static void RandomizedEncryption()
	{
		byte[] key = NewKey(); byte[] data = Encoding.UTF8.GetBytes("same payload");
		byte[] first = SnapshotCipher.Encrypt(data, key); byte[] second = SnapshotCipher.Encrypt(data, key);
		Assert(!first.SequenceEqual(second), "Fresh IV changes ciphertext");
		Assert(data.SequenceEqual(SnapshotCipher.Decrypt(first, key)) && data.SequenceEqual(SnapshotCipher.Decrypt(second, key)), "Both decrypt");
	}

	private static void MalformedSnapshot()
	{
		using var directory = new TestDirectory();
		Directory.CreateDirectory(System.IO.Path.GetDirectoryName(directory.Snapshot)!);
		foreach (byte[] plaintext in new[] { BitConverter.GetBytes(-1), BitConverter.GetBytes(int.MaxValue), new byte[] { 0, 0, 0, 0, 1 } })
		{
			File.WriteAllBytes(directory.Snapshot, SnapshotCipher.Encrypt(plaintext, directory.Keys.Key));
			Throws<InvalidDataException>(() => directory.Open());
		}
	}

	private static void NativeRoundTrip()
	{
		using var directory = new TestDirectory();
		bool apple = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
		if (!apple && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { throw new PlatformNotSupportedException(); }
		string storageDirectory = System.IO.Path.Combine(directory.Path, "SecureStorage");
		try
		{
			using (var storage = new SecureStorage(directory.Path))
			{
				storage.SetString("native", "round trip 🔐"); Assert(storage.Flush(), "Native persist");
			}
			using (var storage = new SecureStorage(directory.Path))
			{
				Assert(storage.TryGetString("native", out string? value) && value == "round trip 🔐", "Native key survives reopen");
				Assert(storage.DeleteKey("native") && storage.Flush(), "Native delete");
				storage.SetString("native", "second snapshot"); Assert(storage.Flush(), "Key reused after empty storage");
			}
			if (apple) { Assert(new AppleKeyStore(false).DeleteKeyForTests(storageDirectory) == 0, "Remove temporary native key"); }
			else { File.Delete(System.IO.Path.Combine(storageDirectory, WindowsKeyStore.KeyFileName)); }
			Throws<CryptographicException>(() => new SecureStorage(directory.Path));
		}
		finally
		{
			if (apple) { new AppleKeyStore(false).DeleteKeyForTests(storageDirectory); }
		}
	}

	private static byte[] NewKey()
	{
		var key = new byte[SnapshotCipher.KeyLength]; RandomNumberGenerator.Fill(key); return key;
	}

	private static void NativeStableIdentity()
	{
		using var original = new TestDirectory(); using var moved = new TestDirectory();
		string identity = "com.beamable.tests." + Guid.NewGuid().ToString("N");
		var keyStore = new AppleKeyStore(false, identity);
		try
		{
			using (var storage = new SecureStorage(original.Path, identity))
			{
				Assert(storage.SetString("upgrade", "retained") && storage.Flush(), "Create stable store");
			}
			Directory.Move(original.Path, moved.Path);
			using (var storage = new SecureStorage(moved.Path, identity))
			{
				Assert(storage.TryGetString("upgrade", out string? value) && value == "retained", "Lookup independent of absolute directory");
			}
			Throws<CryptographicException>(() => new SecureStorage(moved.Path, identity + ".wrong"));
		}
		finally { keyStore.DeleteKeyForTests(System.IO.Path.Combine(moved.Path, "SecureStorage")); }
	}

	private static void NativeLegacyPromotion()
	{
		using var original = new TestDirectory(); using var moved = new TestDirectory();
		string identity = "com.beamable.tests." + Guid.NewGuid().ToString("N");
		string oldDirectory = System.IO.Path.Combine(original.Path, "SecureStorage");
		var legacy = new AppleKeyStore(false); var stable = new AppleKeyStore(false, identity);
		try
		{
			using (var storage = new SecureStorage(original.Path))
			{
				Assert(storage.SetString("upgrade", "original key") && storage.Flush(), "Create old path-derived store");
			}
			using (var promoted = new SecureStorage(original.Path, identity))
			{
				Assert(promoted.TryGetString("upgrade", out string? value) && value == "original key", "Reuse old key via native import");
			}
			Assert(legacy.DeleteKeyForTests(oldDirectory) == 0, "Remove legacy alias to prove stable lookup");
			Directory.Move(original.Path, moved.Path);
			using var reopened = new SecureStorage(moved.Path, identity);
			Assert(reopened.TryGetString("upgrade", out string? restored) && restored == "original key", "Promoted key survives relocation");
		}
		finally
		{
			legacy.DeleteKeyForTests(oldDirectory);
			stable.DeleteKeyForTests(oldDirectory);
		}
	}

	private static void Assert(bool condition, string message)
	{
		if (!condition) { throw new Exception(message); }
	}

	private static void Throws<T>(Action action) where T : Exception
	{
		try { action(); }
		catch (T) { return; }
		throw new Exception("Expected " + typeof(T).Name);
	}

	private sealed class TestKeyStore : IKeyStore
	{
		internal readonly byte[] Key = NewKey();
		internal bool Fail;
		internal bool Missing;
		internal bool LastAllowCreate;

		public byte[] GetKey(string storageDirectory, bool allowCreate)
		{
			LastAllowCreate = allowCreate;
			if (Fail || (Missing && !allowCreate)) { throw new CryptographicException("Test key unavailable."); }
			return (byte[])Key.Clone();
		}
	}

	private sealed class TestDirectory : IDisposable
	{
		internal readonly string Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beamable-storage-tests-" + Guid.NewGuid().ToString("N"));
		internal readonly TestKeyStore Keys = new TestKeyStore();
		internal string Snapshot => System.IO.Path.Combine(Path, "SecureStorage", SecureStorage.SnapshotFileName);
		internal SecureStorage Open() => new SecureStorage(Path, Keys);
		public void Dispose() { if (Directory.Exists(Path)) { Directory.Delete(Path, recursive: true); } }
	}
}
