using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Beamable.Security.Internal
{
	internal sealed class WindowsKeyStore : IKeyStore
	{
		internal const string KeyFileName = "key.dpapi";

		public byte[] GetKey(string storageDirectory, bool allowCreate)
		{
			string keyPath = Path.Combine(storageDirectory, KeyFileName);
			if (File.Exists(keyPath))
			{
				return Transform(SecureStorage.ReadBoundedFile(keyPath, 65536), protect: false);
			}
			if (!allowCreate) { throw new CryptographicException("The DPAPI key file is missing. Existing storage will not be overwritten."); }
			byte[] key = new byte[SnapshotCipher.KeyLength];
			try
			{
				using (RandomNumberGenerator random = RandomNumberGenerator.Create()) { random.GetBytes(key); }
				AtomicFile.Write(keyPath, Transform(key, protect: true));
				return key;
			}
			catch
			{
				Array.Clear(key, 0, key.Length);
				throw;
			}
		}

		private static byte[] Transform(byte[] input, bool protect)
		{
			GCHandle pinned = GCHandle.Alloc(input, GCHandleType.Pinned);
			var output = new DataBlob();
			try
			{
				var source = new DataBlob { Length = input.Length, Data = pinned.AddrOfPinnedObject() };
				// UI_FORBIDDEN; deliberately omit LOCAL_MACHINE to use CurrentUser protection.
				bool success = protect
					? CryptProtectData(ref source, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output)
					: CryptUnprotectData(ref source, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output);
				if (!success)
				{
					throw new CryptographicException("Windows DPAPI operation failed.", new Win32Exception(Marshal.GetLastWin32Error()));
				}
				if (output.Length <= 0 || output.Length > 65536 || output.Data == IntPtr.Zero)
				{
					throw new CryptographicException("Windows DPAPI returned invalid data.");
				}
				var result = new byte[output.Length];
				Marshal.Copy(output.Data, result, 0, result.Length);
				return result;
			}
			finally
			{
				pinned.Free();
				if (output.Data != IntPtr.Zero)
				{
					if (!protect && output.Length > 0 && output.Length <= 65536)
					{
						for (int i = 0; i < output.Length; i++) { Marshal.WriteByte(output.Data, i, 0); }
					}
					LocalFree(output.Data);
				}
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct DataBlob
		{
			public int Length;
			public IntPtr Data;
		}

		[DllImport("Crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CryptProtectData(ref DataBlob input, string? description, IntPtr entropy,
			IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);

		[DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CryptUnprotectData(ref DataBlob input, IntPtr description, IntPtr entropy,
			IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);

		[DllImport("Kernel32.dll")]
		private static extern IntPtr LocalFree(IntPtr memory);
	}
}
