﻿using Beamable.Common.Semantics;
using cli.Dotnet;
using System.CommandLine;

namespace cli.Commands.Project;

public class CreateCommonLibraryArgs : CommandArgs
{
	public ServiceName ProjectName;
	public string SpecifiedVersion;
	public string OutputPath;
}

public class NewCommonLibraryCommand : AppCommand<CreateCommonLibraryArgs>, IStandaloneCommand, IEmptyResult
{
	public NewCommonLibraryCommand() : base("new-common-lib", "Create common library project that later can be connected to the services.")
	{}

	public override void Configure()
	{
		AddArgument(new Argument<ServiceName>("name", "The name of the new library project."),
			(args, i) => args.ProjectName = i);
		AddOption(new SpecificVersionOption(), (args, i) => args.SpecifiedVersion = i);
		AddOption(new Option<string>("--output-path", "The path where the project is going to be created."),
			(args, i) => args.OutputPath = i);
	}

	public override async Task Handle(CreateCommonLibraryArgs args)
	{
		var path = Path.Combine(args.OutputPath, args.ProjectName);
		await args.ProjectService.CreateCommonProject(args.ProjectName.Value, path, args.SpecifiedVersion);
	}
}