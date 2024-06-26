﻿using Beamable.Common;
using cli.Services.Content;

namespace cli.Content.Tag;

public class ContentTagRemoveCommand : AppCommand<ContentTagAddCommandArgs>
{
	private ContentService _contentService;

	public ContentTagRemoveCommand() : base("rm", "Removes tag from content")
	{
	}

	public override void Configure()
	{
		AddArgument(ContentTagCommand.CONTENT_ARGUMENT, (args, s) => args.content = s);
		AddArgument(ContentTagCommand.TAG_ARGUMENT, (args, s) => args.tag = s);
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIds = s);
		AddOption(ContentTagCommand.REGEX_OPTION, (args, b) => args.treatAsRegex = b);
	}

	public override Task Handle(ContentTagAddCommandArgs args)
	{
		_contentService = args.ContentService;

		if (args.ManifestIds.Length == 0)
			args.ManifestIds = new[] { "global" };

		foreach (var manifestId in args.ManifestIds)
		{
			var local = _contentService.GetLocalCache(manifestId);

			var contentIds = args.GetContentsList(local);
			var removedValues = new List<string>();

			foreach (var id in contentIds)
			{
				if (local.Tags.RemoveTagFromContent(id, args.tag))
					removedValues.Add(id);
			}

			local.Tags.WriteToFile();
			BeamableLogger.Log("Removed tag {ArgsTag} from content ({RemovedValuesCount}): {Join}", args.tag, removedValues.Count, string.Join(", ", removedValues));
		}

		return Task.CompletedTask;
	}
}
