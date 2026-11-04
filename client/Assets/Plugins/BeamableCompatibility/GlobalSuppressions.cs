using System.Diagnostics.CodeAnalysis;

// Steamworks.NET 15.0.0 lives in Assembly-CSharp-firstpass, separately from the SDK.
// Preserve its native DLL/callback architecture selection without modifying vendor files.
// SteamworksCompatibilityTests guards against suppressing unrelated firstpass code.
[assembly: SuppressMessage("PreprocessorDirectives", "UAC0008", Scope = "module",
    Justification = "Keep the vendored Steamworks.NET architecture selection until the dependency is upgraded.")]

// The vendor's OptionValue union is not Serializable. Preserve its original declaration.
[assembly: SuppressMessage("Serialization", "UAC1001", Scope = "member",
    Target = "~F:Steamworks.SteamNetworkingConfigValue_t.m_val",
    Justification = "Native interop union; its type is intentionally not Unity-serializable.")]
