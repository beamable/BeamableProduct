using Beamable.Common.BeamCli.Contracts;
using System.CommandLine;

namespace cli.Commands.Project.StorageData;

public class EraseStorageObjectCommandArgs : CommandArgs
{
	public string beamoId;
	
}

public class EraseStorageObjectCommandOutput
{
	
}

public class EraseStorageObjectCommand 
	: AtomicCommand<EraseStorageObjectCommandArgs, EraseStorageObjectCommandOutput>
	, IResultSteam<MongoLogChannel, CliLogMessage>

{
	public EraseStorageObjectCommand() : base("erase", "Clear the data for a storage object")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("beamoId", "the beamoId for the storage object"),
			(args, i) => args.beamoId = i);
	}

	public override async Task<EraseStorageObjectCommandOutput> GetResult(EraseStorageObjectCommandArgs args)
	{
		return await EraseVolumes(args, args.beamoId, this);
	}
	
	public static async Task<EraseStorageObjectCommandOutput> EraseVolumes(CommandArgs args, string beamoId, IResultSteam<MongoLogChannel, CliLogMessage> stream)
	{
		StorageGroupCommand.GetInfo(args, beamoId, out var sd, out var db, out var dockerPath);
		
		// stop the storage if it is running
		var wasRunning = await StorageGroupCommand.EnsureStorageState(args, sd, db, false);
		var argString = $"volume rm --force {db.DataVolumeName} {db.FilesVolumeName}";
		await StorageGroupCommand.RunDockerCommand(args, dockerPath, stream, argString);
		await StorageGroupCommand.EnsureStorageState(args, sd, db, wasRunning);
		
		return new EraseStorageObjectCommandOutput();
	}
}
