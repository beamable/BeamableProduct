using Beamable.Common;
using Newtonsoft.Json;
using System.CommandLine;
using Beamable.Common.BeamCli;
using Beamable.Server;
using cli.Services;

namespace cli;

public class ConfigCommandProjectResult
{
	public List<string> linkedUnityProjects;
	public List<string> linkedUnrealProjects;
	public List<string> additionalProjectPaths;
	public List<string> projectPathsToIgnore;
	public string projectRoot;
}

public class ConfigCommandProjectChannel : IResultChannel
{
	public string ChannelName => "projectconfig";
}

public class ConfigCommand 
	: AtomicCommand<ConfigCommandArgs, ConfigCommandResult>
		, IResultSteam<ConfigCommandProjectChannel, ConfigCommandProjectResult>
		, ISkipManifest
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
		AddOption(
			new Option<bool>(new string[] { "--include-project-config", "-ipc" },
				"Should the command also emit project configuration values"),
			(args, i) => args.includeProjectConfig = i);
	}

	public override Task<ConfigCommandResult> GetResult(ConfigCommandArgs args)
	{
		// If we were asked to set the config values, we first set them.
		var res = new ConfigCommandResult();
		res.configPath = args.ConfigService.ConfigDirectoryPath;

		if (args.includeProjectConfig)
		{
			var projectData = args.ConfigService.LoadDataFile<ProjectData>(Constants.CONFIG_LINKED_PROJECTS);
			var projectRes = new ConfigCommandProjectResult
			{
				projectRoot = args.ConfigService.GetProjectRootPath(),
				additionalProjectPaths = args.ConfigService.LoadExtraPathsFromFile(),
				projectPathsToIgnore = args.ConfigService.LoadPathsToIgnoreFromFile(),
				linkedUnityProjects = projectData.unityProjectsPaths?.ToList(),
				linkedUnrealProjects = projectData.unrealProjectsPaths.Select(x => x.Path).ToList()
			};
			this.SendResults<ConfigCommandProjectChannel, ConfigCommandProjectResult>(projectRes);
			if (AutoLogOutput)
			{
				LogResult(projectRes);
			}
		}
		
		if (args.IgnoreOverrides)
		{
			if (args.IsSet)
			{
				args.ConfigService.SetConfigString(Constants.CONFIG_PLATFORM, args.AppContext.Host);
				args.ConfigService.SetConfigString(Constants.CONFIG_CID, args.AppContext.Cid);
				args.ConfigService.SetConfigString(Constants.CONFIG_PID, args.AppContext.Pid);
				args.ConfigService.FlushConfig();
			}

			res.host = args.ConfigService.GetConfigStringIgnoreOverride(Constants.CONFIG_PLATFORM);
			res.cid = args.ConfigService.GetConfigStringIgnoreOverride(Constants.CONFIG_CID);
			res.pid = args.ConfigService.GetConfigStringIgnoreOverride(Constants.CONFIG_PID);
		}
		else
		{
			if (args.IsSet)
			{
				if (args.ConfigService.GetConfigStringIgnoreOverride(Constants.CONFIG_PLATFORM) != args.AppContext.Host)
					args.ConfigService.SetLocalOverride(Constants.CONFIG_PLATFORM, args.AppContext.Host);
				else
					args.ConfigService.DeleteLocalOverride(Constants.CONFIG_PLATFORM);

				if (args.ConfigService.GetConfigStringIgnoreOverride(Constants.CONFIG_CID) != args.AppContext.Cid)
					args.ConfigService.SetLocalOverride(Constants.CONFIG_CID, args.AppContext.Cid);
				else
					args.ConfigService.DeleteLocalOverride(Constants.CONFIG_CID);

				if (args.ConfigService.GetConfigStringIgnoreOverride(Constants.CONFIG_PID) != args.AppContext.Pid)
					args.ConfigService.SetLocalOverride(Constants.CONFIG_PID, args.AppContext.Pid);
				else
					args.ConfigService.DeleteLocalOverride(Constants.CONFIG_PID);

				args.ConfigService.FlushLocalOverrides();
			}

			res.host = args.ConfigService.GetConfigString(Constants.CONFIG_PLATFORM);
			res.cid = args.ConfigService.GetConfigString(Constants.CONFIG_CID);
			res.pid = args.ConfigService.GetConfigString(Constants.CONFIG_PID);
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
	public bool includeProjectConfig;
}
