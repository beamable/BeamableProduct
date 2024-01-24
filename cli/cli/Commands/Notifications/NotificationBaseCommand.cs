namespace cli.Notifications;

public class NotificationCommandArgs : CommandArgs
{
	
}
public class NotificationBaseCommand : AppCommand<NotificationCommandArgs>
{
	public NotificationBaseCommand() : base("listen", "listen to events")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(NotificationCommandArgs args)
	{
		return Task.CompletedTask;
	}
}
