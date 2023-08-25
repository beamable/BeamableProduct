using Beamable.Common;
using cli.Services.Content;
using Spectre.Console;
using System.Text.Json;

namespace cli.Content;

public class ContentPullCommand : AppCommand<ContentPullCommandArgs>
{
	private ContentService _contentService;
	public ContentPullCommand() : base("pull", "Pulls currently deployed content")
	{
	}

	public override async Task Handle(ContentPullCommandArgs args)
	{
		_contentService = args.ContentService;

		var localCache = _contentService.GetLocalCache(args.ManifestId);
		await localCache.UpdateTags();
		var result = await localCache.PullContent();

		if (args.printOutput)
		{
			var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
			AnsiConsole.WriteLine(json);
		}
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFEST_OPTION,
			(args, s) => args.ManifestId = s);
		AddOption(new ConfigurableOptionFlag("print-output", "Print content to console"),
			(args, b) => args.printOutput = b);
	}
}

public class ContentPullCommandArgs : ContentCommandArgs
{
	public bool printOutput;
}
