using Beamable.Common.BeamCli.Contracts;
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
			description: "The list of services to include, defaults to all local services")
		{
			AllowMultipleArgumentsPerToken = true,
			Arity = ArgumentArity.ZeroOrMore
		}, binder);
	}

	public static void AddServiceTagsOption<TArgs>(AppCommand<TArgs> command, Action<TArgs, List<string>> bindWithTags, Action<TArgs, List<string>> bindWithoutTags)
		where TArgs : CommandArgs
	{
		var withTagsOption = new Option<List<string>>(
			name: "--with-group",
			description:
			$"A set of {CliConstants.PROP_BEAM_SERVICE_GROUP} tags that will include the associated services"
		)
		{
			AllowMultipleArgumentsPerToken = true, 
			Arity = ArgumentArity.ZeroOrMore
		};
		withTagsOption.AddAlias("--with-groups");
		
		var withoutTagsOption = new Option<List<string>>(
			name: "--without-group",
			description:
			$"A set of {CliConstants.PROP_BEAM_SERVICE_GROUP} tags that will exclude the associated services. Exclusion takes precedence over inclusion."
		)
		{
			AllowMultipleArgumentsPerToken = true, 
			Arity = ArgumentArity.ZeroOrMore
		};
		withoutTagsOption.AddAlias("--without-groups");

		command.AddOption(withoutTagsOption, (args, option) =>
		{
			var tags = GetTags(option);
			bindWithoutTags(args, tags.ToList());
		});
		command.AddOption(withTagsOption, (args, option) =>
		{
			var tags = GetTags(option);
			bindWithTags(args, tags.ToList());
		});

		// take a list of option inputs (like, ["tag1;tag2", "tag3"], and flatten them into a single list of tags, like ["tag1", "tag2", "tag3"]
		static HashSet<string> GetTags(List<string> optionInput)
		{
			var tags = new List<string>();
			foreach (var optionValue in optionInput)
			{
				if (string.IsNullOrEmpty(optionValue)) continue;
				var optionTags = optionValue.Split(CliConstants.SPLIT_OPTIONS,
					StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
				tags.AddRange(optionTags);
			}
			return new HashSet<string>(tags);
		}
	}

	/// <summary>
	/// tag existing services content, merge with tags and without tags, and optionally include storage definitions
	/// </summary>
	/// <param name="args"></param>
	/// <param name="withTags"></param>
	/// <param name="withoutTags"></param>
	/// <param name="includeStorage"></param>
	/// <param name="services"></param>
	/// <exception cref="CliException"></exception>
	public static void FinalizeServicesArg(CommandArgs args, List<string> withTags, List<string> withoutTags, bool includeStorage, ref List<string> services)
	{
		services ??= new List<string>();
		var noExplicitlyListedServices = services.Count == 0;
		var noInclusionTags = withTags == null || withTags.Count == 0;
		
		if (noExplicitlyListedServices && noInclusionTags) // get all services
		{
			services = args.BeamoLocalSystem?.BeamoManifest?.ServiceDefinitions
				.Where(x =>
				{
					var hasLocalProjectFile = !string.IsNullOrEmpty(x.ProjectPath);
					var fitsTypeRequirement = includeStorage || x.Protocol == BeamoProtocolType.HttpMicroservice;
					return hasLocalProjectFile && fitsTypeRequirement;
				})
				.Select(x => x.BeamoId)
				.ToList() ?? new List<string>();
		}
		
		if (withTags != null) // add included groups
		{
			foreach (var group in withTags)
			{
				if (!args.BeamoLocalSystem!.BeamoManifest!.ServiceGroupToBeamoIds.TryGetValue(group, out var ids))
					continue;
				services.AddRange(ids);
			}
		}

		services = services.Distinct().ToList(); // de-dupe services... This is important to do before removal, because the remove operation will only remove the first (and hopefully only) instance of a service id. 
		
		if (withoutTags != null) // remove excluded groups
		{
			foreach (var group in withoutTags)
			{
				if (!args.BeamoLocalSystem!.BeamoManifest!.ServiceGroupToBeamoIds.TryGetValue(group, out var ids))
					continue;
				foreach (var id in ids)
				{
					services.Remove(id);
				}
			}
		}
		
		if (services.Count == 0)
		{
			throw new CliException("No services are listed.");
		}

		Log.Debug("using services " + string.Join(",", services));
	}
	public static void FinalizeServicesArg(CommandArgs args, ref List<string> services)
	{
		FinalizeServicesArg(args, 
			withTags: null, 
			withoutTags: null, 
			includeStorage: false, 
			ref services);
	}

	public static async Task<ProjectBuildStatusReport> IsProjectBuilt(CommandArgs args, string beamoServiceId)
	{

		var service = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.FirstOrDefault(x => x.BeamoId == beamoServiceId);
		if (service == null)
		{
			throw new CliException($"service does not exist, service=[{beamoServiceId}]");
		}

		var deps = args.BeamoLocalSystem.GetDependencies(beamoServiceId);
		Log.Debug("Found service definition, service=[{serviceId}] projectPath=[{ProjectPath}] deps=[{Dependencies}]", beamoServiceId, service.ProjectDirectory, string.Join(",", deps));
		var projectPath = args.ConfigService.BeamableRelativeToExecutionRelative(service.ProjectDirectory);
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
