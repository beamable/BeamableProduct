using cli.Services.Content;
using System.Diagnostics;

namespace cli.Content;

public class ContentCommand : AppCommand<ContentCommandArgs>
{
	private readonly ContentService _contentService;
	public ContentCommand(ContentService contentService) : base("content", "Open content folder in file explorer")
	{
		_contentService = contentService;
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOption("manifestId", "set the manifest to use, 'global' by default"),
			(args, s) => args.ManifestId = s);
	}

	public override Task Handle(ContentCommandArgs args)
	{
		new Process
		{
			StartInfo = new ProcessStartInfo(_contentService.ContentLocal.ContentDirPath)
			{
				UseShellExecute = true
			}
		}.Start();
		return Task.CompletedTask;
	}
}

public class ContentCommandArgs : CommandArgs
{
	public string ManifestId;
}
