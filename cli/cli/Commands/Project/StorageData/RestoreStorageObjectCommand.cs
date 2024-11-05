using Beamable.Common.BeamCli.Contracts;
using cli.Services;
using CliWrap;
using Serilog;
using System.CommandLine;

namespace cli.Commands.Project.StorageData;

public class RestoreStorageObjectCommandArgs : CommandArgs
{
	public string beamoId;
	public string snapshotFolder;
	public bool hardReset;
}

public class RestoreStorageObjectCommandOutput
{

}
public class RestoreStorageObjectCommand
	: AtomicCommand<RestoreStorageObjectCommandArgs, RestoreStorageObjectCommandOutput>
	, IResultSteam<MongoLogChannel, CliLogMessage>
{
	public RestoreStorageObjectCommand() : base("restore", "Restore a storage object from a snapshot folder")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("beamoId", "The beamoId for the storage object"),
			(args, i) => args.beamoId = i);
		AddOption(
			new Option<bool>(new string[] { "--merge", "-m" }, "When true, merges the snapshot into the existing data"),
			(args, i) => args.hardReset = !i);
		AddOption(new Option<string>(new string[] { "--input", "-i" }, () =>
		{
			return "snapshot";
		}, "The input for the snapshot"), (args, i) => args.snapshotFolder = i);
	}

	public override async Task<RestoreStorageObjectCommandOutput> GetResult(RestoreStorageObjectCommandArgs args)
	{

		if (args.hardReset)
		{
			await EraseStorageObjectCommand.EraseVolumes(args, args.beamoId, this);
		}

		StorageGroupCommand.GetInfo(args, args.beamoId, out var sd, out var db, out var dockerPath);

		var wasRunning = await StorageGroupCommand.EnsureStorageState(args, sd, db, true);
		var containerName = BeamoLocalSystem.GetBeamIdAsMongoContainer(sd.BeamoId);
		var copySuccess = await DockerCopyUtil.Copy(dockerPath, containerName, "/beamable/.", args.snapshotFolder + "/.",
			DockerCopyUtil.CopyDirection.INTO_CONTAINER);

		Log.Debug($"copy success=[{copySuccess}]");

		var mongoAvailable = false;
		do
		{
			var connStr = $"mongodb://{db.RootUsername}:{db.RootPassword}@localhost/?authSource=admin";
			mongoAvailable = await StorageGroupCommand.RunDockerCommand(args, dockerPath, this, $"exec {containerName} mongosh {connStr} ", false);
		} while (!mongoAvailable);


		var argString = $"exec {containerName} mongorestore /beamable -u {db.RootUsername} -p {db.RootPassword}";

		var isSuccess = await StorageGroupCommand.RunDockerCommand(args, dockerPath, this, argString);


		Log.Debug($"mongorestore success=[{isSuccess}]");
		await StorageGroupCommand.EnsureStorageState(args, sd, db, wasRunning);

		return new RestoreStorageObjectCommandOutput();
	}
}
