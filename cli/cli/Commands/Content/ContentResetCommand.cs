using cli.Services.Content;
using System.CommandLine;

namespace cli.Content;

public class ContentResetCommand : AtomicCommand<ContentResetCommandArgs, ContentResetResult>
{
	private static ConfigurableOptionFlag DELETE_CREATED_OPTION = new("delete-created", "Deletes any created content. If filters are provided, will only delete the created content that matches the filter");
	private ContentService _contentService;

	public ContentResetCommand() : base("reset", "Sets local content to match remote one")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIdsToReset = s);
		AddOption(ContentCommand.FILTER_TYPE_OPTION, (args, s) => args.FilterType = s);
		AddOption(ContentCommand.FILTER_OPTION, (args, s) => args.Filter = s.Split(','));
		AddOption(DELETE_CREATED_OPTION, (args, b) => args.DeleteCreatedContent = b);
	}

	public override async Task<ContentResetResult> GetResult(ContentResetCommandArgs args)
	{
		_contentService = args.ContentService;

		// Resets the content for all the given manifests
		var resetPromises = new List<Task>();
		foreach (var manifestId in args.ManifestIdsToReset)
		{
			resetPromises.Add(_contentService.ResetLocalContent(manifestId, args.Filter, args.FilterType, args.DeleteCreatedContent));
		}

		await Task.WhenAll(resetPromises);

		return new();
	}
}

public class ContentResetCommandArgs : ContentCommandArgs
{
	public string[] ManifestIdsToReset;

	public ContentFilterType FilterType;
	public string[] Filter;

	public bool DeleteCreatedContent;
}

public class ContentResetResult
{
}
