using System;
using System.Runtime.InteropServices;

namespace Beamable.Security.Internal
{
	internal static class KeyStoreFactory
	{
		internal static IKeyStore Create(SecureStoragePlatform platform, string? storeId = null)
		{
			if (platform == SecureStoragePlatform.Auto)
			{
#if BEAMABLE_SECURE_STORAGE_IOS
				platform = SecureStoragePlatform.IOS;
#else
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { platform = SecureStoragePlatform.Windows; }
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { platform = SecureStoragePlatform.MacOS; }
				else { throw new PlatformNotSupportedException("Secure storage supports Windows, macOS, and iOS. No insecure fallback is available."); }
#endif
			}
#if BEAMABLE_SECURE_STORAGE_IOS
			if (platform != SecureStoragePlatform.IOS) { throw new PlatformNotSupportedException("This DLL was built for iOS only."); }
#else
			if (platform == SecureStoragePlatform.IOS) { throw new PlatformNotSupportedException("Use the iOS build of Beamable.SecureStorage.dll on iOS."); }
#endif
			switch (platform)
			{
				case SecureStoragePlatform.Windows: return new WindowsKeyStore();
				case SecureStoragePlatform.MacOS: return new AppleKeyStore(isIOS: false, storeId);
				case SecureStoragePlatform.IOS: return new AppleKeyStore(isIOS: true, storeId);
				default: throw new ArgumentOutOfRangeException(nameof(platform));
			}
		}
	}
}
