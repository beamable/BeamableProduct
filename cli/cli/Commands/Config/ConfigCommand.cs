using Beamable.Common;
using Newtonsoft.Json;

namespace cli;

public class ConfigCommand : AtomicCommand<ConfigCommandArgs, ConfigCommandResult>
{
	public ConfigCommand() : base("config", "List the current beamable configuration")
	{
	}

	public override void Configure()
	{
		// nothing to do.
	}

	public override Task<ConfigCommandResult> GetResult(ConfigCommandArgs args)
	{
		return Task.FromResult(new ConfigCommandResult()
		{
			configPath = args.ConfigService.ConfigDirectoryPath,
			host = args.ConfigService.GetConfigString(Constants.CONFIG_PLATFORM),
			cid = args.ConfigService.GetConfigString(Constants.CONFIG_CID),
			pid = args.ConfigService.GetConfigString(Constants.CONFIG_PID)
		});
	}
}


public class ConfigCommandResult
{
	public string host;
	public string cid;
	public string pid;
	public string configPath;
}

public class ConfigCommandArgs : CommandArgs
{

}
