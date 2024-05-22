namespace cli;

public class ServicesResetCommand : CommandGroup
{
	public ServicesResetCommand() :
		base("reset",
			"Clean up docker related resources")
	{
	}
}
