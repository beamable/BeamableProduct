using cli.Services;
using System.CommandLine;
using Beamable.Server;

namespace cli.Docs;

public class GenerateDocsCommandArgs : CommandArgs
{
	public string categorySlug;
	public string readmeApiKey;
	public string readmeVersion;
	public string commandParentSlug;
	public string guideParentSlug;
}



public class GenerateDocsCommand : AppCommand<GenerateDocsCommandArgs>, IStandaloneCommand
{
	public override bool IsForInternalUse => true;

	public GenerateDocsCommand() : base("docs", "Generate CLI documentation")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--category", () => "cli", "The category slug to use"), (args, i) => args.categorySlug = i);
		AddOption(new Option<string>("--command-slug", () => "cli-commands", "The parent slug for all command docs"), (args, i) => args.commandParentSlug = i);
		AddOption(new Option<string>("--guide-slug", () => "cli-guides", "The parent slug for all guide docs"), (args, i) => args.guideParentSlug = i);
		AddOption(new Option<string>("--readme-key", "The api key to use to push to Readme"), (args, i) => args.readmeApiKey = i);
		AddOption(new Option<string>("--readme-version", "The version string for readme to use"), (args, i) => args.readmeVersion = i);
	}

	public override async Task Handle(GenerateDocsCommandArgs args)
	{
		// build a markdown file for each command...
		var generatorContext = args.DependencyProvider.GetService<CliGenerator>().GetCliContext();

		var docService = args.DependencyProvider.GetService<DocService>();

		docService.SetReadmeAuth(args.readmeApiKey, args.readmeVersion);

		var guideTask = docService.UploadGuides(args);
		var publications = new List<Task>();
		foreach (var command in generatorContext.Commands)
		{
			if (command == generatorContext.Root) continue;
			if (!(command.command is IAppCommand appCommand)) continue;
			
			var doc = docService.GenerateDocFile(command, args);
			Log.Information(doc.markdownContent);

			// TODO: at some point, rate limiting may kick us in the pants.
			var reqObject = new ReadmePostDocumentRequest
			{
				slug = doc.slug,
				body = doc.markdownContent,
				title = doc.title,
				excerpt = doc.excerpt,
				hidden = appCommand.IsForInternalUse,
				categorySlug = doc.categorySlug,
				parentDocSlug = doc.parentSlug
			};
			var task = docService.PublishDoc(reqObject);
			publications.Add(task);

		}

		await Task.WhenAll(publications);
		await guideTask;
	}

}
