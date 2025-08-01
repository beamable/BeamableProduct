﻿using Beamable.Common;
using cli.DeploymentCommands;
using cli.Services;

namespace cli.Content;

public class ContentPublishCommand : AtomicCommand<ContentPublishCommandArgs, ContentPublishResult>, ISkipManifest, IResultSteam<ProgressStreamResultChannel, ContentProgressUpdateData>
{
	private ContentService _contentService;

	public ContentPublishCommand() : base("publish", "Publish content and manifest")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIdsToPublish = s);
	}

	public override async Task<ContentPublishResult> GetResult(ContentPublishCommandArgs args)
	{
		_contentService = args.ContentService;

		// Prepare the filters --- makes sure all the given manifests exist.
		var publishPromises = new List<Task>();
		foreach (string manifestId in args.ManifestIdsToPublish)
		{
			publishPromises.Add(_contentService.PublishContent(manifestId, this.SendResults<ProgressStreamResultChannel, ContentProgressUpdateData>));
		}
		await Task.WhenAll(publishPromises);

		return new();
	}
	
}

public class ContentPublishCommandArgs : ContentCommandArgs
{
	public string[] ManifestIdsToPublish;
}

public class ContentPublishResult
{
}

