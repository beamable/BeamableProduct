using System.CommandLine;

namespace cli;

public class ConfigSetCommandArgs : CommandArgs
{
	public string name;
	public string value;
}

public class ConfigSetCommand : AppCommand<ConfigSetCommandArgs>
{
	private readonly ConfigService _configService;

	public ConfigSetCommand(ConfigService configService)
		: base("set", "set a config value")
	{
		_configService = configService;
	}

	public override void Configure()
	{
		var name = new Argument<string>(nameof(ConfigSetCommandArgs.name));
		var value = new Argument<string>(nameof(ConfigSetCommandArgs.value));
		name.Description = "The name of a config option to set. ex: cid, pid, etc.";
		value.Description = "The value of the config setting";

		AddArgument(name, (args, i) => args.name = i);
		AddArgument(value, (args, i) => args.value = i);
	}

	public override Task Handle(ConfigSetCommandArgs args)
	{
		_configService.SetConfigString(args.name, args.value);
		_configService.FlushConfig();
		return Task.CompletedTask;
	}
}