using Beamable.Common.Content;
using System.CommandLine;
using System.Diagnostics;

namespace cli.Content;

public class ContentReplaceCommand : AppCommand<ContentReplaceCommandArgs>, ISkipManifest
{
	private ContentService _contentService;

	public ContentReplaceCommand() : base("replace-local", "Replaces the local content from a specific realm cached folder to another realm cached folder | You will lose all the content in the target realm ")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOption("from", "The source Realm PID"), (args, s) => args.Source = s);
		AddOption(new ConfigurableOption("to", "The target Realm PID"), (args, s) => args.Destination = s);
	}

	public override Task Handle(ContentReplaceCommandArgs args)
	{
		string contentDirectory = Path.Combine(args.ConfigService.BeamableWorkspace, ".beamable/content/");
		
		string sourcePath = Path.Combine(contentDirectory, args.Source);	
		
		string destinationPath = Path.Combine(contentDirectory, args.Destination);		

		return args.ContentService.ReplaceLocalContent(contentDirectory, sourcePath, destinationPath);
	}

}

public class ContentReplaceCommandArgs : ContentCommandArgs
{
	public string Source;
	public string Destination;
}
