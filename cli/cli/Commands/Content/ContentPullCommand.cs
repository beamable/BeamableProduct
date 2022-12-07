using Beamable.Common;
using cli.Services;
using System.Text.Json;

namespace cli.Content;

public class ContentPullCommand : AppCommand<ContentPullCommandArgs>
{
	private readonly ContentService _contentService;
	public ContentPullCommand(ContentService contentService) : base("pull", "Pulls currently deployed content")
	{
		_contentService = contentService;
	}

	public override async Task Handle(ContentPullCommandArgs args)
	{
		var manifest = await _contentService.GetManifest(args.ManifestId);
		_contentService.UpdateTags(manifest);
		var result = await _contentService.PullContent(manifest);
		if(args.printOutput){
			var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
			BeamableLogger.Log(json);
		}
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOptionFlag("printOutput", "Print content to console"),
			(args, b) => args.printOutput = b);
	}
}

public class ContentPullCommandArgs : ContentCommandArgs
{
	public bool printOutput;
}
