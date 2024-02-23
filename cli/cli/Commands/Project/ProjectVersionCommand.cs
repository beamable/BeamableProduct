using cli.Services;
using CliWrap;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;
using System.CommandLine;
using System.Text;
using Serilog;

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
		Log.Debug($"discovered {projectList.Count} projects...");
		foreach (var project in projectList)
		{
			var solutionPath =
				Directory.GetParent(Path.Combine(args.ConfigService.BaseDirectory, project.RelativeDockerfilePath));
			Log.Debug($"sln=[{solutionPath}]");

			var (_, buffer) =
				await CliExtensions.RunWithOutput(args.AppContext.DotnetPath, $"sln list {solutionPath!.FullName}");

			var projectsPaths = buffer.ToString().ReplaceLineEndings("\n").Split("\n").Where(s => s.EndsWith(".csproj"))
				.Select(p => Directory.GetParent(Path.Combine(solutionPath.FullName, p))!.FullName).ToList();
			Log.Debug($"sln=[{solutionPath}] inner project reference count=[{projectsPaths.Count}]");

			foreach (string projectPath in projectsPaths)
			{
				Log.Debug($"checking {projectPath}");
				(_, buffer) =
					await CliExtensions.RunWithOutput(args.AppContext.DotnetPath, $"list {projectPath} package");
				var result = buffer.ToString().ReplaceLineEndings("\n").Split("\n").Where(s => s.Contains("> Beamable."))
					.ToList();

				foreach (var line in result)
				{
					var splitedLine = line.Split(" ",
						StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
					var packageIndex = splitedLine.FindIndex(s => s.StartsWith("Beamable."));
					var packageName = splitedLine[packageIndex];
					var packageVersion = splitedLine[packageIndex + 1];

					Log.Debug($"result index=[{packageIndex}] name=[{packageName}] version=[{packageVersion}]");

					if (!string.IsNullOrWhiteSpace(args.requestedVersion) &&
						!args.requestedVersion.Equals(packageVersion))
					{
						(_, buffer) = await CliExtensions.RunWithOutput(args.AppContext.DotnetPath,
								$"add {projectPath} package {packageName} --version \"{args.requestedVersion}\"");
						AnsiConsole.WriteLine(buffer.ToString());
						packageVersion = args.requestedVersion;
					}

					results.Add(new BeamablePackageInProject()
					{
						projectPath = projectPath,
						packageName = packageName,
						packageVersion = packageVersion
					});
				}
			}
		}

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
