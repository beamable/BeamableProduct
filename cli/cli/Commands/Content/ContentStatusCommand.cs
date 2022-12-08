using cli.Services;
using cli.Services.Content;
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
		AddOption(new ConfigurableOption("manifestId", "set the manifest to use, 'global' by default"),
			(args, s) => args.ManifestId = s);
		AddOption(new ConfigurableOptionFlag("skipUpToDate", "Skip displaying up to date content"),
			(args, b) => args.skipUpToDate = b);
		AddOption(new ConfigurableIntOption("limitDisplay", "Limit content displayed amount"),
			(args, s) => args.limitDisplay = s);
	}

	public override async Task Handle(ContentStatusCommandArgs args)
	{
		await _contentService.DisplayStatusTable(args.ManifestId,args.skipUpToDate, args.limitDisplay);
	}
}

public class ContentStatusCommandArgs : ContentCommandArgs
{
	public bool skipUpToDate;
	public int limitDisplay;
}
