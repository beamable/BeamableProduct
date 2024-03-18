using System.CommandLine;

namespace cli.Dotnet;

public class GeneratePropertiesFileCommandArgs : CommandArgs
{
	public string OutputPath;
	public string BeamPath;
	public string SolutionDir;
}

public class GeneratePropertiesFileCommand : AppCommand<GeneratePropertiesFileCommandArgs>, IEmptyResult
{


	public GeneratePropertiesFileCommand() : base("generate-properties", "Generates a Directory.Build.props file with the beam path and solution dir")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("output", description: "Where the file will be created"),
			(args, i) => args.OutputPath = i);
		AddArgument(new Argument<string>("beam-path", description: "Beam path to be used"),
			(args, i) => args.BeamPath = i);
		AddArgument(new Argument<string>("solution-dir", description: "The solution path to be used"),
			(args, i) => args.SolutionDir = i);
	}

	public override Task Handle(GeneratePropertiesFileCommandArgs args)
	{
		if (!Directory.Exists(args.OutputPath))
		{
			throw new CliException("Output path argument passed does not exist.");
		}

		if (!Directory.Exists(args.SolutionDir))
		{
			throw new CliException("SolutionDir path must exist.");
		}

		string fileContents = @$"
<Project>
 <PropertyGroup>
     <SolutionDir Condition=""'$(SolutionDir)' == ''"">{args.SolutionDir}</SolutionDir>
     <BeamableTool>{args.BeamPath}</BeamableTool>
 </PropertyGroup>
</Project>";
		var path = Path.Combine(args.OutputPath, "Directory.Build.props");
		File.WriteAllText(path, fileContents);

		return Task.CompletedTask;
	}
}
