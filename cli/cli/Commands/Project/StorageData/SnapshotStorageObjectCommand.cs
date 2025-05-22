using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using cli.Services;
using System.CommandLine;
using Beamable.Server;

namespace cli.Commands.Project.StorageData;

public class SnapshotStorageObjectCommandArgs : CommandArgs
{
	public string beamoId;
	public string outputPath;
}

public class SnapshotStorageObjectCommandOutput
{
	
}

public class MongoLogChannel : IResultChannel
{
	public string ChannelName => "mongoLogs";
}

public class SnapshotStorageObjectCommand 
	: AtomicCommand<SnapshotStorageObjectCommandArgs, SnapshotStorageObjectCommandOutput>
	, IResultSteam<MongoLogChannel, CliLogMessage>
{
	public SnapshotStorageObjectCommand() : base("snapshot", "Create a snapshot of a local Storage Object")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("beamoId", "The beamoId for the storage object"),
			(args, i) => args.beamoId = i);
		AddOption(new Option<string>(new string[] { "--output", "-o" }, () =>
		{
			return "snapshot";
		}, "The output for the snapshot"), (args, i) => args.outputPath = i);
	}

	public override async Task<SnapshotStorageObjectCommandOutput> GetResult(SnapshotStorageObjectCommandArgs args)
	{
		StorageGroupCommand.GetInfo(args, args.beamoId, out var sd, out var db, out var dockerPath);
		
		var wasRunning = await StorageGroupCommand.EnsureStorageState(args, sd, db, true);
		var containerName = BeamoLocalSystem.GetBeamIdAsMongoContainer(sd.BeamoId);
		var argString = $"exec {containerName} mongodump --out=/beamable -u {db.RootUsername} -p {db.RootPassword}";
		var isSuccess = await StorageGroupCommand.RunDockerCommand(args, dockerPath, this, argString);
		Log.Debug($"mongodump success=[{isSuccess}]");

		var copySuccess = await DockerCopyUtil.Copy(dockerPath, containerName, "/beamable/.", args.outputPath,
			DockerCopyUtil.CopyDirection.FROM_CONTAINER);
		
		Log.Debug($"copy success=[{copySuccess}]");
		
		await StorageGroupCommand.EnsureStorageState(args, sd, db, wasRunning);

		return new SnapshotStorageObjectCommandOutput();
		// return $"{DockerCmd} exec {_storage.ContainerName} mongodump --out=/beamable -u {config.LocalInitUser} -p {config.LocalInitPass}";
	}
}
