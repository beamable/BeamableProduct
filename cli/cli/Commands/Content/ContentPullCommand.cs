using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using cli.Services;
using Newtonsoft.Json;

namespace cli.Content;

public class ContentPullCommand : AppCommand<ContentPullCommandArgs>
{
	private readonly ContentService _contentService;
	private string ManifestId = "global";
	public ContentPullCommand(ContentService contentService) : base("pull", "Pulls currently deployed content")
	{
		_contentService = contentService;
	}

	public override async Task Handle(ContentPullCommandArgs args)
	{
		var manifest = await _contentService.GetManifest();
		var result = await _contentService.PullContent(manifest);
		
	}

	public override void Configure()
	{
	}
}

public class ContentPullCommandArgs : CommandArgs
{
}
