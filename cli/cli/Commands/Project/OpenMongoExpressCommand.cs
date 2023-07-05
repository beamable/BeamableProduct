using Beamable.Common;
using Beamable.Common.Semantics;
using cli.Services;
using cli.Utils;
using Docker.DotNet;
using Serilog;
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
		AddArgument(new Argument<ServiceName>("service-name",()=>new ServiceName(),"Name of the storage to open mongo-express to"), (arg, i) => arg.storageName = i);
	}

	public override async Task Handle(OpenMongoExpressCommandArgs args)
	{
		if (string.IsNullOrWhiteSpace(args.storageName))
		{
			var storages = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
				.Where(definition => definition.Protocol == BeamoProtocolType.EmbeddedMongoDb).ToList();
			if (storages.Count == 1)
			{
				args.storageName = new ServiceName(storages[0].BeamoId);
				BeamableLogger.Log(
					$"No service-name passed as argument. Running command for {args.storageName} since it is the only one storage in BeamoManifest.");
			}
			else
			{
				throw new CliException("No service-name passed as argument.");
			}
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

			Log.Information("Starting mongo-express");
			var res = await args.BeamoLocalSystem.GetOrCreateMongoExpress(args.storageName, connStr.Value);

			var port = res.NetworkSettings.Ports["8081/tcp"][0];
			var url = $"http://localhost:{port.HostPort}";
			Log.Information($"Opening web page {url}");
			await Task.Delay(250); // give mongo a chance to boot :(
			MachineHelper.OpenBrowser(url);
		}
		catch (DockerContainerNotFoundException ex)
		{
			Log.Error($"The storage is not running. Please run 'beam services deploy --ids {args.storageName}'");
			return;
		}
		catch (Exception ex)
		{
			BeamableLogger.LogException(ex);
			throw;
		}
	}
}
