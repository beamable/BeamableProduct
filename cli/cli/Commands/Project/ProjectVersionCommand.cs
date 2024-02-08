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

public class ProjectVersionCommand : AtomicCommand<ProjectVersionCommandArgs, ProjectVersionCommandResult>
{
	public ProjectVersionCommand() : base(
		"version",
		"Commands that lists Beamable package versions of a SAMS projects")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--requested-version", "Request specific version of Beamable packages"),
			(args, i) => args.requestedVersion = string.IsNullOrWhiteSpace(i) ? string.Empty : i.Trim());
	}

	public override async Task<ProjectVersionCommandResult> GetResult(ProjectVersionCommandArgs args)
	{
		var projectList = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols.Values
			.Where(p => !string.IsNullOrWhiteSpace(p.RelativeDockerfilePath)).ToList();
		List<BeamablePackageInProject> results = new();
		foreach (var project in projectList)
		{
			var solutionPath =
				Directory.GetParent(Path.Combine(args.ConfigService.BaseDirectory, project.RelativeDockerfilePath));

			var (_, buffer) =
				await CliExtensions.RunWithOutput(args.AppContext.DotnetPath, "sln list",
					solutionPath!.FullName);

			var projectsPaths = buffer.ToString().ReplaceLineEndings("\n").Split("\n").Where(s => s.EndsWith(".csproj"))
				.Select(p => Directory.GetParent(Path.Combine(solutionPath.FullName, p))!.FullName).ToList();
			foreach (string projectPath in projectsPaths)
			{
				(_, buffer) =
					await CliExtensions.RunWithOutput(args.AppContext.DotnetPath, "list package", projectPath);
				var result = buffer.ToString().ReplaceLineEndings("\n").Split("\n").Where(s => s.Contains("> Beamable."))
					.ToList();

				foreach (var line in result)
				{
					var splitedLine = line.Split(" ",
						StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
					var packageIndex = splitedLine.FindIndex(s => s.StartsWith("Beamable."));
					var packageName = splitedLine[packageIndex];
					var packageVersion = splitedLine[packageIndex + 1];

					if (!string.IsNullOrWhiteSpace(args.requestedVersion) &&
					    !args.requestedVersion.Equals(packageVersion))
					{
						(_, buffer) = await CliExtensions.RunWithOutput(args.AppContext.DotnetPath,
								$"add package {packageName} --version \"{args.requestedVersion}\"", projectPath);
						AnsiConsole.WriteLine(buffer.ToString());
						packageVersion = args.requestedVersion;
					}

					results.Add(new BeamablePackageInProject()
					{
						projectPath = projectPath, packageName = packageName, packageVersion = packageVersion
					});
				}
			}
		}

		var json = JsonConvert.SerializeObject(results);
		AnsiConsole.Write(
			new Panel(new JsonText(json))
				.Header("Projects versions")
				.Collapse()
				.RoundedBorder());
		return BeamablePackageInProject.ToResult(results);
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
