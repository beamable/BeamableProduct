using Beamable.Common.BeamCli;
using Beamable.Server;
using cli.Services;
using cli.Utils;
using System.CommandLine;

namespace cli;

public class RealmConfigUseCommandArgs : CommandArgs
{
	public string realm;
	public string game;
}

[CliContractType, Serializable]
public class RealmConfigUseCommandResult
{
	public string pid;
	public string realmName;
	public string gameName;
	public string gamePid;
}

/// <summary>
/// Switches the workspace to a realm picked by name or pid, without the prompts of `beam init`.
/// </summary>
public class RealmConfigUseCommand : AtomicCommand<RealmConfigUseCommandArgs, RealmConfigUseCommandResult>, ISkipManifest
{
	public RealmConfigUseCommand() : base("use", "Target a realm by name or pid (writes its pid to the workspace config)") { }

	public override void Configure()
	{
		AddArgument(new Argument<string>("realm", "The realm's name (as listed by `beam org realms`) or pid"),
			(args, i) => args.realm = i);
		AddOption(new Option<string>("--game", "The game the realm belongs to, by name or pid; needed only when several games have a realm with this name"),
			(args, i) => args.game = i);
	}

	public override async Task<RealmConfigUseCommandResult> GetResult(RealmConfigUseCommandArgs args)
	{
		if (string.IsNullOrWhiteSpace(args.ConfigService.ConfigDirectoryPath))
			throw new CliException("No beamable project exists. Please use beam init");
		if (string.IsNullOrWhiteSpace(args.AppContext.Cid))
			throw new CliException("No cid is configured for this workspace. Please use beam init");

		var (game, realm) = await RealmSelection.Resolve(args.RealmsApi, args.game, args.realm);

		args.ConfigService.WriteConfigString(ConfigService.CFG_JSON_FIELD_PID, realm.Pid);
		// like `beam init`, switching realms clears a local pid override so the new pid takes effect
		args.ConfigService.DeleteLocalOverride(ConfigService.CFG_JSON_FIELD_PID);

		Log.Information($"Now targeting realm '{RealmSelection.RealmLabel(realm)}' in game '{RealmSelection.GameLabel(game)}'.");
		return new RealmConfigUseCommandResult
		{
			pid = realm.Pid,
			realmName = realm.ProjectName,
			gameName = RealmSelection.GameLabel(game),
			gamePid = game.Pid,
		};
	}
}
