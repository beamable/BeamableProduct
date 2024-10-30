using Beamable.Common.BeamCli.Contracts;
using cli.Services;
using CliWrap;
using Docker.DotNet.Models;
using JetBrains.Annotations;
using Serilog;

namespace cli.Commands.Project.StorageData;

public class StorageGroupCommand : CommandGroup
{
	public StorageGroupCommand() : base("storage", "Commands for managing storage data")
	{
	}

	public static async Task<bool> EnsureStorageState(CommandArgs args, BeamoServiceDefinition sd, EmbeddedMongoDbLocalProtocol db, bool desiredState)
	{
		var (isRunning, containerId) = await IsRunning();

		if (isRunning && !desiredState)
		{
			Log.Information("Stopping storage...");
			await args.BeamoLocalSystem.Client.Containers.StopContainerAsync(
				containerId,
				new ContainerStopParameters());
		}

		if (!isRunning && desiredState)
		{
			Log.Information("Starting storage...");
			await args.BeamoLocalSystem.RunLocalEmbeddedMongoDb(sd, db);
		}
		
		return isRunning;
		
		async Task<(bool, string)> IsRunning()
		{
			string containerId = null;
			try
			{
				var containerResponse = await args.BeamoLocalSystem.Client.Containers.InspectContainerAsync(
					BeamoLocalSystem.GetBeamIdAsMongoContainer(sd.BeamoId));
				containerId = containerResponse.ID;

			}
			catch
			{
				containerId = null;
			}

			return (!string.IsNullOrEmpty(containerId), containerId);
		}
	}

	public static void GetInfo(CommandArgs args, string beamoId, out BeamoServiceDefinition definition, out EmbeddedMongoDbLocalProtocol db, out string dockerPath)
	{
		if (!args.BeamoLocalSystem.BeamoManifest.TryGetDefinition(beamoId,out definition))
		{
			throw new CliException($"no service definition available for id=[{beamoId}]");
		}

		if (!args.BeamoLocalSystem.BeamoManifest.EmbeddedMongoDbLocalProtocols.TryGetValue(definition.BeamoId, out db))
		{
			throw new CliException($"no local storage object exists for id=[{definition.BeamoId}]");
		}
		
		dockerPath = args.AppContext.DockerPath;
		if (!DockerPathOption.TryValidateDockerExec(dockerPath, out var dockerPathError))
		{
			throw new CliException(dockerPathError);
		}
	}
	
	public static async Task<bool> RunDockerCommand(CommandArgs args, string dockerPath, IResultSteam<MongoLogChannel, CliLogMessage> stream, string dockerArg, bool streamLogs=true)
	{
			
		Log.Verbose($"docker exec string=[{dockerPath} {dockerArg}]");

		var command = Cli
			.Wrap(dockerPath)
			.WithArguments(dockerArg)
			.WithValidation(CommandResultValidation.None)
			.WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
			{
				if (streamLogs)
				{
					var log = CliLogMessage.FromStringNow(line);
					stream.SendResults(log);
				}

				Log.Debug(line);
			}))
			.WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
			{
				if (streamLogs)
				{
					var log = CliLogMessage.FromStringNow(line, "Error");
					stream.SendResults(log);
				}

				Log.Error(line);
			}));

		var res = await command.ExecuteAsync();
		return res.ExitCode == 0;
	}
}
