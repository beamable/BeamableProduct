using cli.Services.Content;

namespace cli.Content;

public class ContentStatusCommand : AppCommand<ContentStatusCommandArgs>
{
	private ContentService _contentService;

	public ContentStatusCommand() : base("status", "Show current status of the content")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIds = s);
		AddOption(new ConfigurableOptionFlag("show-up-to-date", "Show up to date content"), (args, b) => args.showUpToDate = b);
		AddOption(new ConfigurableIntOption(nameof(ContentStatusCommandArgs.limit), "Limit content displayed amount (default: 100)"), (args, s) => args.limit = s);
		AddOption(new ConfigurableIntOption(nameof(ContentStatusCommandArgs.skip), "Skips content amount"), (args, s) => args.skip = s);
	}

	public override async Task Handle(ContentStatusCommandArgs args)
	{
		_contentService = args.ContentService;

		if (args.ManifestIds.Length == 0)
			args.ManifestIds = new[] { "global" };
		
		foreach (string id in args.ManifestIds) 
			await _contentService.DisplayStatusTable(id, args.showUpToDate, args.limit, args.skip);
	}
}

public class ContentStatusCommandArgs : ContentCommandArgs
{
	public bool showUpToDate;
	public int limit;
	public int skip;
}
