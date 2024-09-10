using Beamable.Common;
using Beamable.Common.Semantics;
using cli.Services;
using cli.Utils;
using Docker.DotNet;
using Serilog;
using Spectre.Console;
using System.CommandLine;

namespace cli.Commands.Project;

public class OpenMongoExpressCommandArgs : CommandArgs
{
	public ServiceName storageName;
}

public class OpenMongoExpressCommand : AppCommand<OpenMongoExpressCommandArgs>
{
	public OpenMongoExpressCommand() : base("open-mongo", "Opens a Mongo-Express web page for the given mongo storage object")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<ServiceName>("service-name", () => new ServiceName(), "Name of the storage to open mongo-express to"), (arg, i) => arg.storageName = i);
	}

	public override async Task Handle(OpenMongoExpressCommandArgs args)
	{
		if (string.IsNullOrWhiteSpace(args.storageName))
		{
			var storages = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
				.Where(definition => definition.Protocol == BeamoProtocolType.EmbeddedMongoDb).ToList();

			switch (storages.Count)
			{
				case 1:
					args.storageName = new ServiceName(storages[0].BeamoId);
					Log.Debug($"No service-name passed as argument. " +
							  $"Running command for {args.storageName} since it is the only storage in BeamoManifest.");
					break;
				case > 1:
					Log.Information("Found more than one storage in the directory");
					args.storageName = new ServiceName(AskForStorageAndRunBeamCommandTask(storages));
					return;
			}
		}

		if (string.IsNullOrWhiteSpace(args.storageName))
		{
			throw new CliException("No storage found. Navigate to a .beamable workspace with valid Microstorages. ");
		}

		// first, get the local connection string,
		await HandleLocalCase(args);
	}

	async Task HandleLocalCase(OpenMongoExpressCommandArgs args)
	{
		try
		{
			Log.Information("Finding local connection string...");
			var connStr = await args.BeamoLocalSystem.GetLocalConnectionString(args.BeamoLocalSystem.BeamoManifest, args.storageName);

			Log.Information($"ConnStr: {connStr.Value}");
			Log.Information("Starting mongo-express");
			var res = await args.BeamoLocalSystem.GetOrCreateMongoExpress(args.storageName, connStr.Value);

			var port = res.NetworkSettings.Ports["8081/tcp"][0];
			var url = $"http://localhost:{port.HostPort}";
			Log.Information($"Opening web page {url}");
			await Task.Delay(250); // give mongo a chance to boot :(
			MachineHelper.OpenBrowser(url);
		}
		catch (DockerContainerNotFoundException)
		{
			Log.Error($"The storage is not running. Please run 'beam services run --ids {args.storageName}'");
			return;
		}
		catch (Exception ex)
		{
			BeamableLogger.LogException(ex);
			throw;
		}
	}

	private static string AskForStorageAndRunBeamCommandTask(
		IEnumerable<BeamoServiceDefinition> storages)
	{
		return AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select the storage to use:")
				.PageSize(10)
				.MoreChoicesText("[grey](Move up and down to reveal more storage)[/]")
				.AddChoices(storages.Select(serviceDef => serviceDef.BeamoId))
				.AddBeamHightlight());
	}
}
