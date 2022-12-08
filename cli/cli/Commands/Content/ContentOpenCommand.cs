using cli.Services;
using System.CommandLine;
using System.Diagnostics;

namespace cli.Content;

public class ContentOpenCommand : AppCommand<ContentOpenCommandArgs>
{
	private readonly ContentService _contentService;

	public ContentOpenCommand(ContentService contentService) : base("open", "Open content file in default editor")
	{
		_contentService = contentService;
	}

	public override void Configure()
	{
		var contentId = new Argument<string>(nameof(ContentOpenCommandArgs.contentId));
		AddArgument(contentId, (args, i) => args.contentId = i);
	}

	public override Task Handle(ContentOpenCommandArgs args)
	{
		var path = string.IsNullOrWhiteSpace(args.contentId)
			? _contentService.ContentLocal.ContentDirPath
			: _contentService.ContentLocal.GetContentPath(args.contentId);
		if (File.Exists(path))
		{
			new Process
			{
				StartInfo = new ProcessStartInfo(path)
				{
					UseShellExecute = true
				}
			}.Start();
		}

		return Task.CompletedTask;
	}
}

public class ContentOpenCommandArgs : CommandArgs
{
	public string contentId;
}
