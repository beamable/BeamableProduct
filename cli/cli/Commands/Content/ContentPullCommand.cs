using Beamable.Common;
using cli.Services.Content;
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
		var manifest = await _contentService.GetManifest(args.ManifestId);
		_contentService.UpdateTags(manifest);
		var result = await _contentService.PullContent(manifest);
		if (args.printOutput)
		{
			var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
			BeamableLogger.Log(json);
		}
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOption("manifestId", "set the manifest to use, 'global' by default"),
			(args, s) => args.ManifestId = s);
		AddOption(new ConfigurableOptionFlag("printOutput", "Print content to console"),
			(args, b) => args.printOutput = b);
	}
}

public class ContentPullCommandArgs : ContentCommandArgs
{
	public bool printOutput;
}
