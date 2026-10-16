namespace Beamable.Player.CloudSaving
{
	/// <summary>Selects the file version to keep when resolving a save conflict.</summary>
	/// <remarks>The other version is replaced. Obtain an explicit choice from the player or the game's conflict policy.</remarks>
	public enum ConflictResolveType
	{
		/// <summary>Keeps the local file and schedules its version for cloud synchronization.</summary>
		UseLocal,
		/// <summary>Keeps the downloaded cloud version and replaces the local file.</summary>
		UseCloud,
	}
}
