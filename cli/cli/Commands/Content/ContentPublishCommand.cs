using cli.Services.Content;

namespace cli.Content;

public class ContentPublishCommand : AppCommand<ContentPublishCommandArgs>
{
	private ContentService _contentService;

	public ContentPublishCommand() : base("publish", "Publish content and manifest")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFEST_OPTION,
			(args, s) => args.ManifestId = s);
	}

	public override async Task Handle(ContentPublishCommandArgs args)
	{
		_contentService = args.ContentService;

		await _contentService.PublishContentAndManifest(args.ManifestId);
	}
}

public class ContentPublishCommandArgs : ContentCommandArgs
{
}
