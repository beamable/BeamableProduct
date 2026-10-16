namespace Beamable.Security
{
	/// <summary>Selects the native key protection backend, independently of Unity compilation symbols.</summary>
	public enum SecureStoragePlatform
	{
		/// <summary>Detect Windows, macOS, or iOS from the runtime.</summary>
		Auto,
		/// <summary>Windows DPAPI, scoped to the current Windows user.</summary>
		Windows,
		/// <summary>Apple Keychain through the accompanying macOS dynamic library.</summary>
		MacOS,
		/// <summary>Apple Keychain through the statically linked iOS bridge.</summary>
		IOS
	}
}
