using Beamable.Common;
using Beamable.Server;
using Beamable.Server.Editor;
using Beamable.Server.Generator;
using Beamable.Tooling.Common.OpenAPI;
using cli.Services;
using cli.Unreal;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;

namespace cli.Dotnet;

public class GenerateClientFileCommandArgs : CommandArgs
{
	public string microserviceAssemblyPath;
	public string outputDirectory;
	public bool outputToLinkedProjects = true;
}

public class GenerateClientFileCommand : AppCommand<GenerateClientFileCommandArgs>, IEmptyResult
{
	public GenerateClientFileCommand() : base("generate-client", "Generate a C# client file based on a built C# microservice dll directory")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("source", "The .dll filepath for the built microservice"), (arg, i) => arg.microserviceAssemblyPath = i);
		AddOption(new Option<string>("--output-dir", "Directory to write the output client at"), (arg, i) => arg.outputDirectory = i);
		AddOption(new Option<bool>("--output-links", () => true, "When true, generate the source client files to all associated projects"), (arg, i) => arg.outputToLinkedProjects = i);
	}

	public override async Task Handle(GenerateClientFileCommandArgs args)
	{
		#region load client dll into current domain

		// Get the list of all existing microservices
		var allServices = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Where(sd => sd.Protocol is BeamoProtocolType.HttpMicroservice).ToArray();

		// Get the list of dependencies of each of these microservices
		Task<List<DependencyData>>[] allDepTasks = allServices.Select(sd => args.BeamoLocalSystem.GetDependencies(sd.BeamoId)).ToArray();
		var allDepsData = await Task.WhenAll(allDepTasks);
		var allDeps = allDepsData.Select(deps => deps.Select(dep => dep.name));

		// Get the list of all BeamoIds whose DLLs we need to have loaded for a single pass.
		var allServicesToLoadDlls = allServices
			.Where(sd => !string.IsNullOrEmpty(sd.ProjectDirectory)) // must have a valid local project directory.
			.Select(sd => sd.BeamoId).Union(allDeps.SelectMany(d => d)).Distinct().ToArray();

		// Get the list of all assemblies paired with their last edit time.
		var allAssemblies = new List<Assembly>();
		foreach (string beamoId in allServicesToLoadDlls)
		{
			var isProjBuilt = await ProjectCommand.IsProjectBuilt(args, beamoId);
			if (isProjBuilt.isBuilt)
			{
				var dllPath = isProjBuilt.path;
				var absolutePath = Path.GetFullPath(dllPath);
				var absoluteDir = Path.GetDirectoryName(absolutePath)!;
				var loadContext = new AssemblyLoadContext($"generate-client-context-{beamoId}", false);
				loadContext.Resolving += (context, name) =>
				{
					var assemblyPath = Path.Combine(absoluteDir, $"{name.Name}.dll");
					try
					{
						Log.Verbose("loading dll name=[{Name}] version=[{Version}]", name.Name, name.Version);
						var loadedDependentAsm = context.LoadFromAssemblyPath(assemblyPath);
						allAssemblies.Add(loadedDependentAsm);
						return loadedDependentAsm;
					}
					catch (Exception ex)
					{
						BeamableLogger.LogError($@"Unable to load dll at path=[{assemblyPath}] 
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
				Log.Verbose("loading dll name=[{Name}] version=[{Version}] deps=[{Deps}]", userAssembly.GetName().Name, userAssembly.GetName().Version, string.Join(", ", userAssembly.GetReferencedAssemblies().Select(n => n.Name)));

				/// GHOST IN THE MACHINE ---> We need some time to investigate this stuff.
				var requiredAssemblies = userAssembly.GetReferencedAssemblies()
					.Where(asm => !asm.Name.Contains("BeamableMicroserviceBase"))
					.ToList();
				foreach (AssemblyName referencedAssembly in requiredAssemblies)
					allAssemblies.Add(loadContext.LoadFromAssemblyName(referencedAssembly));

				allAssemblies.Add(userAssembly);
			}
		}
		Log.Verbose("finished loading all dll files.");

		#endregion
		
		// need to turn the crank loading types until the spigot bleeds dry.
		var startCount = allAssemblies.Count;
		var finalCount = 0;
		while (startCount != finalCount)
		{
			var currentAssemblies = allAssemblies.ToList(); // copy working set.
			startCount = currentAssemblies.Count;
			var _ = currentAssemblies.SelectMany(asm => asm.GetExportedTypes()).ToArray();
			finalCount = allAssemblies.Count;
			Log.Verbose($"Loaded all types, and found {startCount} assemblies, and after, found {finalCount} assemblies.");
		}

		var allTypes = allAssemblies.SelectMany(asm => asm.GetExportedTypes()).ToArray();
		var allMsTypes = allTypes.Where(t => t.IsSubclassOf(typeof(Microservice)) && t.GetCustomAttribute<MicroserviceAttribute>() != null).ToArray();
		foreach (var type in allMsTypes)
		{
			var attribute = type.GetCustomAttribute<MicroserviceAttribute>()!;
			var descriptor = new MicroserviceDescriptor { Name = attribute.MicroserviceName, AttributePath = attribute.SourcePath, Type = type };

			if (args.outputToLinkedProjects)
			{
				// UNITY

				if (args.ProjectService.GetLinkedUnityProjects().Count > 0)
				{
					var generator = new ClientCodeGenerator(descriptor);
					if (!string.IsNullOrEmpty(args.outputDirectory))
					{
						Directory.CreateDirectory(args.outputDirectory);
						var outputPath = Path.Combine(args.outputDirectory, $"{descriptor.Name}Client.cs");
						generator.GenerateCSharpCode(outputPath);
					}

					foreach (var unityProjectPath in args.ProjectService.GetLinkedUnityProjects())
					{
						var unityAssetPath = Path.Combine(args.ConfigService.BaseDirectory, unityProjectPath, "Assets");

						if (!Directory.Exists(unityAssetPath))
						{
							BeamableLogger.LogError($"Could not generate [{descriptor.Name}] client linked unity project because directory doesn't exist [{unityAssetPath}]");
							continue;
						}

						GeneratedFileDescriptor fileDescriptor = new GeneratedFileDescriptor() { Content = generator.GetCSharpCodeString(), FileName = $"{descriptor.Name}Client.cs" };

						Task generationTask = GenerateFile(new List<GeneratedFileDescriptor>() { fileDescriptor }, args, unityAssetPath);
						if (generationTask != null)
						{
							await generationTask;
							break;
						}
					}
				}
			}
		}

		// Handle Unreal code-gen
		if (args.outputToLinkedProjects && args.ProjectService.GetLinkedUnrealProjects().Count > 0)
		{
			// Get the list of all microservice docs
			var docs = allMsTypes.Select(t =>
			{
				var attribute = t.GetCustomAttribute<MicroserviceAttribute>();
				var gen = new ServiceDocGenerator();
				return gen.Generate(t, attribute, null, true);
			}).ToArray();

			// Get the list of schemas
			var orderedSchemas = SwaggerService.ExtractAllSchemas(docs, GenerateSdkConflictResolutionStrategy.RenameUncommonConflicts);

			// For each linked project, we generate the SAMS-Client source code and inject it according to that project's linking configuration 
			foreach (var unrealProjectData in args.ProjectService.GetLinkedUnrealProjects())
			{
				var unrealGenerator = new UnrealSourceGenerator();
				var previousGenerationFilePath = Path.Combine(args.ConfigService.BaseDirectory, unrealProjectData.BeamableBackendGenerationPassFile);

				// We expect to find the OSS at the Plugin directory.
				var expectedOssPath = Path.Combine(args.ConfigService.BaseDirectory, unrealProjectData.Path, "Plugins/OnlineSubsystemBeamable");
				var isOSSInLinkedProject = Directory.Exists(expectedOssPath);

				// Set up the generator to generate code with the correct output path for the AutoGen folders.
				if (isOSSInLinkedProject)
				{
					if (unrealProjectData.CoreProjectName != "OnlineSubsystemBeamable" || !unrealProjectData.SourceFilesPath.StartsWith("Plugins/OnlineSubsystemBeamable"))
					{
						throw new CliException("You've added the OnlineSubsystemBeamable (OSB) plugin after you had already linked your Unreal Project." +
											   $"Please delete your '{Constants.CONFIG_DIR}/{Constants.CONFIG_LINKED_PROJECTS}' file and re-run 'beam project add-unreal-project'." +
											   "When using the OSB plugin, MS files are generated into the plugin's Customer folder; so, please delete your AutoGen folders from their " +
											   "previous location (outside of the plugin).");
					}
				}

				UnrealSourceGenerator.exportMacro = unrealProjectData.CoreProjectName.ToUpper() + "_API";

				UnrealSourceGenerator.blueprintExportMacro = unrealProjectData.BlueprintNodesProjectName.ToUpper() + "_API";
				UnrealSourceGenerator.blueprintHeaderFileOutputPath = unrealProjectData.MsBlueprintNodesHeaderPath;
				UnrealSourceGenerator.blueprintCppFileOutputPath = unrealProjectData.MsBlueprintNodesCppPath;

				UnrealSourceGenerator.headerFileOutputPath = unrealProjectData.MsCoreHeaderPath;
				UnrealSourceGenerator.cppFileOutputPath = unrealProjectData.MsCoreCppPath;

				UnrealSourceGenerator.genType = UnrealSourceGenerator.GenerationType.Microservice;
				UnrealSourceGenerator.previousGenerationPassesData = JsonConvert.DeserializeObject<PreviousGenerationPassesData>(File.ReadAllText(previousGenerationFilePath));
				UnrealSourceGenerator.currentGenerationPassDataFilePath = $"{unrealProjectData.CoreProjectName}_GenerationPass";

				var unrealFileDescriptors = unrealGenerator.Generate(new SwaggerService.DefaultGenerationContext
				{
					Documents = docs,
					OrderedSchemas = orderedSchemas,
					ReplacementTypes = new Dictionary<OpenApiReferenceId, ReplacementTypeInfo>
					{
						{
							"ClientPermission", new ReplacementTypeInfo
							{
								ReferenceId = "ClientPermission",
								EngineReplacementType = "FBeamClientPermission",
								EngineOptionalReplacementType = $"{UnrealSourceGenerator.UNREAL_OPTIONAL}BeamClientPermission",
								EngineImport = @"#include ""BeamBackend/ReplacementTypes/BeamClientPermission.h""",
							}
						},
						{
							"ExternalIdentity", new ReplacementTypeInfo
							{
								ReferenceId = "ExternalIdentity",
								EngineReplacementType = "FBeamExternalIdentity",
								EngineOptionalReplacementType = $"{UnrealSourceGenerator.UNREAL_OPTIONAL}BeamExternalIdentity",
								EngineImport = @"#include ""BeamBackend/ReplacementTypes/BeamExternalIdentity.h""",
							}
						},
						{
							"Tag", new ReplacementTypeInfo
							{
								ReferenceId = "Tag",
								EngineReplacementType = "FBeamTag",
								EngineOptionalReplacementType = $"{UnrealSourceGenerator.UNREAL_OPTIONAL}BeamTag",
								EngineImport = @"#include ""BeamBackend/ReplacementTypes/BeamTag.h""",
							}
						}
					}
				});

				var hasOutputPath = !string.IsNullOrEmpty(args.outputDirectory);
				var outputDir = args.outputDirectory;
				if (!hasOutputPath) outputDir = Path.Combine(args.ConfigService.BaseDirectory, unrealProjectData.SourceFilesPath);

				var allFilesToCreate = unrealFileDescriptors.Select(fd => Path.Join(outputDir, $"{fd.FileName}")).ToList();
				// We need to run the "generate project files if any file was created"
				var needsProjectFilesRebuild = !allFilesToCreate.All(File.Exists);
				// We always clean up the output directory's AutoGen folders  --- every file we create is in the AutoGen folder.
				var outputDirInfo = new DirectoryInfo(outputDir);
				var autoGenDirs = outputDirInfo.GetDirectories("AutoGen", SearchOption.AllDirectories);
				foreach (DirectoryInfo directoryInfo in autoGenDirs) Directory.Delete(directoryInfo.ToString(), true);

				var writeFiles = new List<Task>();
				for (int i = 0; i < allFilesToCreate.Count; i++)
				{
					string filePath = allFilesToCreate[i];
					var path = Path.GetDirectoryName(filePath);
					if (path == null) throw new CliException($"Parent path for file {filePath} is null. If you're a customer seeing this, report a bug.");

					Directory.CreateDirectory(path);
					writeFiles.Add(File.WriteAllTextAsync(filePath, unrealFileDescriptors[i].Content));
				}

				await Task.WhenAll(writeFiles);

				// Run the Regenerate Project Files utility for the project (so that create files are automatically updated in IDEs).
				if (needsProjectFilesRebuild)
					RunGenerateProjectFiles(Path.Combine(args.ConfigService.BaseDirectory, unrealProjectData.Path));

				static void RunGenerateProjectFiles(string unrealRoot)
				{
					// go into the unreal root
					var cmd = $"cd {unrealRoot};";
					// Get the name of the uproject
					cmd += @"$uproject = Get-ChildItem ""*.uproject"" -Name;";
					// Get the path to the UnrealEngine version for this project (this is stored here as-per UE -- https://forums.unrealengine.com/t/generate-vs-project-files-by-command-line/277707/18).
					cmd += @"$bin = & { (Get-ItemProperty 'Registry::HKEY_CLASSES_ROOT\Unreal.ProjectFile\shell\rungenproj' -Name 'Icon' ).'Icon' };";
					// Build the actual command to run at this path and pipe it into the cmd.exe.
					cmd += @"$bin + ' -projectfiles %cd%\' + $uproject | cmd.exe";

					// Run the command and print the result
					var _ = ExecutePowershellCommand(cmd);
				}


				static string ExecutePowershellCommand(string command)
				{
					var processStartInfo = new ProcessStartInfo();
					processStartInfo.FileName = "powershell.exe";
					processStartInfo.Arguments = $"-Command \"{command}\"";
					processStartInfo.UseShellExecute = false;
					processStartInfo.RedirectStandardOutput = true;

					using var process = new Process();
					process.StartInfo = processStartInfo;
					process.Start();
					string output = process.StandardOutput.ReadToEnd();
					return output;
				}
			}
		}
	}

	Task GenerateFile(List<GeneratedFileDescriptor> descriptors, GenerateClientFileCommandArgs args, string projectPath)
	{
		var outputDirectory = Path.Combine(projectPath, "Beamable", "Autogenerated", "Microservices");
		Directory.CreateDirectory(outputDirectory);

		int identicalFileCounter = 0;

		for (int i = 0; i < descriptors.Count; i++)
		{
			var outputPath = Path.Combine(outputDirectory, $"{descriptors[i].FileName}");

			if (File.Exists(outputPath))
			{
				var existingContent = File.ReadAllText(outputPath);
				if (string.Compare(existingContent, descriptors[i].Content, CultureInfo.InvariantCulture,
						CompareOptions.IgnoreSymbols) == 0)
				{
					identicalFileCounter++;
					continue;
				}
			}

			File.WriteAllText(outputPath, descriptors[i].Content);
		}

		// don't need to write anything, because the files are identical.

		if (identicalFileCounter == descriptors.Count)
			return Task.CompletedTask;

		return null;
	}
}
