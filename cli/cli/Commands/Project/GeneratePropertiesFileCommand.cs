using cli.Services;
using Serilog;
using System.CommandLine;

namespace cli.Dotnet;

public class GeneratePropertiesFileCommandArgs : CommandArgs
{
	public string OutputPath;
	public string BeamPath;
	public string SolutionDir;
	public string RelativeBuildDir;
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
		AddArgument(new Argument<string>("beam-path", description: "Beam path to be used. Use BEAM_SOLUTION_DIR to template in $(SolutionDir)"),
			(args, i) => args.BeamPath = i.Replace("BEAM_SOLUTION_DIR", "$(SolutionDir)"));
		AddArgument(new Argument<string>("solution-dir", description: @"The solution path to be used. 
The following values have special meaning and are not treated as paths... 
- """ + Beamable.Common.BeamCli.Contracts.CliConstants.GENERATE_PROPS_SLN_NEXT_TO_PROPS + @""" = $([System.IO.Path]::GetDirectoryName(`$(DirectoryBuildPropsPath)`)) "),
			(args, i) =>
			{
				if (i == Beamable.Common.BeamCli.Contracts.CliConstants.GENERATE_PROPS_SLN_NEXT_TO_PROPS)
				{
					args.SolutionDir = "$([System.IO.Path]::GetDirectoryName(`$(DirectoryBuildPropsPath)`))";
				}
				else
				{
					args.SolutionDir = i;
				}
			});


		AddOption(new Option<string>("--build-dir", description: "A path relative to the given solution directory, that will be used to store the projects /bin and /obj directories. Note: the given path will have the project's assembly name and the bin or obj folder appended"),
			(args, i) => args.RelativeBuildDir = i);
	}

	public override Task Handle(GeneratePropertiesFileCommandArgs args)
	{
		if (!Directory.Exists(args.OutputPath))
		{
			throw new CliException($"Output path argument passed does not exist. path=[{args.OutputPath}]");
		}


		const string buildDirFlag = "BUILD_DIR_OPTIONS";

		// this line could be used on the second propertyGroup to only apply those values to project that are considered beamable projects.
		//  however, this won't work until we are capturing all referenced projects as well.
		//  Condition="$(BeamableServiceIds.Contains('$(MSBuildProjectName)|'))"

		var BeamableServiceIds = string.Join("|", args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Select(x => x.BeamoId)) + "|";

		string fileContents = @$"
<Project>
	<PropertyGroup>
		<!-- BeamableServiceIds is a list of known beamoIds at the time of Directory.Build.Props generation. -->
		<BeamableServiceIds>{BeamableServiceIds}</BeamableServiceIds>
	</PropertyGroup>

	<PropertyGroup>
		<!-- Path configurations -->
		<SolutionDir Condition=""'$(SolutionDir)' == ''"">{args.SolutionDir}</SolutionDir>

{buildDirFlag}
	</PropertyGroup>
</Project>";

		var buildDirXml = "";
		if (!string.IsNullOrEmpty(args.RelativeBuildDir))
		{

			var objDir = Path.Combine(args.RelativeBuildDir, "$(MSBuildProjectName)", "obj");
			var binDir = Path.Combine(args.RelativeBuildDir, "$(MSBuildProjectName)", "bin");
			Log.Verbose("obj " + objDir);
			Log.Verbose("bin " + binDir);
			buildDirXml = @$"
		<!-- Hide obj and bin folders -->
		<BaseOutputPath>$(SolutionDir)/{binDir}</BaseOutputPath>
        <BaseIntermediateOutputPath>$(SolutionDir)/{objDir}</BaseIntermediateOutputPath>
";
		}

		fileContents = fileContents.Replace(buildDirFlag, buildDirXml);

		var path = Path.Combine(args.OutputPath, "Directory.Build.props");
		File.WriteAllText(path, fileContents);

		return Task.CompletedTask;
	}
}
