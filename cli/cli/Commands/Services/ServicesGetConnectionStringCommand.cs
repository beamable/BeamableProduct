using Beamable.Common;
using cli.Services;
using Spectre.Console;
using System.CommandLine;

namespace cli;

public class ServicesGetConnectionStringCommandArgs : CommandArgs
{
	public string StorageName;
	public bool IsRemote;
	public bool IsQuiet;
}

public class ServicesGetConnectionStringCommandOutput
{
	public string connectionString;
}
public class ServicesGetConnectionStringCommand : AtomicCommand<ServicesGetConnectionStringCommandArgs, ServicesGetConnectionStringCommandOutput>
{
	public ServicesGetConnectionStringCommand() : base("get-connection-string",
		"Gets the Microstorage connection string")
	{
	}

	public override void Configure()
	{
		var remoteOptions = new[] { "-r", "--remote" };
		var quietOptions = new[] { "-q", "--quiet" };

		AddArgument(new Argument<string>("storage-name", "The name of the Microstorage"),
			(args, i) => args.StorageName = i);
		AddOption(new Option<bool>(remoteOptions, "The Microstorage remote connection string"),
			(arg, i) => arg.IsRemote = i);
		AddOption(new Option<bool>(quietOptions, "Ignores confirmation step"),
			(arg, i) => arg.IsQuiet = i);
	}

	public override async Task<ServicesGetConnectionStringCommandOutput> GetResult(ServicesGetConnectionStringCommandArgs args)
	{
		var canProceed = args.IsQuiet || AnsiConsole.Confirm(
			"[yellow]WARNING:[/] The MongoDB connection string allows full read/write access to your Storage. Before proceeding, make sure you are in a secure environment and your screen is not visible to anyone unauthorized. Proceed?",
			false);

		if (!canProceed)
		{
			return new ServicesGetConnectionStringCommandOutput();
		}

		try
		{
			var connectionString = await args.GetLocalOrRemoteConnectionString();
			return new ServicesGetConnectionStringCommandOutput { connectionString = connectionString };
		}
		catch (Exception ex)
		{
			throw new CliException(ex.Message);
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
