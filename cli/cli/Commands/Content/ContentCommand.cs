using Beamable.Common.Content;
using System.CommandLine;
using System.Diagnostics;

namespace cli.Content;

public class ContentCommand : CommandGroup
{
	public static readonly Option<string[]> MANIFESTS_FILTER_OPTION =
		new("--manifest-ids", () => new[] { "global" }, "Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest");

	
	public static readonly Option<string> FILTER_OPTION =
		new("--filter", () => "", "Accepts different strings to filter which content files will be affected. See the `filter-type` option");

	public static readonly Option<ContentFilterType> FILTER_TYPE_OPTION =
		new("--filter-type",
			() => ContentFilterType.ExactIds,
			"Defines the semantics for the `filter` argument. When no filters are given, affects all existing content." +
			$"\n{nameof(ContentFilterType.ExactIds)} => Will only add the given tags to the ','-separated list of filters" +
			$"\n{nameof(ContentFilterType.Regexes)} => Will add the given tags to any content whose Id is matched by any of the ','-separated list of filters (C# regex string)" +
			$"\n{nameof(ContentFilterType.TypeHierarchy)} => Will add the given tags to any content of the ','-separated list of filters (content type strings with full hierarchy --- StartsWith comparison)" +
			$"\n{nameof(ContentFilterType.Tags)} => Will add the given tags to any content that currently has any of the ','-separated list of filters (tags)");

	public ContentCommand() : base("content", "Open content folder in file explorer")
	{
	}
}

public class ContentCommandArgs : CommandArgs
{
	public string[] ManifestIds;
}
