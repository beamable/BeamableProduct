using cli.Services.Content;
using System.CommandLine;
using System.Diagnostics;

namespace cli.Content;

public class ContentOpenCommand : AppCommand<ContentOpenCommandArgs>
{
	private ContentService _contentService;

	public ContentOpenCommand() : base("open", "Open content file in default editor")
	{
	}

	public override void Configure()
	{
		var contentId = new Argument<string>(nameof(ContentOpenCommandArgs.contentId));
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIds = s);
		AddArgument(contentId, (args, i) => args.contentId = i);
	}

	public override async Task Handle(ContentOpenCommandArgs args)
	{
		_contentService = args.ContentService;

		if (args.ManifestIds.Length == 0)
			args.ManifestIds = new[] { "global" };

		// Get only the local files for all the given manifest ids.
		var localManifests = await Task.WhenAll(args.ManifestIds.Select(async m => await _contentService.GetAllContentFiles(null, m, true)));

		// Look for the given content id and open the file.
		foreach (LocalContentFiles localFiles in localManifests)
		{
			foreach (ContentFile f in localFiles.ContentFiles)
			{
				var path = f.Id == args.contentId ? f.LocalFilePath : Path.GetDirectoryName(f.LocalFilePath);
				if (File.Exists(path))
				{
					new Process { StartInfo = new ProcessStartInfo(path) { UseShellExecute = true } }.Start();
				}
			}
		}
	}
}

public class ContentOpenCommandArgs : ContentCommandArgs
{
	public string contentId;
}
