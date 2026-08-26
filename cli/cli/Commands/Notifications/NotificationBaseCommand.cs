namespace cli.Notifications;


public class NotificationBaseCommand : CommandGroup
{
	public NotificationBaseCommand() : base("listen", "Listen for real-time Beamable notifications — player or server events")
	{
	}
}
