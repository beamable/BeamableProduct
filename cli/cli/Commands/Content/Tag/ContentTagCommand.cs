using cli.Services.Content;
using System.Diagnostics;

namespace cli.Content.Tag;

public class ContentTagCommand : AppCommand<ContentTagCommandArgs>
{
		private ContentService _contentService;

		public ContentTagCommand() : base("tag", "opens tags file")
		{
		}

		public override void Configure()
		{
			AddOption(ContentCommand.MANIFEST_OPTION, (args, s) => args.ManifestId = s);
		}

		public override Task Handle(ContentTagCommandArgs args)
		{
			_contentService = args.ContentService;
			new Process
			{
				StartInfo = new ProcessStartInfo(_contentService.GetLocalCache(args.ManifestId).Tags.FullPath) { UseShellExecute = true }
			}.Start();
			return Task.CompletedTask;
		}
}

public class ContentTagCommandArgs : ContentCommandArgs
{
}
