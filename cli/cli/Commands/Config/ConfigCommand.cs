using Beamable.Common;
using Newtonsoft.Json;
using System.CommandLine;

namespace cli;

public class ConfigCommand : AtomicCommand<ConfigCommandArgs, ConfigCommandResult>, ISkipManifest
{
	public ConfigCommand() : base("config", "List the current beamable configuration")
	{
	}

	public override void Configure()
	{
		// nothing to do.
		AddOption(new Option<bool>("--no-overrides", () => false, "Whether this command should ignore the local config overrides"), (args, b) => args.IgnoreOverrides = b);
		AddOption(
			new Option<bool>("--set", () => false,
				"When true, whatever '--host', '--cid', '--pid' values you provide will be set. If '--no-overrides' is true, this will set the version controlled configuration file. If not, this will set the local overrides file inside the .beamable/temp directory"),
			(args, b) => args.IsSet = b);
	}

	public override Task<ConfigCommandResult> GetResult(ConfigCommandArgs args)
	{
		// If we were asked to set the config values, we first set them.
		var res = new ConfigCommandResult();
		res.configPath = args.ConfigService.ConfigDirectoryPath;

		if (args.IgnoreOverrides)
		{
			if (args.IsSet)
			{
				args.ConfigService.WriteConfig(config =>
				{
					ConfigService.SetConfig(ConfigService.CFG_JSON_FIELD_HOST, args.AppContext.Host, config);
					ConfigService.SetConfig(ConfigService.CFG_JSON_FIELD_CID, args.AppContext.Cid, config);
					ConfigService.SetConfig(ConfigService.CFG_JSON_FIELD_PID, args.AppContext.Pid, config);
				});
				// args.ConfigService.SetConfigString(ConfigService.CFG_JSON_FIELD_HOST, args.AppContext.Host);
				// args.ConfigService.SetConfigString(ConfigService.CFG_JSON_FIELD_CID, args.AppContext.Cid);
				// args.ConfigService.SetConfigString(ConfigService.CFG_JSON_FIELD_PID, args.AppContext.Pid);
				// args.ConfigService.FlushConfig();
			}

			res.host = args.ConfigService.GetConfigStringIgnoreOverride(ConfigService.CFG_JSON_FIELD_HOST);
			res.cid = args.ConfigService.GetConfigStringIgnoreOverride(ConfigService.CFG_JSON_FIELD_CID);
			res.pid = args.ConfigService.GetConfigStringIgnoreOverride(ConfigService.CFG_JSON_FIELD_PID);
		}
		else
		{
			if (args.IsSet)
			{
				args.ConfigService.WriteConfig(config =>
				{
					ConfigService.SetConfig(ConfigService.CFG_JSON_FIELD_HOST, args.AppContext.Host, config);
					ConfigService.SetConfig(ConfigService.CFG_JSON_FIELD_CID, args.AppContext.Cid, config);
					ConfigService.SetConfig(ConfigService.CFG_JSON_FIELD_PID, args.AppContext.Pid, config);
				}, true);
				
				// if (args.ConfigService.GetConfigStringIgnoreOverride(ConfigService.CFG_JSON_FIELD_HOST) != args.AppContext.Host)
				// 	args.ConfigService.SetLocalOverride(ConfigService.CFG_JSON_FIELD_HOST, args.AppContext.Host);
				//
				// if (args.ConfigService.GetConfigStringIgnoreOverride(ConfigService.CFG_JSON_FIELD_CID) != args.AppContext.Cid)
				// 	args.ConfigService.SetLocalOverride(ConfigService.CFG_JSON_FIELD_CID, args.AppContext.Cid);
				//
				// if (args.ConfigService.GetConfigStringIgnoreOverride(ConfigService.CFG_JSON_FIELD_PID) != args.AppContext.Pid)
				// 	args.ConfigService.SetLocalOverride(ConfigService.CFG_JSON_FIELD_PID, args.AppContext.Pid);

				// args.ConfigService.FlushLocalOverrides();
			}

			res.host = args.ConfigService.GetConfigString2(ConfigService.CFG_JSON_FIELD_HOST);
			res.cid = args.ConfigService.GetConfigString2(ConfigService.CFG_JSON_FIELD_CID);
			res.pid = args.ConfigService.GetConfigString2(ConfigService.CFG_JSON_FIELD_PID);
		}
		
		

		return Task.FromResult(res);
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
	public bool IgnoreOverrides;
	public bool IsSet;
}
