using Beamable.Common;
using Newtonsoft.Json;

namespace cli;

public class ConfigCommandArgs : CommandArgs
{

}

public class ConfigCommand : AppCommand<ConfigCommandArgs>
{
	private readonly ConfigService _configService;

	public ConfigCommand(ConfigService configService) : base("config", "list the current configuration")
	{
		_configService = configService;
	}

	public override void Configure()
	{
		// nothing to do.
	}

	public override Task Handle(ConfigCommandArgs args)
	{
		BeamableLogger.Log(_configService.ConfigFilePath);
		BeamableLogger.Log(_configService.PrettyPrint());
		return Task.CompletedTask;
	}
}
