using cli.Services.Content;

namespace cli.Content;

public class ContentResetCommand : AppCommand<ContentResetCommandArgs>
{
	private ContentService _contentService;

	public ContentResetCommand() : base("reset", "Sets local content to match remote one")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOption("manifest-id", "Set the manifest to use, 'global' by default"),
			(args, s) => args.ManifestId = s);
	}

	public override async Task Handle(ContentResetCommandArgs args)
	{
		_contentService = args.ContentService;

		var manifest = await _contentService.GetManifest(args.ManifestId);
		_contentService.UpdateTags(manifest);
		var _ = await _contentService.PullContent(manifest);
		var localContent = _contentService.ContentLocal.GetLocalContentStatus(manifest).Where(content => content.status == ContentStatus.Created);
		foreach (LocalContent content in localContent) _contentService.ContentLocal.Remove(content);

	}
}

public class ContentResetCommandArgs : ContentCommandArgs
{
}
