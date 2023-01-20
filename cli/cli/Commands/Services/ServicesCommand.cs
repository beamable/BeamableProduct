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
	private readonly IAppContext _ctx;
	private readonly ConfigService _configService;
	private readonly LoginCommand _loginCommand;
	private readonly ConfigCommand _configCommand;
	private readonly IRealmsApi _realmsApi;
	private readonly IAliasService _aliasService;
	private readonly BeamoLocalSystem _localBeamo;

	public ServicesCommand(IAppContext ctx, BeamoLocalSystem localBeamo)
		: base("services", "Initialize a new beamable project in the current directory.")
	{
		_ctx = ctx;
		_localBeamo = localBeamo;
	}

	public override void Configure() { }

	public override Task Handle(ServicesCommandArgs args)
	{
		return Task.CompletedTask;
	}
}
