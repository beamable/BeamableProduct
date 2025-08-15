// this file was copied from nuget package Beamable.Common@5.3.0
// https://www.nuget.org/packages/Beamable.Common/5.3.0

using Beamable.Common.BeamCli;

namespace Beamable.Common.Content
{
	/// <summary>
	/// Filters that can be applied to <see cref="ContentService.FilterLocalContentFiles"/> to get a subset of content files.
	/// </summary>
	[CliContractType]
	public enum ContentFilterType
	{
		/// <summary>
		/// Matches the given array of filters as though they were fully formed ContentIds.
		/// </summary>
		ExactIds,

		/// <summary>
		/// Matches the given array of filters as though they were fully formed ContentTypeIds.
		/// The comparison is a StartsWith so... 'items' will return ANY item or its subclasses.
		/// </summary>
		TypeHierarchy,

		/// <summary>
		/// Matches the given array of filters as though they were fully formed ContentTypeIds.
		/// The comparison is an equals so... 'items' will return only content files that are exactly of the `items` content type (no subclasses).
		/// </summary>
		TypeHierarchyStrict,

		/// <summary>
		/// Matches the given array of filters as though they were C# regexes.
		/// </summary>
		Regexes,

		/// <summary>
		/// Matches the given array of filters as though they were tags.
		/// Any content file that has any of the filter tags will be included in the filtered list.
		/// </summary>
		Tags,
	}
}
