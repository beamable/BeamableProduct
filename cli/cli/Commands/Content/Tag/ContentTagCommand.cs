using System.CommandLine;
using System.Diagnostics;

namespace cli.Content.Tag;

public class ContentTagCommand : CommandGroup<ContentTagCommandArgs>
{
	public static readonly Argument<string> TAG_ARGUMENT =
		new("tag", "List of tags for us to affect");


	public ContentTagCommand() : base("tag", "Commands for adding/removing tags")
	{
	}
}

public class ContentTagCommandArgs : ContentCommandArgs
{
}
