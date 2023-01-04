using cli.Services.Content;

namespace cli.Content;

public class ContentPublishCommand : AppCommand<ContentPublishCommandArgs>
{
	private readonly ContentService _contentService;

	public ContentPublishCommand(ContentService contentService) : base("publish", "Publish content and manifest")
	{
		_contentService = contentService;
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOption("manifestId", "set the manifest to use, 'global' by default"),
			(args, s) => args.ManifestId = s);
	}

	public override async Task Handle(ContentPublishCommandArgs args)
	{
		await _contentService.PublishContentAndManifest(args.ManifestId);
	}
}

public class ContentPublishCommandArgs : ContentCommandArgs
{
}
