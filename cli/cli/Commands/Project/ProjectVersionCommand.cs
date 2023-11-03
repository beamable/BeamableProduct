using cli.Services;
using CliWrap;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;
using System.CommandLine;
using System.Text;

namespace cli.Commands.Project;

public class ProjectVersionCommandArgs : CommandArgs
{
	public string requestedVersion;
}

public class ProjectVersionCommand : AppCommand<ProjectVersionCommandArgs>, IResultSteam<DefaultStreamResultChannel, ProjectVersionCommandResult>
{
	public ProjectVersionCommand() : base(
		"version",
		"Commands that lists Beamable package versions of a SAMS projects")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--requested-version", "Request specific version of Beamable packages."),
			(args, i) => args.requestedVersion = i.Trim());
	}

	public override async Task Handle(ProjectVersionCommandArgs args)
	{
		var projectList = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols.Values.ToList();
		List<BeamablePackageInProject> results = new();
		foreach (var project in projectList)
		{
			var buffer = new StringBuilder();
			var solutionPath = Directory.GetParent(Path.Combine(args.ConfigService.BaseDirectory, project.RelativeDockerfilePath));
			await CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, "sln list").WithWorkingDirectory(solutionPath.FullName)
				.WithStandardOutputPipe(PipeTarget.ToStringBuilder(buffer)).ExecuteAsync();
			var projectsPaths = buffer.ToString().Replace("\r\n", "\n").Split("\n").Where(s => s.EndsWith(".csproj"))
				.Select(p => Directory.GetParent(Path.Combine(solutionPath.FullName, p)).FullName).ToList();
			foreach (string projectPath in projectsPaths)
			{
				buffer.Clear();
				await CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, "list package")
					.WithWorkingDirectory(projectPath)
					.WithStandardOutputPipe(PipeTarget.ToStringBuilder(buffer)).ExecuteAsync();
				var result = buffer.ToString().Replace("\r\n","\n").Split("\n").Where(s=>s.Contains("> Beamable.")).ToList();
				
				foreach (var line in result)
				{
					var splitedLine = line.Split(" ",
						StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
					var packageIndex = splitedLine.FindIndex(s => s.StartsWith("Beamable."));
					var package = new BeamablePackageInProject()
					{
						projectPath = projectPath,
						packageName = splitedLine[packageIndex],
						packageVersion = splitedLine[packageIndex + 1]
					};
					results.Add(package);

					if (!string.IsNullOrWhiteSpace(args.requestedVersion) &&
					    !args.requestedVersion.Equals(package.packageVersion))
					{
						buffer.Clear();
						await CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, $"add package {package.packageName} --version \"{args.requestedVersion}\"")
							.WithWorkingDirectory(projectPath)
							.WithStandardOutputPipe(PipeTarget.ToStringBuilder(buffer)).ExecuteAsync();
						AnsiConsole.WriteLine(buffer.ToString());
					}
				}
			}
		}
		var json = JsonConvert.SerializeObject(results);
		AnsiConsole.Write(
			new Panel(new JsonText(json))
				.Header("Server response")
				.Collapse()
				.RoundedBorder());
		this.SendResults(BeamablePackageInProject.ToResult(results));
	}
}


[Serializable]
public class BeamablePackageInProject
{
	public string projectPath;
	public string packageName;
	public string packageVersion;

	public static ProjectVersionCommandResult ToResult(IList<BeamablePackageInProject> list)
	{
		return new ProjectVersionCommandResult()
		{
			projectPaths = list.Select(e => e.projectPath).ToArray(),
			packageNames = list.Select(e => e.packageName).ToArray(),
			packageVersions = list.Select(e => e.packageVersion).ToArray(),
		};
	}
}

[Serializable]
public class ProjectVersionCommandResult
{
	public string[] projectPaths;
	public string[] packageNames;
	public string[] packageVersions;
}
