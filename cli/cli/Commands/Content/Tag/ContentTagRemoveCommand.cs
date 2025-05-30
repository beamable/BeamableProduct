using Beamable.Common;
using Beamable.Common.Content;

namespace cli.Content.Tag;

public class ContentTagRemoveCommand : AppCommand<ContentTagRemoveCommandArgs>
{
	private ContentService _contentService;

	public ContentTagRemoveCommand() : base("rm", "Removes tag from content")
	{
	}

	public override void Configure()
	{
		AddArgument(ContentTagCommand.TAG_ARGUMENT, (args, s) => args.Tags = s.Split(','));

		AddOption(ContentCommand.FILTER_TYPE_OPTION, (args, b) => args.FilterType = b);
		AddOption(ContentCommand.FILTER_OPTION, (args, s) => args.Filter = string.IsNullOrEmpty(s) ? Array.Empty<string>() : s.Split(','));
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIds = s);
	}

	public override async Task Handle(ContentTagRemoveCommandArgs args)
	{
		_contentService = args.ContentService;

		if (args.ManifestIds.Length == 0)
			args.ManifestIds = new[] { "global" };

		var tasks = new List<Task<LocalContentFiles>>();
		foreach (string manifestId in args.ManifestIds)
		{
			tasks.Add(_contentService.GetAllContentFiles(null, manifestId, args.FilterType, args.Filter, true));
		}

		// Get the files and filter them.
		var filteredContentFiles = await Task.WhenAll(tasks);

		// Save the added task to disk
		var tagRemoveTasks = new List<Task>();
		foreach (var f in filteredContentFiles)
		{
			tagRemoveTasks.Add(_contentService.RemoveTags(f, args.Tags));
		}

		await Task.WhenAll(tagRemoveTasks);
	}
}

public class ContentTagRemoveCommandArgs : ContentTagCommandArgs
{
	public string[] Filter;
	public string[] Tags;
	public ContentFilterType FilterType;
}
