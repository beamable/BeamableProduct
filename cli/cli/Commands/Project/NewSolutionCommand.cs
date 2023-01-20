using Beamable.Common;
using cli.Services;
using CliWrap;
using Serilog;
using System.CommandLine;
using UnityEngine;

namespace cli.Dotnet;

public class NewSolutionCommandArgs : CommandArgs
{
	public string name;
	public string directory;
}

public class NewSolutionCommand : AppCommand<NewSolutionCommandArgs>
{
	public NewSolutionCommand() : base("new", "start a brand new beamable solution using dotnet")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("name", "the name of the new project"), (args, i) => args.name = i);
		AddArgument(new Argument<string>("output", () => "", description: "where the project be created"), (args, i) => args.directory = i);
	}

	public override async Task Handle(NewSolutionCommandArgs args)
	{
		// in the current directory, create a project using dotnet. 
		await args.ProjectService.CreateNewSolution(args.directory, args.name, args.name);

		// initialize a beamable project in that directory...
		
	}
}
