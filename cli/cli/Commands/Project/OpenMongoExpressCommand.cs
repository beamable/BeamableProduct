using Beamable.Common;
using cli.Utils;
using Docker.DotNet;
using Serilog;
using System.CommandLine;

namespace cli.Commands.Project;

public class OpenMongoExpressCommandArgs : CommandArgs
{
	public bool isRemote;
	public string storageName;
}

public class OpenMongoExpressCommand : AppCommand<OpenMongoExpressCommandArgs>
{
	public OpenMongoExpressCommand() : base("open-mongo", "Opens a Mongo-Express web page for the given mongo storage object")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("service-name", "Name of the storage to open mongo-express to"), (arg, i) => arg.storageName = i);
		AddOption(new Option<bool>("--remote", "If passed, Mongo-express will open to the remote version of this storage. Otherwise, it will try and use the local version"), (arg, i) => arg.isRemote = i);
	}

	public override async Task Handle(OpenMongoExpressCommandArgs args)
	{
		// first, get the local connection string,
		if (args.isRemote)
		{
			HandleRemoteCase(args);
		}
		else
		{
			await HandleLocalCase(args);
		}
	}

	void HandleRemoteCase(OpenMongoExpressCommandArgs args)
	{
		throw new NotImplementedException("Remote mongo-express is not supported at the moment.");
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
			var url = $"http://{port.HostIP}:{port.HostPort}";
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
