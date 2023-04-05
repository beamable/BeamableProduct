using Beamable.Common;
using Newtonsoft.Json;

namespace cli;

public class ConfigCommandArgs : CommandArgs
{

}

public class ConfigCommand : AppCommand<ConfigCommandArgs>
{

	public ConfigCommand() : base("config", "List the current configuration")
	{
	}

	public override void Configure()
	{
		// nothing to do.
	}

	public override Task Handle(ConfigCommandArgs args)
	{
		BeamableLogger.Log(args.ConfigService.ConfigFilePath);
		BeamableLogger.Log($"cid=[{args.AppContext.Cid}] pid=[{args.AppContext.Pid}]");
		BeamableLogger.Log(args.ConfigService.PrettyPrint());
		return Task.CompletedTask;
	}
}
