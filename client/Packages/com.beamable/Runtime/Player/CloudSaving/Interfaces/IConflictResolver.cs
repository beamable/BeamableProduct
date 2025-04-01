using System.Collections.Generic;

namespace Beamable.Player.CloudSaving
{
	public interface IConflictResolver
	{
		/// <summary>
		/// Gets a read-only list of all detected data conflicts.
		/// <para>
		/// Each entry in the list contains details about a specific conflict, including the conflicting file and its metadata.
		/// </para>
		/// </summary>
		IReadOnlyList<DataConflictDetail> Conflicts { get; }

		/// <summary>
		/// Resolves a specific data conflict by determining whether to keep the local or cloud version of the file.
		/// </summary>
		/// <param name="conflictDetail">The details of the conflict to resolve.</param>
		/// <param name="resolveType">The resolution strategy, specifying whether to keep the local or cloud version.</param>
		void Resolve(DataConflictDetail conflictDetail, ConflictResolveType resolveType);
	}
}
