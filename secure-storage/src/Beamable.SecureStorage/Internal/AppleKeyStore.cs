using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Beamable.Security.Internal
{
	internal sealed class AppleKeyStore : IKeyStore
	{
		private const string Service = "com.beamable.secure-storage.v1";
		private const int ItemNotFound = -25300;
		private readonly string? _storeId;
#if BEAMABLE_SECURE_STORAGE_IOS
		private const string NativeLibrary = "__Internal";
#else
		private const string NativeLibrary = "BeamableSecureStorage";
#endif

		internal AppleKeyStore(bool isIOS, string? storeId = null)
		{
			_storeId = storeId;
#if BEAMABLE_SECURE_STORAGE_IOS
			if (!isIOS) { throw new PlatformNotSupportedException("This DLL was built for iOS only."); }
			if (string.IsNullOrWhiteSpace(storeId)) { throw new ArgumentException("iOS requires a stable storeId independent of the container directory.", nameof(storeId)); }
#else
			if (isIOS) { throw new PlatformNotSupportedException("Use the iOS build of Beamable.SecureStorage.dll on iOS."); }
#endif
		}

		public byte[] GetKey(string storageDirectory, bool allowCreate)
		{
			string account = GetAccount(_storeId == null ? storageDirectory : "logical-store:" + _storeId);
			IntPtr bytes = IntPtr.Zero;
			int length = 0;
			try
			{
				int status = GetNativeKey(Service, account, 0, out bytes, out length);
				if (status == ItemNotFound && !allowCreate && _storeId != null)
				{
					// Upgrade from the original path-derived account while that old path is still
					// reachable. Copy the protected key, never regenerate over existing ciphertext.
					int legacyStatus = GetNativeKey(Service, GetAccount(storageDirectory), 0, out bytes, out length);
					if (legacyStatus == 0)
					{
						try { status = ImportNativeKey(Service, account, bytes, length); }
						finally { FreeNativeKey(bytes, length); bytes = IntPtr.Zero; length = 0; }
						if (status == 0) { status = GetNativeKey(Service, account, 0, out bytes, out length); }
					}
					else { status = legacyStatus; }
				}
				if (status == ItemNotFound && allowCreate)
				{
					status = GetNativeKey(Service, account, 1, out bytes, out length);
				}
				if (status != 0) { throw new CryptographicException("Apple Keychain operation failed (OSStatus " + status + ")."); }
				if (length != SnapshotCipher.KeyLength || bytes == IntPtr.Zero) { throw new CryptographicException("Apple Keychain returned an invalid key."); }
				var result = new byte[length];
				Marshal.Copy(bytes, result, 0, length);
				return result;
			}
			finally
			{
				if (bytes != IntPtr.Zero)
				{
					FreeNativeKey(bytes, length);
				}
			}
		}

		// Only used by native integration checks to remove their own temporary Keychain items.
		internal int DeleteKeyForTests(string storageDirectory)
		{
			return DeleteNativeKey(Service, GetAccount(_storeId == null ? storageDirectory : "logical-store:" + _storeId));
		}

		private static string GetAccount(string identity)
		{
			using (SHA256 sha = SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(identity));
				return BitConverter.ToString(hash).Replace("-", string.Empty);
			}
		}

		[DllImport(NativeLibrary, EntryPoint = "BSS_GetKey", CallingConvention = CallingConvention.Cdecl)]
		private static extern int GetNativeKey([MarshalAs(UnmanagedType.LPStr)] string service, [MarshalAs(UnmanagedType.LPStr)] string account,
			int allowCreate, out IntPtr data, out int length);
		[DllImport(NativeLibrary, EntryPoint = "BSS_FreeKey", CallingConvention = CallingConvention.Cdecl)]
		private static extern void FreeNativeKey(IntPtr data, int length);
		[DllImport(NativeLibrary, EntryPoint = "BSS_ImportKey", CallingConvention = CallingConvention.Cdecl)]
		private static extern int ImportNativeKey([MarshalAs(UnmanagedType.LPStr)] string service, [MarshalAs(UnmanagedType.LPStr)] string account,
			IntPtr data, int length);
		[DllImport(NativeLibrary, EntryPoint = "BSS_DeleteKey", CallingConvention = CallingConvention.Cdecl)]
		private static extern int DeleteNativeKey([MarshalAs(UnmanagedType.LPStr)] string service, [MarshalAs(UnmanagedType.LPStr)] string account);
	}
}
