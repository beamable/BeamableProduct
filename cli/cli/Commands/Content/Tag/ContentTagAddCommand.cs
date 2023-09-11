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
		AddArgument(ContentTagCommand.CONTENT_ARGUMENT, (args, s) => args.content = s);
		AddArgument(ContentTagCommand.TAG_ARGUMENT, (args, s) => args.tag = s);
		AddOption(ContentCommand.MANIFEST_OPTION, (args, s) => args.ManifestId = s);
		AddOption(ContentTagCommand.REGEX_OPTION, (args, b) => args.treatAsRegex = b);
	}

	public override Task Handle(ContentTagAddCommandArgs args)
	{
		_contentService = args.ContentService;
		var local = _contentService.GetLocalCache(args.ManifestId);


		var contentIds = args.GetContentsList(local);
		var addedValues = new List<string>();

		foreach (var id in contentIds)
		{
			if (local.Tags.AddTagToContent(id, args.tag))
			{
				addedValues.Add(id);
			}
		}
		local.Tags.WriteToFile();
		BeamableLogger.Log("Added tag {ArgsTag} to content ({AddedValuesCount}): {Join}", args.tag, addedValues.Count, string.Join(", ", addedValues));
		return Task.CompletedTask;
	}
}

public class ContentTagAddCommandArgs : ContentTagCommandArgs
{
	public string content;
	public string tag;
	public bool treatAsRegex;

	public List<string> GetContentsList(ContentLocalCache cache)
	{
		var result = treatAsRegex
			? cache.ContentMatchingRegex(content).ToList()
			: content.Split(",").ToList();

		if (result.Count == 0)
		{
			throw new CliException("Could not find any matching content.");
		}

		var wrongContent = result.Where(id => cache.GetContent(id) == null).ToList();
		if (wrongContent.Count != 0)
		{
			throw new CliException($"Could not find content: {string.Join(", ", wrongContent)}");
		}

		return result;
	}
}
