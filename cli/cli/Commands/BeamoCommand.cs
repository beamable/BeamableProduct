namespace cli;
public class BeamoCommandArgs : CommandArgs { }

public class BeamoCommand : AppCommand<BeamoCommandArgs>
{
	private readonly ConfigService _configService;

	public BeamoCommand(ConfigService configService) : base("beamo", "list the current configuration")
	{
		_configService = configService;
	}
	public override void Configure()
	{
		// nothing to do.
	}

	public override Task Handle(BeamoCommandArgs args)
	{
		Console.WriteLine(_configService.ConfigFilePath);
		Console.WriteLine(_configService.PrettyPrint());
		return Task.CompletedTask;
	}
}

