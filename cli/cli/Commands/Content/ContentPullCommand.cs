﻿using Beamable.Common;
using cli.Services.Content;
using Spectre.Console;
using System.Text;

namespace cli.Content;

public class ContentPullCommand : AtomicCommand<ContentPullCommandArgs, LocalContentState>
{
	private ContentService _contentService;

	public ContentPullCommand() : base("pull", "Pulls currently deployed content")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIdsToPull = s.Length > 0 ? s[0].Split(',') : Array.Empty<string>());
		AddOption(ContentCommand.CONTENT_IDS_FILTER_OPTION, (args, s) => args.ContentIdsToPull = s.Length > 0 ? s[0].Split(',') : Array.Empty<string>());
	}

	public override async Task<LocalContentState> GetResult(ContentPullCommandArgs args)
	{
		_contentService = args.ContentService;

		// Prepare the filters (makes sure all the given manifests exist.
		var manifestsToPull = await _contentService.PrepareManifestFilter(args.ManifestIdsToPull);

		// Resets the content for all the given manifests
		for (int i = 0; i < manifestsToPull.Length; i++)
		{
			var localCache = _contentService.GetLocalCache(manifestsToPull[i]);

			await localCache.UpdateTags();
			if(args.ContentIdsToPull?.Length == 0)
				_ = await localCache.PullContent();
			else
				_ = await localCache.PullContent(args.ContentIdsToPull);
		}

		// Get the local content state for all the requested manifests
		var allLocalStates = await _contentService.GetLocalContentForManifests(manifestsToPull);

		// Builds the local content state from a list of local states
		return _contentService.BuildLocalContentState(manifestsToPull, allLocalStates);
	}
}

public class ContentPullCommandArgs : ContentCommandArgs
{
	public string[] ManifestIdsToPull;
	public string[] ContentIdsToPull;
}
