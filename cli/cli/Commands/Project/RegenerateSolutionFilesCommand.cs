using Beamable.Common;
using Beamable.Common.Semantics;
using cli.Dotnet;
using System.CommandLine;

namespace cli.Commands.Project;

public class RegenerateSolutionFilesCommand : AppCommand<RegenerateSolutionFilesCommandArgs>, IStandaloneCommand, IEmptyResult
{
	public RegenerateSolutionFilesCommand() : 
		base("regenerate", "Regenerate the solution csproj, Dockerfile and Program.cs files.")
	{
	}

	public override void Configure()
	{
		AddArgument(new ServiceNameArgument(), (args, i) => args.ProjectName = i);
		AddArgument(new Argument<string>("output", () => string.Empty, description: "Where the temp project will be created"),
			(args, i) => args.tempDirectory = i);
		AddArgument(new Argument<string>("copy-path", () => string.Empty, description: "The path to where the files will be copied to."),
			(args, i) => args.projectDirectory = i);
		AddOption(new SkipCommonOptionFlag(), (args, i) => args.SkipCommon = i);
		AddOption(new Option<ServiceName>("--solution-name", "The name of the solution of the new project"),
			(args, i) => args.SolutionName = i);
		AddOption(new SpecificVersionOption(), (args, i) => args.SpecifiedVersion = i);
	}

	public override async Task Handle(RegenerateSolutionFilesCommandArgs args)
	{
		BeamableLogger.Log("Start creating a temporary project!");
		
		args.SolutionName = string.IsNullOrEmpty(args.SolutionName) ? args.ProjectName : args.SolutionName;

		var solutionArgs = new NewSolutionCommandArgs()
		{
			directory = args.tempDirectory,
			SolutionName = args.ProjectName,
			ProjectName = args.ProjectName,
			Quiet = true,
		};
		
		//Create the temporary project to have the files to copy
		var path = await args.ProjectService.CreateNewSolution(solutionArgs);

		var filesToCopy = new string[3]
		{
			"Program.cs",
			"Dockerfile",
			$"{args.SolutionName}.csproj"
		};

		//Copy all files to the desired path
		foreach (var fileName in filesToCopy)
		{
			BeamableLogger.Log($"Copying file {fileName} ...");
			var filePath = $"{path}/services/{args.SolutionName}/{fileName}";
			File.Copy(filePath, $"{args.projectDirectory}/{fileName}", true);
		}
		
		BeamableLogger.Log($"Finished copying files.");
		
		//Starts erasing the temporary project created
		DeleteTempProject(path);
		
		BeamableLogger.Log($"Files regenerated successfully!");
	}

	private void DeleteTempProject(string path)
	{
		var di = new DirectoryInfo(path);

		BeamableLogger.Log($"Started erasing files and directories");
		foreach (FileInfo file in di.EnumerateFiles())
		{
			file.Delete();
		}

		foreach (DirectoryInfo dir in di.EnumerateDirectories())
		{
			dir.Delete(true);
		}
		di.Delete(true);
	}
}
