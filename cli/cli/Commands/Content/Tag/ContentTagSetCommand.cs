using Beamable.Common.Content;

namespace cli.Content.Tag;

public class ContentTagSetCommand : AppCommand<ContentTagSetCommandArgs>, IEmptyResult, ISkipManifest
{
	public static readonly ConfigurableOptionFlag CLEAR_TAG_FLAG_OPTION =
		new ConfigurableOptionFlag("clear", "Set the tags to a empty value, if you pass something in [<tag>] it will be ignored.");

	private ContentService _contentService;

	public ContentTagSetCommand() : base("set", "Set tag to content")
	{
	}

	public override void Configure()
	{
		AddArgument(ContentTagCommand.TAG_ARGUMENT, (args, s) => args.Tags = s.Split(','));

		AddOption(CLEAR_TAG_FLAG_OPTION, (args, b) => args.Clear = b);
		AddOption(ContentCommand.FILTER_TYPE_OPTION, (args, b) => args.FilterType = b);
		AddOption(ContentCommand.FILTER_OPTION, (args, s) => args.Filter = string.IsNullOrEmpty(s) ? Array.Empty<string>() : s.Split(','));
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIds = s);
	}

	public override async Task Handle(ContentTagSetCommandArgs args)
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

		// Clear the tags 
		if (args.Clear)
		{
			args.Tags = Array.Empty<string>();
		}
		
		// Save the tags task to disk
		var tagAddTasks = new List<Task>();
		foreach (var f in filteredContentFiles)
		{
			tagAddTasks.Add(_contentService.SetTags(f, args.Tags));
		}

		await Task.WhenAll(tagAddTasks);
	}
}

public class ContentTagSetCommandArgs : ContentTagCommandArgs
{
	public string[] Filter;
	public string[] Tags;
	public bool Clear;
	public ContentFilterType FilterType;
}
