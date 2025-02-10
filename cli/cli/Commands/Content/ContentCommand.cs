using cli.Services.Content;
using System.CommandLine;
using System.Diagnostics;

namespace cli.Content;

public class ContentCommand : CommandGroup
{
	public static readonly Option<string[]> MANIFESTS_FILTER_OPTION =
		new("--manifest-ids", Array.Empty<string>, "Inform a subset of ','-separated manifest ids for which to return data. By default, will return all manifests");
	public static readonly Option<string[]> CONTENT_IDS_FILTER_OPTION =
		new("--content-ids", Array.Empty<string>, "Inform a subset of ','-separated content ids for which to return data. By default, will return all content");

	public ContentCommand() : base("content", "Open content folder in file explorer")
	{
	}
}

public class ContentCommandArgs : CommandArgs
{
	public string[] ManifestIds;
}
