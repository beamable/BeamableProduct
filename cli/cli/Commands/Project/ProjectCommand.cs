
using Serilog;
using System.CommandLine;

namespace cli.Dotnet;

public class ProjectCommand : CommandGroup
{
	public ProjectCommand() : base(
		"project",
		"Commands that relate to a standalone Beamable project")
	{
	}

	public static void AddWatchOption<TArgs>(AppCommand<TArgs> command, Action<TArgs, bool> binder)
		where TArgs : CommandArgs
	{
		var option = new Option<bool>(
			name: "--watch",
			description: "When true, the command will run forever and watch the state of the program");
		option.AddAlias("-w");
		command.AddOption(option, binder);
	}

	public static void AddIdsOption<TArgs>(AppCommand<TArgs> command, Action<TArgs, List<string>> binder)
		where TArgs : CommandArgs
	{
		command.AddOption(new Option<List<string>>(
			name: "--ids",
			description: "The list of services to build, defaults to all local services")
		{
			AllowMultipleArgumentsPerToken = true,
			Arity = ArgumentArity.ZeroOrMore
		}, binder);
	}

	public static void FinalizeServicesArg(CommandArgs args, ref List<string> services)
	{
		if (services == null || services.Count == 0)
		{
			services = args.BeamoLocalSystem
				.BeamoManifest?
				.HttpMicroserviceLocalProtocols?
				.Select(x => x.Key)
				.ToList() ?? new List<string>();
		}

		if (services.Count == 0)
		{
			throw new CliException("No services are listed.");
		}

		Log.Debug("using services " + string.Join(",", services));
	}
}
