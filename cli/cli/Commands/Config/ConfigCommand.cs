using Beamable.Common;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.CommandLine;
using System.CommandLine.Binding;

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

	public override async Task<ConfigCommandResult> GetResult(ConfigCommandArgs args)
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
					// only set overrides when they are explicitly passed as flags. 
					var binding = args.DependencyProvider.GetService<BindingContext>();
					var selectedCid = binding.ParseResult.GetValueForOption(CidOption.Instance);
					var selectedPid = binding.ParseResult.GetValueForOption(PidOption.Instance);
					var selectedHost = binding.ParseResult.GetValueForOption(HostOption.Instance);
					
					if (selectedHost != null)
						ConfigService.SetConfig(ConfigService.CFG_JSON_FIELD_HOST, selectedHost, config);
					
					if (selectedCid != null)
						ConfigService.SetConfig(ConfigService.CFG_JSON_FIELD_CID, selectedCid, config);
					
					if (selectedPid != null)
						ConfigService.SetConfig(ConfigService.CFG_JSON_FIELD_PID, selectedPid, config);
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

			res.host = args.ConfigService.GetConfigString(ConfigService.CFG_JSON_FIELD_HOST);
			res.cid = args.ConfigService.GetConfigString(ConfigService.CFG_JSON_FIELD_CID);
			res.pid = args.ConfigService.GetConfigString(ConfigService.CFG_JSON_FIELD_PID);
		}

		res.zid = await ResolveZid(args, res.pid, args.IgnoreOverrides);

		return res;
	}

	/// <summary>
	/// Resolves the effective zone id for the current configuration.
	/// <para>
	/// When a pid is selected, the realm's zone binding is authoritative: it always wins over any locally
	/// configured zid, and a realm that is not bound to a zone resolves to "no zone" (null) regardless of
	/// the local value. When no pid is selected, we fall back to the zid stored in the .beamable config.
	/// </para>
	/// </summary>
	static async Task<string> ResolveZid(ConfigCommandArgs args, string pid, bool ignoreOverrides)
	{
		var localZid = ignoreOverrides
			? args.ConfigService.GetConfigStringIgnoreOverride(ConfigService.CFG_JSON_FIELD_ZID)
			: args.ConfigService.GetConfigString(ConfigService.CFG_JSON_FIELD_ZID);

		try
		{
			// AppContext.Cid is alias-resolved to the numeric cid the customers/{cid}/realms endpoint needs.
			return await ZoneResolver.ResolveZid(args.DependencyProvider, args.AppContext.Cid, pid, localZid);
		}
		catch (Exception ex)
		{
			// Do not fail `beam config` (which is otherwise offline-friendly) if the realm's zone cannot be
			// resolved — report it as unresolved instead.
			BeamableLogger.LogWarning($"Could not resolve the zone bound to realm=[{pid}]: {ex.Message}");
			return null;
		}
	}
}

public class ConfigCommandResult
{
	public string host;
	public string cid;
	public string pid;
	/// <summary>
	/// The effective zone id. When a pid is selected this is the pid's realm zone (null means the realm is
	/// bound to no zone); otherwise it is the zid stored in the local .beamable config.
	/// </summary>
	public string zid;
	public string configPath;
}

public class ConfigCommandArgs : CommandArgs
{
	public bool IgnoreOverrides;
	public bool IsSet;
}
