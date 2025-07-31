using Beamable.Common.BeamCli.Contracts;
using cli.Services;
using cli.Utils;
using Microsoft.Build.Evaluation;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Reflection;
using System.Runtime.Loader;
using Beamable.Server;
using Project = Microsoft.Build.Evaluation.Project;

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

	private static Option<bool> ExactIdsOption = new Option<bool>(
		name: "--exact-ids",
		description:
		"By default, a blank --ids option maps to ALL available ids. When the --exact-ids flag is given, a blank --ids option maps to NO ids");
	
	public static void AddIdsOption<TArgs>(AppCommand<TArgs> command, Action<TArgs, List<string>> binder)
		where TArgs : CommandArgs
	{
		command.AddOption(new Option<List<string>>(
			name: "--ids",
			description: "The list of services to include, defaults to all local services (separated by whitespace). To use NO services, use the --exact-ids flag") { AllowMultipleArgumentsPerToken = true, Arity = ArgumentArity.ZeroOrMore }, binder);
		command.AddOption(ExactIdsOption);
	}

	public static void AddServiceTagsOption<TArgs>(AppCommand<TArgs> command, Action<TArgs, List<string>> bindWithTags, Action<TArgs, List<string>> bindWithoutTags)
		where TArgs : CommandArgs
	{
		var withTagsOption = new Option<List<string>>(
			name: "--with-group",
			description:
			$"A set of {CliConstants.PROP_BEAM_SERVICE_GROUP} tags that will include the associated services"
		) { AllowMultipleArgumentsPerToken = true, Arity = ArgumentArity.ZeroOrMore };
		withTagsOption.AddAlias("--with-groups");

		var withoutTagsOption = new Option<List<string>>(
			name: "--without-group",
			description:
			$"A set of {CliConstants.PROP_BEAM_SERVICE_GROUP} tags that will exclude the associated services. Exclusion takes precedence over inclusion"
		) { AllowMultipleArgumentsPerToken = true, Arity = ArgumentArity.ZeroOrMore };
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
	public static void FinalizeServicesArg(CommandArgs args, List<string> withTags, List<string> withoutTags, bool includeStorage, ref List<string> services, bool allowEmptyServices=false)
	{
		services ??= new List<string>();

		var ctx = args.Provider.GetService<BindingContext>();
		var exactResult = ctx.ParseResult.GetValueForOption(ExactIdsOption);
		
		var noExplicitlyListedServices = services.Count == 0 && !exactResult;
		var noInclusionTags = withTags == null || withTags.Count == 0;

		if (noExplicitlyListedServices && noInclusionTags) // get all services
		{
			services = args.BeamoLocalSystem?.BeamoManifest?.ServiceDefinitions
				.Where(x =>
				{
					var hasLocalProjectFile = !string.IsNullOrEmpty(x.AbsoluteProjectPath);
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

		if (!allowEmptyServices && services.Count == 0)
		{
			throw new CliException("No services are listed.");
		}

		Log.Debug("using services " + string.Join(",", services));
	}

	public static void FinalizeServicesArg(CommandArgs args, ref List<string> services, bool allowEmptyServices=false)
	{
		FinalizeServicesArg(args,
			withTags: null,
			withoutTags: null,
			includeStorage: false,
			ref services,
			allowEmptyServices);
	}

	public static ProjectBuildStatusReport IsProjectBuiltMsBuild(Project project)
	{
		
		var outDir = project.GetPropertyValue("OutDir");
		outDir = outDir.LocalizeSlashes();
		var fullOutDir = Path.Combine(project.DirectoryPath, outDir);
		var assemblyName = project.GetPropertyValue("AssemblyName");

		var fileName = Path.Combine(fullOutDir, assemblyName + ".dll");
		var beamoId = project.GetPropertyValue("BeamoId");
		beamoId = string.IsNullOrEmpty(beamoId) ? Path.GetFileNameWithoutExtension(project.ProjectFileLocation.File) : beamoId;
		var report = new ProjectBuildStatusReport { beamoId = beamoId, path = fileName, isBuilt = File.Exists(fileName) };

		Log.Verbose($"generating build status report beamoId=[{beamoId}] outDir=[{outDir}] asmName=[{assemblyName}] path=[{fileName}] exists=[{report.isBuilt}]");
		return report;
	}

	public static ProjectBuildStatusReport IsProjectBuiltMsBuild(CommandArgs args, string beamoServiceId)
	{
		var service = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.FirstOrDefault(x => x.BeamoId == beamoServiceId);
		if (service == null)
		{
			throw new CliException($"service does not exist, service=[{beamoServiceId}]");
		}

		var projectPath = service.AbsoluteProjectPath;

		Log.Debug("Found service definition, service=[{serviceId}] projectPath=[{ProjectPath}]", beamoServiceId, projectPath);
		var collection = new ProjectCollection();
		var project = collection.LoadProject(projectPath);
		return IsProjectBuiltMsBuild(project);
	}

	public static (List<Assembly>, List<AssemblyLoadContext>) LoadProjectDll(string sessionId, IEnumerable<ProjectBuildStatusReport> builtProjects, bool skipReferencedAssemblies = false, bool skipAssemblyExpansion = false)
	{
		var allAssemblies = new List<Assembly>();
		var allContexts = new List<AssemblyLoadContext>();
		foreach (ProjectBuildStatusReport isProjBuilt in builtProjects)
		{
			if (!isProjBuilt.isBuilt)
			{
				Log.Warning("Cannot load DLL that is not built");
				continue;
			}

			var beamoId = isProjBuilt.beamoId;
			var dllPath = isProjBuilt.path;
			var absolutePath = Path.GetFullPath(dllPath);
			var absoluteDir = Path.GetDirectoryName(absolutePath)!;
			var loadContext = new AssemblyLoadContext($"generate-client-context-{sessionId}-{beamoId}", true);
			allContexts.Add(loadContext);
			loadContext.Resolving += (context, name) =>
			{
				var assemblyPath = Path.Combine(absoluteDir, $"{name.Name}.dll");
				try
				{
					Log.Verbose("loading dll name=[{Name}] version=[{Version}]", name.Name, name.Version);
					var loadedDependentAsm = context.LoadFromAssemblyPath(assemblyPath);
					if(!skipReferencedAssemblies)
						allAssemblies.Add(loadedDependentAsm);
					
					return loadedDependentAsm;
				}
				catch (Exception ex)
				{
					Log.Error($@"Unable to load dll at path=[{assemblyPath}] 
name=[{name}] 
context=[{context.Name}]
message=[{ex.Message}]
ex-type=[{ex.GetType().Name}]
inner-message=[{ex.InnerException?.Message}]
inner-type=[{ex.InnerException?.GetType().Name}]
");
					throw;
				}
			};

			var userAssembly = loadContext.LoadFromAssemblyPath(absolutePath);
			if(!skipReferencedAssemblies)
			{
				Log.Verbose("loading dll name=[{Name}] version=[{Version}] deps=[{Deps}]", userAssembly.GetName().Name, userAssembly.GetName().Version,
					string.Join(", ", userAssembly.GetReferencedAssemblies().Select(n => n.Name)));

				/// GHOST IN THE MACHINE ---> We need some time to investigate this stuff.
				var requiredAssemblies = userAssembly.GetReferencedAssemblies()
					.Where(asm => !asm.Name.Contains("BeamableMicroserviceBase") && !asm.Name.Contains("Beamable.Server"))
					.ToList();
				foreach (AssemblyName referencedAssembly in requiredAssemblies)
				{
					Assembly loadedAssembly = null;
					try
					{
						loadedAssembly = loadContext.LoadFromAssemblyName(referencedAssembly);
					}
					catch (Exception ex)
					{
						var likelyPath = Path.Combine(Path.GetDirectoryName(userAssembly.Location),
							referencedAssembly.Name + ".dll");
						Log.Debug($"Failed to load assembly=[{referencedAssembly.FullName}] via AssemblyName, so falling back to path=[{likelyPath}]. ex-type=[{ex.GetType().Name}] ex-message=[{ex.Message}]");
						loadedAssembly = loadContext.LoadFromAssemblyPath(likelyPath);

						if (loadedAssembly.FullName != referencedAssembly.FullName)
						{
							Log.Warning($"The assembly=[{referencedAssembly.FullName}] had to load via file-path fallback, but the full-name no longer matches the original assembly reference. Loaded=[{userAssembly.FullName}]");
						}
					}

					allAssemblies.Add(loadedAssembly);

				}
			}
			else
			{
				Log.Verbose("loading dll name=[{Name}] version=[{Version}]", userAssembly.GetName().Name, userAssembly.GetName().Version);
			}

			allAssemblies.Add(userAssembly);
		}

		Log.Verbose("finished loading all dll files.");

		// need to turn the crank loading types until the spigot bleeds dry.
		if(!skipAssemblyExpansion)
		{
			var startCount = allAssemblies.Count;
			var finalCount = 0;
			while (startCount != finalCount)
			{
				var currentAssemblies = allAssemblies.ToList(); // copy working set.
				startCount = currentAssemblies.Count;
				foreach (var currentAssembly in currentAssemblies)
				{
					Log.Verbose($"expanding types from {currentAssembly.FullName}");
					var _ = currentAssembly.GetExportedTypes();
				}

				finalCount = allAssemblies.Count;
			}

			Log.Verbose($"Loaded all types, and found {startCount} assemblies, and after, found {finalCount} assemblies.");
		}

		return (allAssemblies, allContexts);
	}

	public struct ProjectBuildStatusReport
	{
		public string beamoId;
		public bool isBuilt;
		public string path;
	}
}
