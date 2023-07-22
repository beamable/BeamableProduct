using Beamable.Common;
using cli.Services;
using System.CommandLine;

namespace cli;

public class ServicesGetConnectionStringCommandArgs : CommandArgs
{
	public string StorageName;
	public bool IsRemote;
}

public class ServicesGetConnectionStringCommand : AppCommand<ServicesGetConnectionStringCommandArgs>
{
	public ServicesGetConnectionStringCommand() : base("get-connection-string",
		"Gets the Micro-storage connection string")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("storage-name", "The name of the Micro-storage"), (args, i) => args.StorageName = i);
		AddOption(new Option<bool>("--remote", "The Micro-storage remote connection string"), (arg, i) => arg.IsRemote = i);
	}

	public override async Task Handle(ServicesGetConnectionStringCommandArgs args)
	{
		try
		{
			var connectionString = await args.GetLocalOrRemoteConnectionString();
			BeamableLogger.Log($"The connection string for \"{args.StorageName}\" is: {connectionString}");
		}
		catch (Exception ex)
		{
			BeamableLogger.LogError(ex.Message);
		}
	}
}

public static class ServicesGetConnectionStringCommandArgsExtensions
{
	public static async Task<string> GetLocalOrRemoteConnectionString(this ServicesGetConnectionStringCommandArgs args)
	{
		if (args.IsRemote) return await args.BeamoService.GetStorageConnectionString();

		return await args.GetLocalConnectionStringFromDocker();
	}

	private static async Task<string> GetLocalConnectionStringFromDocker(
		this ServicesGetConnectionStringCommandArgs args)
	{
		return (await args.BeamoLocalSystem.GetLocalConnectionString(args.BeamoLocalSystem.BeamoManifest,
			args.StorageName, "localhost")).Value;
	}
}
