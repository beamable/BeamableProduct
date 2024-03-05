using cli.Services.Content;
using System.CommandLine;
using System.Diagnostics;

namespace cli.Content.Tag;

public class ContentTagCommand : CommandGroup<ContentTagCommandArgs>
{
	public static readonly ConfigurableOptionFlag REGEX_OPTION =
		new("treat-as-regex", "Treat content argument as regex pattern");
	public static readonly Argument<string> TAG_ARGUMENT = new Argument<string>("tag", "Tag argument");

	public static readonly Argument<string> CONTENT_ARGUMENT =
		new Argument<string>("content", "Accepts content ids separated by commas or regex pattern when used in combination with \"treat-as-regex\" option");
	private ContentService _contentService;

	public ContentTagCommand() : base("tag", "Opens tags file")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFEST_OPTION, (args, s) => args.ManifestId = s);
	}

	public override Task Handle(ContentTagCommandArgs args)
	{
		_contentService = args.ContentService;
		new Process
		{
			StartInfo = new ProcessStartInfo(_contentService.GetLocalCache(args.ManifestId).Tags.GetPath()) { UseShellExecute = true }
		}.Start();
		return Task.CompletedTask;
	}
}

public class ContentTagCommandArgs : ContentCommandArgs
{
}
