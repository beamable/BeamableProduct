using System.CommandLine;
using Beamable.Common.BeamCli;

namespace cli;

[CliContractType]
[Serializable]
public class ConfigSetCommandArgs : CommandArgs
{
	public string name;
	public string value;
}

public class ConfigSetCommand : AppCommand<ConfigSetCommandArgs>
{
	private ConfigService _configService;

	public ConfigSetCommand()
		: base("set", "Set a config value")
	{
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
		_configService = args.ConfigService;

		_configService.WriteConfigString(args.name, args.value);
		return Task.CompletedTask;
	}
}
