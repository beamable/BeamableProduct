using Beamable.Common;
using Newtonsoft.Json;

namespace cli;

public class ConfigCommandArgs : CommandArgs
{
}

public class ConfigCommand : AppCommand<ConfigCommandArgs>, IResultSteam<DefaultStreamResultChannel, ConfigCommandResult>
{
	public ConfigCommand() : base("config", "List the current beamable configuration")
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

		this.SendResults(new ConfigCommandResult()
		{
			host = args.ConfigService.GetConfigString(Constants.CONFIG_PLATFORM),
			cid = args.ConfigService.GetConfigString(Constants.CONFIG_CID),
			pid = args.ConfigService.GetConfigString(Constants.CONFIG_PID)
		});

		return Task.CompletedTask;
	}
}

public class ConfigCommandResult
{
	public string host;
	public string cid;
	public string pid;
}
