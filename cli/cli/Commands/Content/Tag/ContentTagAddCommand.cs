using cli.Services.Content;
using Serilog;
using System.CommandLine;

namespace cli.Content.Tag;

public class ContentTagAddCommand : AppCommand<ContentTagAddCommandArgs>
{
	public static readonly ConfigurableOptionFlag REGEX_OPTION =
		new("treat-as-regex", "Treat content argument as regex pattern");

	private ContentService _contentService;

	public ContentTagAddCommand() : base("add", "adds tag to content")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("content"), (args, s) => args.content = s);
		AddArgument(new Argument<string>("tag"), (args, s) => args.tag = s);
		AddOption(ContentCommand.MANIFEST_OPTION, (args, s) => args.ManifestId = s);
		AddOption(REGEX_OPTION, (args, b) => args.treatAsRegex = b);
	}

	public override Task Handle(ContentTagAddCommandArgs args)
	{
		_contentService = args.ContentService;
		var local = _contentService.GetLocalCache(args.ManifestId);
		
		
		var contentIds = args.treatAsRegex
			? local.ContentMatchingRegex(args.content).ToList()
			: new List<string>() { args.content };

		if (!args.treatAsRegex && local.GetContent(args.content) == null || contentIds.Count == 0)
		{
			throw new CliException("Could not find any matching content.");
		}

		foreach (var id in contentIds)
		{
			local.Tags.AddTagToContent(id, args.tag);
		}
		local.Tags.WriteToFile();
		Log.Information("Added tag `{ArgsTag} to content ({ContentIdsCount}): {Join}", args.tag, contentIds.Count, string.Join(",",contentIds));
		return Task.CompletedTask;
	}
}

public class ContentTagAddCommandArgs : ContentTagCommandArgs
{
	public string content;
	public string tag;
	public bool treatAsRegex;
}
