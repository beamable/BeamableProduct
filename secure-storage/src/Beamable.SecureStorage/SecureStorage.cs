using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Beamable.Security.Internal;

namespace Beamable.Security
{
	/// <summary>
	/// Owns one encrypted snapshot below the supplied directory. Changes are buffered until Flush.
	/// One instance owns the directory at a time; calls on that instance are synchronized.
	/// </summary>
	public sealed class SecureStorage : ISecureStorage, IDisposable
	{
		internal const string SnapshotFileName = "values.bss";
		private const int MaximumKeyBytes = 1024;
		private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
		private readonly object _gate = new object();
		private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.Ordinal);
		private readonly string _snapshotPath;
		private readonly FileStream _ownershipLock;
		private readonly byte[] _key;
		private int _serializedSize = sizeof(int);
		private bool _dirty;
		private bool _disposed;
		private Exception? _lastError;

		/// <summary>The library-owned directory, below the constructor's directoryPath.</summary>
		public string StorageDirectory { get; }

		/// <summary>The last operational error, or null after a successful call or a missing-key read.</summary>
		public Exception? LastError { get { lock (_gate) { return _lastError; } } }

		/// <summary>Whether accepted updates or deletions have not yet been flushed.</summary>
		public bool HasPendingChanges { get { lock (_gate) { return _dirty; } } }

		/// <summary>
		/// Creates or opens storage. Throws if initialization, native key access, or authentication fails.
		/// No plaintext fallback is provided. Desktop compatibility overload; on iOS use the storeId overload.
		/// </summary>
		public SecureStorage(string directoryPath, SecureStoragePlatform platform = SecureStoragePlatform.Auto)
			: this(directoryPath, KeyStoreFactory.Create(platform))
		{
		}

		/// <summary>
		/// Opens a store using a stable application-qualified identity for Apple Keychain lookup.
		/// Keep storeId unchanged when the application container moves. Required for iOS.
		/// </summary>
		public SecureStorage(string directoryPath, string storeId, SecureStoragePlatform platform = SecureStoragePlatform.Auto)
			: this(directoryPath, KeyStoreFactory.Create(platform, ValidateStoreId(storeId)))
		{
		}

		internal SecureStorage(string directoryPath, IKeyStore keyStore)
		{
			if (string.IsNullOrWhiteSpace(directoryPath)) { throw new ArgumentException("A storage directory is required.", nameof(directoryPath)); }
			StorageDirectory = Path.Combine(Path.GetFullPath(directoryPath), "SecureStorage");
			Directory.CreateDirectory(StorageDirectory);
			_snapshotPath = Path.Combine(StorageDirectory, SnapshotFileName);
			// Keep this file in place after releasing the lock; deleting it could allow two
			// callers to lock different file identities for the same directory.
			_ownershipLock = new FileStream(Path.Combine(StorageDirectory, "owner.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
			_key = Array.Empty<byte>();
			try
			{
				bool snapshotExists = File.Exists(_snapshotPath);
				_key = keyStore.GetKey(StorageDirectory, allowCreate: !snapshotExists);
				if (_key.Length != SnapshotCipher.KeyLength) { throw new CryptographicException("The storage key has an invalid length."); }
				if (snapshotExists)
				{
					byte[] envelope = ReadBoundedFile(_snapshotPath, SnapshotCipher.MaximumFileLength);
					byte[] plaintext = SnapshotCipher.Decrypt(envelope, _key);
					try
					{
						if (plaintext.Length > SnapshotCipher.MaximumPlaintextLength) { throw new InvalidDataException("Snapshot is too large."); }
						LoadSnapshot(plaintext);
					}
					finally { Array.Clear(plaintext, 0, plaintext.Length); }
				}
			}
			catch
			{
				Array.Clear(_key, 0, _key.Length);
				_ownershipLock.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public bool SetString(string key, string value)
		{
			int keyBytes = ValidateKey(key);
			if (value == null) { throw new ArgumentNullException(nameof(value)); }
			int valueBytes = Utf8.GetByteCount(value);
			lock (_gate)
			{
				ThrowIfDisposed();
				bool exists = _values.TryGetValue(key, out string? previous);
				long nextSize = exists
					? (long)_serializedSize - Utf8.GetByteCount(previous!) + valueBytes
					: (long)_serializedSize + 8 + keyBytes + valueBytes;
				if (nextSize > SnapshotCipher.MaximumPlaintextLength)
				{
					_lastError = new IOException("Storage exceeds the 16 MiB snapshot limit.");
					return false;
				}
				if (!exists || !StringComparer.Ordinal.Equals(previous, value))
				{
					_values[key] = value;
					_serializedSize = (int)nextSize;
					_dirty = true;
				}
				_lastError = null;
				return true;
			}
		}

		/// <inheritdoc />
		public bool TryGetString(string key, out string? value)
		{
			ValidateKey(key);
			lock (_gate)
			{
				ThrowIfDisposed();
				_lastError = null;
				return _values.TryGetValue(key, out value);
			}
		}

		/// <inheritdoc />
		public bool DeleteKey(string key)
		{
			int keyBytes = ValidateKey(key);
			lock (_gate)
			{
				ThrowIfDisposed();
				if (_values.TryGetValue(key, out string? previous))
				{
					_values.Remove(key);
					_serializedSize -= 8 + keyBytes + Utf8.GetByteCount(previous);
					_dirty = true;
				}
				_lastError = null;
				return true;
			}
		}

		/// <inheritdoc />
		public bool Flush()
		{
			lock (_gate)
			{
				ThrowIfDisposed();
				if (!_dirty) { _lastError = null; return true; }
				try
				{
					if (_values.Count == 0)
					{
						File.Delete(_snapshotPath);
					}
					else
					{
						byte[] plaintext = SerializeSnapshot();
						try { AtomicFile.Write(_snapshotPath, SnapshotCipher.Encrypt(plaintext, _key)); }
						finally { Array.Clear(plaintext, 0, plaintext.Length); }
					}
					_dirty = false;
					_lastError = null;
					return true;
				}
				catch (Exception exception) when (IsOperationalError(exception))
				{
					_lastError = exception;
					return false;
				}
			}
		}

		/// <summary>Releases the directory lock and clears key bytes. Does not flush pending changes.</summary>
		public void Dispose()
		{
			lock (_gate)
			{
				if (_disposed) { return; }
				_disposed = true;
				_values.Clear();
				Array.Clear(_key, 0, _key.Length);
				_ownershipLock.Dispose();
			}
		}

		internal static byte[] ReadBoundedFile(string path, int maximumLength)
		{
			using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				if (stream.Length > maximumLength) { throw new InvalidDataException("Secure storage file is too large."); }
				byte[] bytes = new byte[(int)stream.Length];
				int offset = 0;
				while (offset < bytes.Length)
				{
					int count = stream.Read(bytes, offset, bytes.Length - offset);
					if (count == 0) { throw new EndOfStreamException(); }
					offset += count;
				}
				return bytes;
			}
		}

		private byte[] SerializeSnapshot()
		{
			using (var stream = new MemoryStream(_serializedSize))
			{
				try
				{
					using (var writer = new BinaryWriter(stream, Utf8, leaveOpen: true))
					{
						writer.Write(_values.Count);
						foreach (KeyValuePair<string, string> pair in _values)
						{
							WriteString(writer, pair.Key);
							WriteString(writer, pair.Value);
						}
					}
					return stream.ToArray();
				}
				finally
				{
					Array.Clear(stream.GetBuffer(), 0, (int)stream.Length);
				}
			}
		}

		private void LoadSnapshot(byte[] plaintext)
		{
			using (var stream = new MemoryStream(plaintext, writable: false))
			using (var reader = new BinaryReader(stream, Utf8))
			{
				int count = reader.ReadInt32();
				if (count < 0 || count > (stream.Length - 4) / 8) { throw new InvalidDataException("Invalid entry count."); }
				for (int i = 0; i < count; i++)
				{
					string key = ReadString(reader, MaximumKeyBytes);
					string value = ReadString(reader, SnapshotCipher.MaximumPlaintextLength);
					if (key.Length == 0 || _values.ContainsKey(key)) { throw new InvalidDataException("Invalid or duplicate storage key."); }
					_values.Add(key, value);
				}
				if (stream.Position != stream.Length) { throw new InvalidDataException("Unexpected data after snapshot."); }
				_serializedSize = plaintext.Length;
			}
		}

		private static void WriteString(BinaryWriter writer, string value)
		{
			byte[] bytes = Utf8.GetBytes(value);
			try { writer.Write(bytes.Length); writer.Write(bytes); }
			finally { Array.Clear(bytes, 0, bytes.Length); }
		}

		private static string ReadString(BinaryReader reader, int limit)
		{
			int length = reader.ReadInt32();
			if (length < 0 || length > limit || length > reader.BaseStream.Length - reader.BaseStream.Position)
			{
				throw new InvalidDataException("Invalid string length.");
			}
			byte[] bytes = reader.ReadBytes(length);
			try { return Utf8.GetString(bytes); }
			finally { Array.Clear(bytes, 0, bytes.Length); }
		}

		private static int ValidateKey(string key)
		{
			if (string.IsNullOrEmpty(key)) { throw new ArgumentException("A nonempty storage key is required.", nameof(key)); }
			int length = Utf8.GetByteCount(key);
			if (length > MaximumKeyBytes) { throw new ArgumentException("Storage keys may contain at most 1024 UTF-8 bytes.", nameof(key)); }
			return length;
		}

		private static string ValidateStoreId(string storeId)
		{
			if (string.IsNullOrWhiteSpace(storeId) || Utf8.GetByteCount(storeId) > 1024)
			{
				throw new ArgumentException("A stable, nonempty store identity of at most 1024 UTF-8 bytes is required.", nameof(storeId));
			}
			return storeId;
		}

		private static bool IsOperationalError(Exception exception)
		{
			return exception is IOException || exception is UnauthorizedAccessException ||
				exception is CryptographicException || exception is SecurityException || exception is NotSupportedException;
		}

		private void ThrowIfDisposed()
		{
			if (_disposed) { throw new ObjectDisposedException(nameof(SecureStorage)); }
		}
	}
}
