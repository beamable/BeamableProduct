using cli.Services.Content;
using System.Diagnostics;

namespace cli.Content;

public class ContentCommand : AppCommand<ContentCommandArgs>
{
	private ContentService _contentService;
	public ContentCommand() : base("content", "Open content folder in file explorer")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOption("manifest-id", "Set the manifest to use, 'global' by default"),
			(args, s) => args.ManifestId = s);
	}

	public override Task Handle(ContentCommandArgs args)
	{
		_contentService = args.ContentService;
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
