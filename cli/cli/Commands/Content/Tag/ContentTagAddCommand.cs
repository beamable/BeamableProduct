using Beamable.Common;
using cli.Services.Content;

namespace cli.Content.Tag;

public class ContentTagAddCommand : AppCommand<ContentTagAddCommandArgs>
{
	private ContentService _contentService;

	public ContentTagAddCommand() : base("add", "Adds tag to content")
	{
	}

	public override void Configure()
	{
		AddArgument(ContentTagCommand.TAG_ARGUMENT, (args, s) => args.Tags = s.Split(','));

		AddOption(ContentCommand.FILTER_TYPE_OPTION, (args, b) => args.FilterType = b);
		AddOption(ContentCommand.FILTER_OPTION, (args, s) => args.Filter = s.Split(','));
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIds = s);
	}

	public override async Task Handle(ContentTagAddCommandArgs args)
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
			tagAddTasks.Add(_contentService.AddTags(f, args.Tags));
		}

		await Task.WhenAll(tagAddTasks);
	}
}

public class ContentTagAddCommandArgs : ContentTagCommandArgs
{
	public string[] Filter;
	public string[] Tags;
	public ContentFilterType FilterType;
}
