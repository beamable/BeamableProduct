using Beamable.Common.Api;
using Beamable.Common.Api.Realms;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesCommandArgs : LoginCommandArgs
{
}

public class ServicesCommand : AppCommand<ServicesCommandArgs>
{
	private IAppContext _ctx;
	// private ConfigService _configService;
	// private LoginCommand _loginCommand;
	// private ConfigCommand _configCommand;
	// private IRealmsApi _realmsApi;
	// private IAliasService _aliasService;
	private BeamoLocalSystem _localBeamo;

	public ServicesCommand()
		: base("services", "Commands that allow interacting with microservices in Beamable project")
	{
	}

	public override void Configure() { }

	public override Task Handle(ServicesCommandArgs args)
	{
		_ctx = args.AppContext;
		_localBeamo = args.BeamoLocalSystem;
		return Task.CompletedTask;
	}
}
