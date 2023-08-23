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
		AddOption(ContentCommand.MANIFEST_OPTION,
			(args, s) => args.ManifestId = s);
	}

	public override async Task Handle(ContentResetCommandArgs args)
	{
		_contentService = args.ContentService;

		var manifest = await _contentService.GetManifest(args.ManifestId);
		_contentService.UpdateTags(manifest, args.ManifestId);
		_ = await _contentService.PullContent(manifest, args.ManifestId);
		var localCache = _contentService.GetLocalCache(args.ManifestId);
		var localContent = localCache.GetLocalContentStatus().Where(content => content.status == ContentStatus.Created);
		foreach (LocalContent content in localContent) localCache.Remove(content);

	}
}

public class ContentResetCommandArgs : ContentCommandArgs
{
}
