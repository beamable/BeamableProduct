using cli.Services.Content;
using System.CommandLine;
using System.Diagnostics;

namespace cli.Content.Tag;

public class ContentTagCommand : CommandGroup<ContentTagCommandArgs>
{
	public static readonly Option<string> FILTER_OPTION =
		new("filter", "Accepts different strings to filter which content files will be affected. See the `filter-type` option.");

	public static readonly Argument<string> TAG_ARGUMENT =
		new("tag", "List of tags for us to affect");


	public ContentTagCommand() : base("tag", "Commands for adding/removing tags")
	{
	}
}

public class ContentTagCommandArgs : ContentCommandArgs
{
}
