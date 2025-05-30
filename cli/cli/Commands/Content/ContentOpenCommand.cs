using System.CommandLine;
using System.Diagnostics;

namespace cli.Content;

public class ContentOpenCommand : AppCommand<ContentOpenCommandArgs>, ISkipManifest
{
	private ContentService _contentService;

	public ContentOpenCommand() : base("open", "Open content file in default editor")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIds = s);
		AddOption(ContentCommand.FILTER_TYPE_OPTION, (args, f) => args.FilterType = f);
		AddOption(ContentCommand.FILTER_OPTION, (args, s) => args.Filter = string.IsNullOrEmpty(s) ? Array.Empty<string>() : s.Split(','));
	}

	public override async Task Handle(ContentOpenCommandArgs args)
	{
		_contentService = args.ContentService;

		if (args.ManifestIds.Length == 0)
			args.ManifestIds = new[] { "global" };

		// If no filter was provided, we just open the content folders directly
		if (args.Filter.Length == 0)
		{
			foreach (var m in args.ManifestIds)
			{
				var dir = args.ContentService.EnsureContentPathForRealmExists(out _, args.AppContext.Pid, m);
				if (File.Exists(dir))
				{
					new Process { StartInfo = new ProcessStartInfo(dir) { UseShellExecute = true } }.Start();
				}
			}
		}

		// Get only the local files for all the given manifest ids.
		var filteredManifests = await Task.WhenAll(args.ManifestIds.Select(async m => await _contentService.GetAllContentFiles(null, m, args.FilterType, args.Filter, true)));

		// Look for the given content id and open the file.
		foreach (LocalContentFiles localFiles in filteredManifests)
		{
			for (int i = 0; i < localFiles.ContentFiles.Count; i++)
			{
				ContentFile f = localFiles.ContentFiles[i];
				var path = f.LocalFilePath;
				Debug.Assert(File.Exists(path), "If you see this, please run the command again with ´--logs v´ and send the logs (in `.beamable/temp`) to beamable.");
				Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, Arguments = $"-{(i == 0 ? "-n" : "-r")}" });
			}
		}
	}
}

public class ContentOpenCommandArgs : ContentCommandArgs
{
	public string[] Filter;
	public ContentFilterType FilterType;
}
