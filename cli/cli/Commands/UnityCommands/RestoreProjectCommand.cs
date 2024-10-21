using cli.Services;
using CliWrap;
using System.CommandLine;

namespace cli.UnityCommands;

public class RestoreProjectCommandArgs : CommandArgs
{
	public string csProjPath;
}

public class RestoreProjectCommandOutput
{
	
}

public class RestoreProjectCommand : AtomicCommand<RestoreProjectCommandArgs, RestoreProjectCommandOutput>
{
	public RestoreProjectCommand() : base("restore", "Restore a dotnet project")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--csproj", "the path to the dotnet csproj path"),
			(args, i) => args.csProjPath = i);
	}

	public override async Task<RestoreProjectCommandOutput> GetResult(RestoreProjectCommandArgs args)
	{
		await CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, $"build {args.csProjPath}")
			.WithValidation(CommandResultValidation.None)
			.ExecuteAsyncAndLog();
		
		return new RestoreProjectCommandOutput();
	}
}
