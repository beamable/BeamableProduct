using cli.Services;
using CliWrap;
using Serilog;
using System.CommandLine;
using System.Text;

namespace cli.Dotnet;

public class ProjectCommand : CommandGroup
{
	public ProjectCommand() : base(
		"project",
		"Commands that relate to a standalone Beamable project")
	{
	}

	public static void AddWatchOption<TArgs>(AppCommand<TArgs> command, Action<TArgs, bool> binder)
		where TArgs : CommandArgs
	{
		var option = new Option<bool>(
			name: "--watch",
			description: "When true, the command will run forever and watch the state of the program");
		option.AddAlias("-w");
		command.AddOption(option, binder);
	}

	public static void AddIdsOption<TArgs>(AppCommand<TArgs> command, Action<TArgs, List<string>> binder)
		where TArgs : CommandArgs
	{
		command.AddOption(new Option<List<string>>(
			name: "--ids",
			description: "The list of services to build, defaults to all local services")
		{
			AllowMultipleArgumentsPerToken = true,
			Arity = ArgumentArity.ZeroOrMore
		}, binder);
	}

	public static void FinalizeServicesArg(CommandArgs args, ref List<string> services)
	{
		if (services == null || services.Count == 0)
		{
			services = args.BeamoLocalSystem
				.BeamoManifest?
				.HttpMicroserviceLocalProtocols?
				.Select(x => x.Key)
				.ToList() ?? new List<string>();
		}

		if (services.Count == 0)
		{
			throw new CliException("No services are listed.");
		}

		Log.Debug("using services " + string.Join(",", services));
	}

	public static async Task<ProjectBuildStatusReport> IsProjectBuilt(CommandArgs args, string beamoServiceId)
	{
		if (!args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols.TryGetValue(beamoServiceId, out var service))
		{
			throw new CliException($"service does not exist, service=[{beamoServiceId}]");
		}

		var deps = await args.BeamoLocalSystem.GetDependencies(beamoServiceId);
		Log.Debug("Found service definition, ctx=[{ServiceDockerBuildContextPath}] dockerfile=[{ServiceRelativeDockerfilePath}] deps=[{Dependencies}]", service.DockerBuildContextPath, service.RelativeDockerfilePath, string.Join(",", deps));
		var dockerfilePath = Path.Combine(args.ConfigService.GetRelativePath(service.DockerBuildContextPath), service.RelativeDockerfilePath);
		var projectPath = Path.GetDirectoryName(dockerfilePath);
		Log.Debug("service path=[{ProjectPath}]", projectPath);
		var commandStr = $"msbuild {projectPath} -t:GetTargetPath -verbosity:diag";
		Log.Debug("running {AppContextDotnetPath} {CommandStr}", args.AppContext.DotnetPath, commandStr);
		var stdOutBuilder = new StringBuilder();
		var result = await CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, commandStr)
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuilder))
			.WithValidation(CommandResultValidation.None)
			.ExecuteAsync();
		Log.Verbose("dotnet program exited with {ResultExitCode}", result.ExitCode);
		var stdOut = stdOutBuilder.ToString();
		var lines = stdOut.Split(Environment.NewLine);
		Log.Verbose("msbuild logs\\n{StdOut}", stdOut);
		var outputPathLine = lines.Select(l => l.ToLowerInvariant().Trim()).FirstOrDefault(l => l.StartsWith("finaloutputpath") && l.EndsWith(".dll"));

		if (string.IsNullOrEmpty(outputPathLine))
			throw new CliException(
				$"service could not identify output path. service=[{beamoServiceId}] command=[{commandStr}]");

		var report = new ProjectBuildStatusReport { path = outputPathLine.Substring("finaloutputpath = ".Length).Trim(), };
		report.isBuilt = File.Exists(report.path);
		Log.Debug("found output path, path=[{ReportPath}] exists=[{ReportIsBuilt}]", report.path, report.isBuilt);
		return report;
	}

	public struct ProjectBuildStatusReport
	{
		public bool isBuilt;
		public string path;
	}
}
