using Beamable.Common.Api.Realms;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesPromoteCommandArgs : LoginCommandArgs
{
	public string SourcePid;
}

public class ServicesPromoteCommand : AtomicCommand<ServicesPromoteCommandArgs, ManifestChecksums>
{
	public static readonly Option<string> SOURCE_PID_OPTION = new("--source-pid", "The PID for the realm from which you wish to pull the manifest from. " +
																				 "\nThe current realm you are signed into will be updated to match the manifest in the given realm");

	private IAppContext _ctx;
	private IRealmsApi _realms;
	private BeamoService _remoteBeamo;


	public ServicesPromoteCommand() :
		base("promote",
			"Promotes the manifest from the given 'sourcePid' to your current realm")
	{
	}

	public override void Configure()
	{
		AddOption(SOURCE_PID_OPTION, (args, s) => args.SourcePid = s);
	}

	public override async Task<ManifestChecksums> GetResult(ServicesPromoteCommandArgs args)
	{

		_ctx = args.AppContext;
		_realms = args.RealmsApi;
		_remoteBeamo = args.BeamoService;
		// Make sure we are up-to-date with the remote manifest
		var realms = await _realms.GetRealms(_ctx.Pid);
		var possiblePids = realms.Select(r => r.Pid);
		if (string.IsNullOrEmpty(args.SourcePid))
			args.SourcePid = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Choose the [lightskyblue1]PID[/] to Promote to the current realm:")
				.AddChoices(possiblePids)
				.AddBeamHightlight());

		ManifestChecksums response = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Default)
			.StartAsync("Sending Request...", async ctx =>
				await _remoteBeamo.Promote(args.SourcePid)
			);

		return PrintResult(response);
	}
}
