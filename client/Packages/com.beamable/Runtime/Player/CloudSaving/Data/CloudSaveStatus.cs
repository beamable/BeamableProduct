namespace Beamable.Player.CloudSaving
{
	/// <summary>Describes the initialization and conflict state of a player's save service.</summary>
	/// <remarks>A completed initialization promise can return a status other than Initialized.
	/// Inspect the value before saving or synchronizing.</remarks>
	public enum CloudSaveStatus
	{
		/// <summary>The service is not initialized; save, load, and forced synchronization operations are unavailable.</summary>
		Inactive,
		/// <summary>The service is ready for local save/load operations and forced cloud synchronization.</summary>
		Initialized,
		/// <summary>Initialization is in progress; save, load, and forced synchronization operations are unavailable.</summary>
		Initializing,
		/// <summary>Unresolved conflicts remain. Local save/load operations are allowed; forced synchronization requires resolution first.</summary>
		ConflictedData,
	}
}
