using Beamable.Common;
using cli.Services.Content;

namespace cli.Content;

public class ContentPublishCommand : AtomicCommand<ContentPublishCommandArgs, LocalContentState>
{
	private ContentService _contentService;

	public ContentPublishCommand() : base("publish", "Publish content and manifest")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIdsToPublish = s);
	}

	public override async Task<LocalContentState> GetResult(ContentPublishCommandArgs args)
	{
		_contentService = args.ContentService;

		// Prepare the filters (makes sure all the given manifests exist.
		var manifestsToPublish = await _contentService.PrepareManifestFilter(args.ManifestIdsToPublish);

		// Publish the local state to the remove manifests
		_ = await Promise.Sequence(manifestsToPublish.Select(m => _contentService.PublishContentAndManifest(m)).ToArray());

		// Get the local content state for all the requested manifests
		var allLocalStates = await _contentService.GetLocalContentForManifests(manifestsToPublish);

		// Builds the local content state from a list of local states
		return _contentService.BuildLocalContentState(manifestsToPublish, allLocalStates);
	}
}

public class ContentPublishCommandArgs : ContentCommandArgs
{
	public string[] ManifestIdsToPublish;
}
