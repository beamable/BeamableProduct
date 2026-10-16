using System;
using System.IO;
using System.Security.Cryptography;

namespace Beamable.Security.Internal
{
	internal static class SnapshotCipher
	{
		// Versioned envelope: magic/version (8), random IV (16), AES-CBC ciphertext, HMAC (32).
		// Independent random 256-bit encryption/MAC keys avoid relying on AesGcm availability in Unity.
		private static readonly byte[] Header = { 66, 83, 83, 84, 79, 82, 69, 1 };
		internal const int KeyLength = 64;
		internal const int MaximumPlaintextLength = 16 * 1024 * 1024;
		internal const int MaximumFileLength = MaximumPlaintextLength + 72;

		internal static byte[] Encrypt(byte[] plaintext, byte[] key)
		{
			if (plaintext.Length > MaximumPlaintextLength) { throw new IOException("Storage exceeds the 16 MiB snapshot limit."); }
			byte[] encryptionKey = Slice(key, 0, 32);
			byte[] authenticationKey = Slice(key, 32, 32);
			try
			{
				using (Aes aes = Aes.Create())
				{
					aes.Key = encryptionKey;
					aes.Mode = CipherMode.CBC;
					aes.Padding = PaddingMode.PKCS7;
					aes.GenerateIV();
					using (ICryptoTransform encryptor = aes.CreateEncryptor())
					{
						byte[] ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
						byte[] envelope = new byte[Header.Length + 16 + ciphertext.Length + 32];
						Buffer.BlockCopy(Header, 0, envelope, 0, Header.Length);
						Buffer.BlockCopy(aes.IV, 0, envelope, Header.Length, 16);
						Buffer.BlockCopy(ciphertext, 0, envelope, Header.Length + 16, ciphertext.Length);
						using (var hmac = new HMACSHA256(authenticationKey))
						{
							byte[] tag = hmac.ComputeHash(envelope, 0, envelope.Length - 32);
							Buffer.BlockCopy(tag, 0, envelope, envelope.Length - 32, 32);
						}
						return envelope;
					}
				}
			}
			finally
			{
				Array.Clear(encryptionKey, 0, encryptionKey.Length);
				Array.Clear(authenticationKey, 0, authenticationKey.Length);
			}
		}

		internal static byte[] Decrypt(byte[] envelope, byte[] key)
		{
			if (envelope.Length < 72 || envelope.Length > MaximumFileLength || (envelope.Length - 56) % 16 != 0)
			{
				throw new CryptographicException("Invalid secure storage envelope.");
			}
			byte[] encryptionKey = Slice(key, 0, 32);
			byte[] authenticationKey = Slice(key, 32, 32);
			try
			{
				using (var hmac = new HMACSHA256(authenticationKey))
				{
					byte[] expected = hmac.ComputeHash(envelope, 0, envelope.Length - 32);
					int difference = 0;
					for (int i = 0; i < 32; i++) { difference |= expected[i] ^ envelope[envelope.Length - 32 + i]; }
					if (difference != 0) { throw new CryptographicException("Secure storage authentication failed."); }
				}
				for (int i = 0; i < Header.Length; i++)
				{
					if (envelope[i] != Header[i]) { throw new CryptographicException("Unsupported secure storage format."); }
				}
				using (Aes aes = Aes.Create())
				{
					aes.Key = encryptionKey;
					aes.IV = Slice(envelope, Header.Length, 16);
					aes.Mode = CipherMode.CBC;
					aes.Padding = PaddingMode.PKCS7;
					using (ICryptoTransform decryptor = aes.CreateDecryptor())
					{
						return decryptor.TransformFinalBlock(envelope, Header.Length + 16, envelope.Length - 56);
					}
				}
			}
			finally
			{
				Array.Clear(encryptionKey, 0, encryptionKey.Length);
				Array.Clear(authenticationKey, 0, authenticationKey.Length);
			}
		}

		private static byte[] Slice(byte[] source, int offset, int length)
		{
			var result = new byte[length];
			Buffer.BlockCopy(source, offset, result, 0, length);
			return result;
		}
	}
}
