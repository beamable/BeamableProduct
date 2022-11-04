namespace cli.Content;

public class ContentCommand : AppCommand<ContentCommandArgs>
{
	public ContentCommand() : base("content", "content command")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(ContentCommandArgs args)
	{
		return Task.CompletedTask;
	}
}

public class ContentCommandArgs : CommandArgs
{
}
