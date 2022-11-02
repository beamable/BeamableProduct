using Beamable.Common.Api.Realms;
using cli.Services;
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

public class ServicesPromoteCommand : AppCommand<ServicesPromoteCommandArgs>
{
	public static readonly Option<string> SOURCE_PID_OPTION = new("--sourcePid", "The PID for the realm from which you wish to pull the manifest from. " +
																				 "\nThe current realm you are signed into will be updated to match the manifest in the given realm.");

	private readonly IAppContext _ctx;
	private readonly IRealmsApi _realms;
	private readonly BeamoService _remoteBeamo;


	public ServicesPromoteCommand(IAppContext ctx, IRealmsApi realms, BeamoService remoteBeamo) :
		base("promote",
			"Promotes the manifest from the given 'sourcePid' to your current realm.")
	{
		_ctx = ctx;
		_realms = realms;
		_remoteBeamo = remoteBeamo;
	}

	public override void Configure()
	{
		AddOption(SOURCE_PID_OPTION, (args, s) => args.SourcePid = s);
	}

	public override async Task Handle(ServicesPromoteCommandArgs args)
	{
		// Make sure we are up-to-date with the remote manifest
		var realms = await _realms.GetRealms(_ctx.Pid);
		var possiblePids = realms.Select(r => r.Pid);
		if (string.IsNullOrEmpty(args.SourcePid))
			args.SourcePid = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Choose the [lightskyblue1]PID[/] to Promote to the current realm:").AddChoices(possiblePids));

		var response = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Default)
			.StartAsync("Sending Request...", async ctx =>
				await _remoteBeamo.Promote(args.SourcePid)
			);

		Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
	}
}
