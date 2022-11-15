using cli.Services;
using System.CommandLine;

namespace cli.Content;

public class ContentStatusCommand : AppCommand<ContentStatusCommandArgs>
{
	private readonly ContentService _contentService;
	public ContentStatusCommand(ContentService contentService) : base("status", "Show current status of the content")
	{
		_contentService = contentService;
	}

	public override void Configure()
	{
		AddOption(new Option<string>("manifestId", "set the manifest to use, 'global' by default"),
			(args, s) => args.ManifestId = s);
	}

	public override async Task Handle(ContentStatusCommandArgs args)
	{
		await _contentService.DisplayStatusTable();
	}
}

public class ContentStatusCommandArgs : ContentCommandArgs
{
}
