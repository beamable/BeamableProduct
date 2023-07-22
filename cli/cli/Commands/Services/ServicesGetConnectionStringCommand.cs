using Beamable.Common;
using System.CommandLine;

namespace cli;

public class ServicesGetConnectionStringCommandArgs : CommandArgs
{
	public string StorageName;
	public bool IsRemote;
}

public class ServicesGetConnectionStringCommand : AppCommand<ServicesGetConnectionStringCommandArgs>
{
	public ServicesGetConnectionStringCommand() : base("get-connection-string", "Gets the Micro-storage connection string")
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
			var connStr = await args.BeamoLocalSystem.GetLocalConnectionString(
				args.BeamoLocalSystem.BeamoManifest, args.StorageName, "localhost");
			BeamableLogger.Log($"The connection string for \"{args.StorageName}\" is: {connStr.Value}");
		}
		catch (Exception ex)
		{
			BeamableLogger.LogError(ex.Message);
		}
	}
}
