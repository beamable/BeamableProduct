using Beamable.Common.Api;
using Beamable.Common.Api.Realms;
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
	private readonly BeamoLocalService _localBeamo;

	public ServicesCommand(IAppContext ctx, BeamoLocalService localBeamo)
		: base("services", "Initialize a new beamable project in the current directory.")
	{
		_ctx = ctx;
		_localBeamo = localBeamo;
	}

	public override void Configure() { }

	public override async Task Handle(ServicesCommandArgs args)
	{
		// var asd = await _containerManagement.PullAndCreateImage("mongo:latest", message =>
		// {
		// 	AnsiConsole.WriteLine(JsonConvert.SerializeObject(message));
		// });
		//
		// await _containerManagement.CreateAndRunContainer(asd,
		// 	$"{lastRegistered.BeamoId}_container",
		// 	new List<DockerPortBinding> { new() { LocalPort = "27017", InContainerPort = "27017" } },
		// 	new List<DockerVolume> { new() { VolumeName = $"{lastRegistered.BeamoId}_data", InContainerPath = "/data/db" }, new() { VolumeName = $"{lastRegistered.BeamoId}_files", InContainerPath = "/beamable" } },
		// 	new List<DockerBindMount>(),
		// 	new List<DockerEnvironmentVariable>
		// 	{
		// 		new() { VariableName = "MONGO_INITDB_ROOT_USERNAME", Value = "beamable" }, new() { VariableName = "MONGO_INITDB_ROOT_PASSWORD", Value = "beamable" }
		// 	});
		//
		// AnsiConsole.WriteLine(asd);
	}
}
