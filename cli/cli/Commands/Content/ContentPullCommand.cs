using cli.Services;

namespace cli.Content;

public class ContentPullCommand : AppCommand<ContentPullCommandArgs>
{
	private readonly ContentService _contentService;
	public ContentPullCommand(ContentService contentService) : base("pull", "Pulls currently deployed content")
	{
		_contentService = contentService;
	}

	public override async Task Handle(ContentPullCommandArgs args)
	{
		var manifest = await _contentService.GetManifest(args.ManifestId);
		_contentService.UpdateTags(manifest);
		var result = await _contentService.PullContent(manifest);
		// var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
		// BeamableLogger.Log(json);
	}

	public override void Configure()
	{
	}
}

public class ContentPullCommandArgs : ContentCommandArgs
{
}
