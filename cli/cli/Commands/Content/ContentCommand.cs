using cli.Services.Content;
using System.CommandLine;
using System.Diagnostics;

namespace cli.Content;

public class ContentCommand : CommandGroup<ContentCommandArgs>
{
	public static readonly Option<string> MANIFEST_OPTION =
		new("--manifest-id", () => "global", "Set the manifest to use, 'global' by default");

	private ContentService _contentService;

	public ContentCommand() : base("content", "Open content folder in file explorer")
	{
	}

	public override void Configure()
	{
		AddOption(MANIFEST_OPTION, (args, s) => args.ManifestId = s);
	}

	public override Task Handle(ContentCommandArgs args)
	{
		_contentService = args.ContentService;

		new Process
		{
			StartInfo = new ProcessStartInfo(_contentService.GetLocalCache(args.ManifestId).ContentDirPath) { UseShellExecute = true }
		}.Start();
		return Task.CompletedTask;
	}
}

public class ContentCommandArgs : CommandArgs
{
	public string ManifestId;
}
