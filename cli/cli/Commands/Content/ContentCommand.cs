using System.CommandLine;

namespace cli.Content;

public class ContentCommand : AppCommand<ContentCommandArgs>
{
	public ContentCommand() : base("content", "content command")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOption("manifestId", "set the manifest to use, 'global' by default"),
			(args, s) => args.ManifestId = s);
	}

	public override Task Handle(ContentCommandArgs args)
	{
		return Task.CompletedTask;
	}
}

public class ContentCommandArgs : CommandArgs
{
	public string ManifestId;
}
