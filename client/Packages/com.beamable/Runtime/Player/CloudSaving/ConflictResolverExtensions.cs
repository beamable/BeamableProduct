using System.Collections.Generic;

namespace Beamable.Player.CloudSaving
{
	/// <summary>
	/// Provides extension methods for <see cref="IConflictResolver"/> to simplify conflict resolution.
	/// </summary>
	public static class ConflictResolverExtensions
	{
		/// <summary>
		/// Resolves all conflicts using the specified resolution type.
		/// </summary>
		/// <param name="resolver">The conflict resolver instance.</param>
		/// <param name="resolveType">The resolution strategy to apply to all conflicts.</param>
		public static void ResolveAll(this IConflictResolver resolver, ConflictResolveType resolveType)
		{
			List<DataConflictDetail> conflicts = new(resolver.Conflicts);
			foreach (var conflict in conflicts)
			{
				resolver.Resolve(conflict, resolveType);
			}
		}

		/// <summary>
		/// Resolves all conflicts by keeping the cloud version of the data.
		/// </summary>
		/// <param name="resolver">The conflict resolver instance.</param>
		public static void UseCloudForAll(this IConflictResolver resolver)
		{
			resolver.ResolveAll(ConflictResolveType.UseCloud);
		}

		/// <summary>
		/// Resolves all conflicts by keeping the local version of the data.
		/// </summary>
		/// <param name="resolver">The conflict resolver instance.</param>
		public static void UseLocalForAll(this IConflictResolver resolver)
		{
			resolver.ResolveAll(ConflictResolveType.UseLocal);
		}

		/// <summary>
		/// Resolves all conflicts by selecting the most recently modified version (local or cloud).
		/// </summary>
		/// <param name="resolver">The conflict resolver instance.</param>
		public static void ResolveAllByNewest(this IConflictResolver resolver)
		{
			List<DataConflictDetail> conflicts = new(resolver.Conflicts);
			foreach (DataConflictDetail conflictDetail in conflicts)
			{
				bool isLocalMoreRecent = conflictDetail.LocalSaveEntry.lastModified >
				                         conflictDetail.CloudSaveEntry.lastModified;
				var resolveType = isLocalMoreRecent ? ConflictResolveType.UseLocal : ConflictResolveType.UseCloud;
				resolver.Resolve(conflictDetail, resolveType);
			}
		}
	}
}
