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

public class ServicesCommand : CommandGroup
{
	public ServicesCommand()
		: base("services", "Commands that allow interacting with microservices in Beamable project")
	{
	}
}
