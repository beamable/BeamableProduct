using Beamable.Common;
using cli.Services.Content;

namespace cli.Content.Tag;

public class ContentTagRemoveCommand : AppCommand<ContentTagRemoveCommandArgs>
{
	private ContentService _contentService;

	public ContentTagRemoveCommand() : base("rm", "Removes tag from content")
	{
	}

	public override void Configure()
	{
		AddArgument(ContentTagCommand.FILTER_OPTION, (args, s) => args.Filter = s.Split(','));
		AddArgument(ContentTagCommand.TAG_ARGUMENT, (args, s) => args.Tags = s.Split(','));

		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIds = s);
		AddOption(ContentTagCommand.FILTER_TYPE_OPTION, (args, b) => args.FilterType = b);
	}

	public override async Task Handle(ContentTagRemoveCommandArgs args)
	{
		_contentService = args.ContentService;

		if (args.ManifestIds.Length == 0)
			args.ManifestIds = new[] { "global" };

		var tasks = new List<Task<LocalContentFiles>>();
		foreach (string manifestId in args.ManifestIds)
		{
			tasks.Add(_contentService.GetAllContentFiles(null, manifestId, true));
		}

		// Get the files and filter them.
		var filteredContentFiles = await Task.WhenAll(tasks);
		for (int i = 0; i < filteredContentFiles.Length; i++)
		{
			LocalContentFiles file = filteredContentFiles[i];
			_contentService.FilterLocalContentFiles(ref file, args.Filter, args.FilterType);
			filteredContentFiles[i] = file;
		}

		// Save the added task to disk
		var tagAddTasks = new List<Task>();
		foreach (var f in filteredContentFiles)
		{
			tagAddTasks.Add(_contentService.RemoveTags(f, args.Tags));
		}

		await Task.WhenAll(tagAddTasks);
	}
}

public class ContentTagRemoveCommandArgs : ContentTagCommandArgs
{
	public string[] Filter;
	public string[] Tags;
	public ContentFilterType FilterType;
}
