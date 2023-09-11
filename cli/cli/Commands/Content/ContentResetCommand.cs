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
		var localCache = _contentService.GetLocalCache(args.ManifestId);

		await localCache.UpdateTags();
		_ = await localCache.PullContent();
		await localCache.RemoveLocalOnlyContent();
	}
}

public class ContentResetCommandArgs : ContentCommandArgs
{
}
