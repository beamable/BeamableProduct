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

	public override Task Handle(ContentOpenCommandArgs args)
	{
		_contentService = args.ContentService;

		if (args.ManifestIds.Length == 0)
			args.ManifestIds = new[] { "global" };

		foreach (var manifestId in args.ManifestIds)
		{
			var localContent = _contentService.GetLocalCache(manifestId);

			var path = string.IsNullOrWhiteSpace(args.contentId)
				? localContent.ContentDirPath
				: localContent.GetContentPath(args.contentId);
			if (File.Exists(path))
			{
				new Process { StartInfo = new ProcessStartInfo(path) { UseShellExecute = true } }.Start();
			}
		}

		return Task.CompletedTask;
	}
}

public class ContentOpenCommandArgs : ContentCommandArgs
{
	public string contentId;
}
