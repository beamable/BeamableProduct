# Steamworks compatibility exceptions

This folder belongs to Beamable and must stay outside `Steamworks.NET` when replacing the vendor files.

The repository vendors Steamworks.NET 15.0.0 (Steam SDK 1.51). Its source compiles into `Assembly-CSharp-firstpass`. `GlobalSuppressions.cs` must remain in that same assembly:

- UAC0008 is suppressed for the assembly because it reports on preprocessor directives outside members. This preserves the vendor's DLL and callback architecture selection.
- UAC1001 is suppressed only for `Steamworks.SteamNetworkingConfigValue_t.m_val`. The vendor union lacks `[Serializable]`; no vendor serialization or interop attributes are changed.

`SteamworksCompatibilityTests` checks Unity's actual assembly source list and fails if unrelated sources enter the assembly or these files move into different assemblies. The UAC0008 exception is assembly-wide, not file-specific.

When updating Steamworks.NET:

1. Replace the vendor directory without replacing this sibling folder.
2. Reassess both exceptions against the new source and Unity diagnostics. Remove exceptions that are no longer needed; do not automatically extend them to new diagnostics.
3. Run the compatibility test and compile on the supported Unity versions. If the vendor introduces an assembly definition, revisit suppression placement before proceeding.
4. Validate native library loading, callbacks and player builds separately. Suppressing diagnostics does not establish native compatibility or support for a future removal of `UNITY_64`.
