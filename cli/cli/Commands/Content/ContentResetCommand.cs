using cli.Services.Content;

namespace cli.Content;

public class ContentResetCommand : AtomicCommand<ContentResetCommandArgs, LocalContentState>
{
	private ContentService _contentService;

	public ContentResetCommand() : base("reset", "Sets local content to match remote one")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIdsToReset = s);
	}

	public override async Task<LocalContentState> GetResult(ContentResetCommandArgs args)
	{
		_contentService = args.ContentService;

		// Prepare the filters (makes sure all the given manifests exist.
		var manifestsToReset = await _contentService.PrepareManifestFilter(args.ManifestIdsToReset);

		// Resets the content for all the given manifests
		for (int i = 0; i < manifestsToReset.Length; i++)
		{
			var localCache = _contentService.GetLocalCache(manifestsToReset[i]);

			await localCache.UpdateTags();
			_ = await localCache.PullContent();
			await localCache.RemoveLocalOnlyContent();
		}

		// Get the local content state for all the requested manifests
		var allLocalStates = await _contentService.GetLocalContentForManifests(manifestsToReset);

		// Builds the local content state from a list of local states
		return _contentService.BuildLocalContentState(manifestsToReset, allLocalStates);
	}
}

public class ContentResetCommandArgs : ContentCommandArgs
{
	public string[] ManifestIdsToReset;
}
