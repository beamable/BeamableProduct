using Beamable;
using Beamable.Common;
using Beamable.Server;
using Beamable.Server.Editor;
using Beamable.Server.Generator;
using Beamable.Tooling.Common.OpenAPI;
using cli.Services;
using cli.Unreal;
using cli.Utils;
using Microsoft.Build.Evaluation;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Loader;

namespace cli.Dotnet;

public class GenerateClientFileCommandArgs : CommandArgs
{
	public string microserviceAssemblyPath;
	public string outputDirectory;
	public bool outputToLinkedProjects = true;
	public List<string> beamoIds;
	public List<string> withTags;
	public List<string> excludeTags;

	public List<string> existingFederationIds;
	public List<string> existingFederationTypeNames;
}

public class GenerateClientFileEvent
{
	public string beamoId;
	public string filePath;
}

public class GenerateClientFileCommand 
	: AppCommand<GenerateClientFileCommandArgs>
		, IResultSteam<DefaultStreamResultChannel, GenerateClientFileEvent>
{
	public GenerateClientFileCommand() : base("generate-client", "Generate a C# client file based on a built C# microservice dll directory")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.beamoIds = i);
		ProjectCommand.AddServiceTagsOption(this, (args, i) => args.withTags = i, (args, i) => args.excludeTags = i);
		AddArgument(new Argument<string>("source", "The .dll filepath for the built microservice"), (arg, i) => arg.microserviceAssemblyPath = i);
		AddOption(new Option<string>("--output-dir", "Directory to write the output client at"), (arg, i) => arg.outputDirectory = i);
		AddOption(new Option<bool>("--output-links", () => true, "When true, generate the source client files to all associated projects"), (arg, i) => arg.outputToLinkedProjects = i);

		var existingFedOption = new Option<List<string>>("--existing-fed-ids", "A set of existing federation ids")
		{
			Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true
		};
		AddOption(existingFedOption, (args, i) => { });
		AddOption(
			new Option<List<string>>("--existing-fed-type-names", "A set of existing class names for federations")
			{
				AllowMultipleArgumentsPerToken = true,
				Arity = ArgumentArity.ZeroOrMore
			},
			(args, opts, i) =>
			{
				var ids = opts.ParseResult.GetValueForOption(existingFedOption);
				args.existingFederationIds = ids;
				args.existingFederationTypeNames = i;
			});
	}

	public override async Task Handle(GenerateClientFileCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, args.withTags, args.excludeTags, false, ref args.beamoIds, true);
		
		#region load client dll into current domain

		var sw = new Stopwatch();
		sw.Start();
		await args.BeamoLocalSystem.InitManifest(fetchServerManifest: false);
		Log.Verbose($"generate-client total ms {sw.ElapsedMilliseconds} - got manifest");

		
		// Get the list of all existing microservices
		var allServices = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Where(sd => sd.Protocol is BeamoProtocolType.HttpMicroservice).ToArray();

		// Get the list of dependencies of each of these microservices
		var allDepTasks = allServices.Select(sd => args.BeamoLocalSystem.GetDependencies(sd.BeamoId)).ToList();
		var allDeps = allDepTasks.Select(deps => deps.Select(dep => dep.name));

		// Get the list of all BeamoIds whose DLLs we need to have loaded for a single pass.
		var allServicesToLoadDlls = allServices
			.Where(sd => !string.IsNullOrEmpty(sd.ProjectDirectory)) // must have a valid local project directory.
			.Select(sd => sd.BeamoId).Union(allDeps.SelectMany(d => d)).Distinct().ToArray();

		// Get the list of all assemblies paired with their last edit time.
		Log.Verbose($"generate-client total ms {sw.ElapsedMilliseconds} - starting");

		// Check all the DLLs that currently exist.
		var checkProjBuilt = allServicesToLoadDlls.Select(beamoId =>
		{
			Project project = null;
			if (args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols.TryGetValue(beamoId, out var httpLocal))
				project = httpLocal.Metadata.msbuildProject;
			else if (args.BeamoLocalSystem.BeamoManifest.EmbeddedMongoDbLocalProtocols.TryGetValue(beamoId, out var dbLocal)) project = dbLocal.Metadata.msbuildProject;
			
			var isProjBuilt =  ProjectCommand.IsProjectBuiltMsBuild(project);
			Log.Verbose($"generate-client total ms {sw.ElapsedMilliseconds} - checked {beamoId} built=[{isProjBuilt}]");
			return isProjBuilt;
		});

		// Based on that, load all dlls that can be loaded.
		var allAssemblies = ProjectCommand.LoadProjectDll(checkProjBuilt);
		Log.Verbose($"generate-client total ms {sw.ElapsedMilliseconds} - done type loading");

		#endregion
		
		var allTypes = allAssemblies.SelectMany(asm => asm.GetExportedTypes()).ToArray();
		var allMsTypes = allTypes.Where(t => t.IsSubclassOf(typeof(Microservice)) && t.GetCustomAttribute<MicroserviceAttribute>() != null).ToArray();
		var allSchemaTypes = ServiceDocGenerator.LoadDotnetDeclaredSchemasFromTypes(allTypes, out var missingAttributes).Select(t => t.type).ToArray();
		
		if (missingAttributes.Count > 0)
		{
			var typesWithErr = string.Join(",", missingAttributes.Select(t => $"({t.Name}, {t.Assembly.GetName().Name})"));
			throw new CliException($"Types [{typesWithErr}] should have {nameof(BeamGenerateSchemaAttribute)} as they are used as fields of a type with {nameof(BeamGenerateSchemaAttribute)}.",
				2, true);
		}
		
		foreach (var type in allMsTypes)
		{
			Log.Verbose($"Generating client for type {type.Name} links=[{args.outputToLinkedProjects}]");
			var attribute = type.GetCustomAttribute<MicroserviceAttribute>()!;
			var descriptor = new MicroserviceDescriptor { Name = attribute.MicroserviceName, AttributePath = attribute.SourcePath, Type = type };

			if (!args.beamoIds.Contains(descriptor.Name))
			{
				Log.Debug($"Skipping client for name=[{descriptor.Name}] because it was not given as a project option.");
				continue;
			}
			
			if (args.outputToLinkedProjects)
			{
				// UNITY

				Log.Verbose($"Linked project count {args.ProjectService.GetLinkedUnityProjects().Count}");
				if (args.ProjectService.GetLinkedUnityProjects().Count > 0)
				{
					var existingFeds = new List<ExistingFederation>();
					for (var i = 0; i < args.existingFederationIds.Count; i++)
					{
						existingFeds.Add(new ExistingFederation
						{
							federationId = args.existingFederationIds[i],
							federationIdTypeName = args.existingFederationTypeNames[i]
						});
					}
					var generator = new ClientCodeGenerator(descriptor, existingFeds);
					if (!string.IsNullOrEmpty(args.outputDirectory))
					{
						Directory.CreateDirectory(args.outputDirectory);
						var outputPath = Path.Combine(args.outputDirectory, $"{descriptor.Name}Client.cs");
						generator.GenerateCSharpCode(outputPath);
					}

					foreach (var unityProjectPath in args.ProjectService.GetLinkedUnityProjects())
					{
						var unityPathRoot = Path.Combine(args.ConfigService.BaseDirectory, unityProjectPath);
						var unityAssetPath = Path.Combine(unityPathRoot, "Assets");
						if (!Directory.Exists(unityAssetPath))
						{
							Log.Error($"Could not generate [{descriptor.Name}] client linked unity project because directory doesn't exist [{unityAssetPath}]");
							continue;
						}

						GeneratedFileDescriptor fileDescriptor = new GeneratedFileDescriptor() { Content = generator.GetCSharpCodeString(), FileName = $"{descriptor.Name}Client.cs" };

						Task generationTask = GenerateFile(descriptor, fileDescriptor, args, unityPathRoot);
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
			var schemasInSomeAssembly = new List<Type>(1024);
			var docs = allMsTypes.Select(t =>
			{
				var schemasInSameAssembly = allSchemaTypes.Where(s => s.Assembly.Equals(t.Assembly)).ToArray();
				schemasInSomeAssembly.AddRange(schemasInSameAssembly);
				
				var attribute = t.GetCustomAttribute<MicroserviceAttribute>();
				var gen = new ServiceDocGenerator();
				return gen.Generate(t, attribute, null, true, schemasInSameAssembly);
			}).ToArray();

			// Get all the schemas that are not declared in the same assembly as a microservice
			var schemasInNonMicroserviceAssemblies = allSchemaTypes.Except(schemasInSomeAssembly).GroupBy(s => s.Assembly)
				.ToDictionary(g => g.Key, g => g.ToArray());

			// Make a new OpenApiDocument for each of these assemblies containing just its schemas.
			docs = docs.Concat(schemasInNonMicroserviceAssemblies.Select(kvp =>
			{
				var gen = new ServiceDocGenerator();
				var doc = gen.Generate(kvp.Key, kvp.Value);
				return doc;
			})).ToArray();
			
			// Get the list of schemas
			var orderedSchemas = SwaggerService.ExtractAllSchemas(docs, GenerateSdkConflictResolutionStrategy.RenameUncommonConflicts);

			// For each linked project, we generate the SAMS-Client source code and inject it according to that project's linking configuration 
			foreach (var unrealProjectData in args.ProjectService.GetLinkedUnrealProjects())
			{
				var unrealGenerator = new UnrealSourceGenerator();
				var previousGenerationFilePath = Path.Combine(args.ConfigService.BaseDirectory, unrealProjectData.BeamableBackendGenerationPassFile);

				UnrealSourceGenerator.exportMacro = unrealProjectData.CoreProjectName.ToUpper() + "_API";

				UnrealSourceGenerator.blueprintExportMacro = unrealProjectData.BlueprintNodesProjectName.ToUpper() + "_API";

				UnrealSourceGenerator.blueprintIncludeStatementPrefix = unrealProjectData.MsBlueprintNodesHeaderPath[(unrealProjectData.MsCoreHeaderPath.IndexOf('/') + 1)..];
				UnrealSourceGenerator.blueprintHeaderFileOutputPath = unrealProjectData.MsBlueprintNodesHeaderPath;
				UnrealSourceGenerator.blueprintCppFileOutputPath = unrealProjectData.MsBlueprintNodesCppPath;

				UnrealSourceGenerator.includeStatementPrefix = unrealProjectData.MsCoreHeaderPath[(unrealProjectData.MsCoreHeaderPath.IndexOf('/') + 1)..];
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
						},
						{
							"ClientContentInfoJson", new ReplacementTypeInfo()
							{
								ReferenceId = "ClientContentInfoJson",
								EngineReplacementType = "FBeamRemoteContentManifestEntry",
								EngineOptionalReplacementType = $"{UnrealSourceGenerator.UNREAL_OPTIONAL}BeamRemoteContentManifestEntry",
								EngineImport = @"#include ""BeamBackend/ReplacementTypes/BeamRemoteContentManifestEntry.h""",
							}
						}
					}
				});


				// Generate Microservice Plugin and Modules around the file descriptors 
				{
					// Generate Plugin file descriptor
					{
						unrealFileDescriptors.Add(new()
						{
							FileName = $"{unrealProjectData.CoreProjectName}.uplugin",
							Content = $@"{{
	""FileVersion"": 3,
	""Version"": 1,
	""VersionName"": ""1.0"",
	""FriendlyName"": ""{unrealProjectData.CoreProjectName}"",
	""Description"": """",
	""Category"": ""Other"",
	""CreatedBy"": """",
	""CreatedByURL"": """",
	""DocsURL"": """",
	""MarketplaceURL"": """",
	""CanContainContent"": false,
	""IsBetaVersion"": false,
	""IsExperimentalVersion"": false,
	""Installed"": false,
	""Modules"": [
		{{
			""Name"": ""{unrealProjectData.CoreProjectName}"",
			""Type"": ""Runtime"",
			""LoadingPhase"": ""Default""
		}},
		{{
			""Name"": ""{unrealProjectData.BlueprintNodesProjectName}"",
			""Type"": ""UncookedOnly"",
			""LoadingPhase"": ""Default""
		}}
	],
	""Plugins"": [
		{{
			""Name"": ""BeamableCore"",
			""Enabled"": true
		}}
	]
}}"
						});
					}

					// Generate the ".Build.cs" file for the regular module 
					{
						unrealFileDescriptors.Add(new()
						{
							FileName = $"Source/{unrealProjectData.CoreProjectName}/{unrealProjectData.CoreProjectName}.Build.cs",
							Content = $@"// Copyright Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;

public class {unrealProjectData.CoreProjectName} : ModuleRules
{{
	public {unrealProjectData.CoreProjectName}(ReadOnlyTargetRules Target) : base(Target)
	{{
		PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

		PublicDependencyModuleNames.AddRange(
			new string[]
			{{
				""Core"",
				""BeamableCore"",
				""BeamableCoreRuntime"",

				""Json"",
				""JsonUtilities"",
			}});


		PrivateDependencyModuleNames.AddRange(
			new string[]
			{{
				""CoreUObject"",
				""Engine"",
				""Slate"",
				""SlateCore"",					
			}});
	}}

	public static void AddMicroserviceClients(ModuleRules Rules)
	{{
		Rules.PublicDependencyModuleNames.AddRange(new[] {{ ""{unrealProjectData.CoreProjectName}"" }});
	}}
	
}}"
						});
					}

					// Generate the ".Build.cs" file for the blueprint module 
					{
						unrealFileDescriptors.Add(new()
						{
							FileName = $"Source/{unrealProjectData.BlueprintNodesProjectName}/{unrealProjectData.BlueprintNodesProjectName}.Build.cs",
							Content = $@"// Copyright Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;

public class {unrealProjectData.BlueprintNodesProjectName} : ModuleRules
{{
	public {unrealProjectData.BlueprintNodesProjectName}(ReadOnlyTargetRules Target) : base(Target)
	{{
		PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

		PublicDependencyModuleNames.AddRange(
			new string[]
			{{
				""Core"",
				""{unrealProjectData.CoreProjectName}"",

				""BeamableCore"",
                ""BeamableCoreRuntime"",
                ""BeamableCoreBlueprintNodes"",
                
                ""BlueprintGraph"",
			}});


		PrivateDependencyModuleNames.AddRange(
			new string[]
			{{
				""CoreUObject"",
				""Engine"",
				""Slate"",
				""SlateCore"",					
			}});
	}}

	public static void AddMicroserviceClientsBp(ModuleRules Rules)
	{{
		Rules.PublicDependencyModuleNames.AddRange(new[] {{ ""{unrealProjectData.BlueprintNodesProjectName}"" }});
	}}
	
}}"
						});
					}

					// Generate the IModuleInterface Header and Cpp files for the Regular Module 
					{
						unrealFileDescriptors.Add(new()
						{
							FileName = $"{unrealProjectData.MsCoreHeaderPath}{unrealProjectData.CoreProjectName}.h",
							Content = $@"// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include ""CoreMinimal.h""
#include ""Modules/ModuleManager.h""

class F{unrealProjectData.CoreProjectName}Module : public IModuleInterface
{{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
}};
"
						});

						unrealFileDescriptors.Add(new()
						{
							FileName = $"{unrealProjectData.MsCoreCppPath}{unrealProjectData.CoreProjectName}.cpp",
							Content = $@"// Copyright Epic Games, Inc. All Rights Reserved.

#include ""{unrealProjectData.CoreProjectName}.h""

#define LOCTEXT_NAMESPACE ""F{unrealProjectData.CoreProjectName}Module""

void F{unrealProjectData.CoreProjectName}Module::StartupModule()
{{
	// This code will execute after your module is loaded into memory; the exact timing is specified in the .uplugin file per-module
}}

void F{unrealProjectData.CoreProjectName}Module::ShutdownModule()
{{
	// This function may be called during shutdown to clean up your module.  For modules that support dynamic reloading,
	// we call this function before unloading the module.
}}

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(F{unrealProjectData.CoreProjectName}Module, {unrealProjectData.CoreProjectName})"
						});
					}

					// Generate the IModuleInterface Header and Cpp files for the Blueprint Module 
					{
						unrealFileDescriptors.Add(new()
						{
							FileName = $"{unrealProjectData.MsBlueprintNodesHeaderPath}{unrealProjectData.BlueprintNodesProjectName}.h",
							Content = $@"// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include ""CoreMinimal.h""
#include ""Modules/ModuleManager.h""

class F{unrealProjectData.BlueprintNodesProjectName}Module : public IModuleInterface
{{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
}};
"
						});

						unrealFileDescriptors.Add(new()
						{
							FileName = $"{unrealProjectData.MsBlueprintNodesCppPath}{unrealProjectData.BlueprintNodesProjectName}.cpp",
							Content = $@"// Copyright Epic Games, Inc. All Rights Reserved.

#include ""{unrealProjectData.BlueprintNodesProjectName}.h""

#define LOCTEXT_NAMESPACE ""F{unrealProjectData.BlueprintNodesProjectName}Module""

void F{unrealProjectData.BlueprintNodesProjectName}Module::StartupModule()
{{
	// This code will execute after your module is loaded into memory; the exact timing is specified in the .uplugin file per-module
}}

void F{unrealProjectData.BlueprintNodesProjectName}Module::ShutdownModule()
{{
	// This function may be called during shutdown to clean up your module.  For modules that support dynamic reloading,
	// we call this function before unloading the module.
}}

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(F{unrealProjectData.BlueprintNodesProjectName}Module, {unrealProjectData.BlueprintNodesProjectName})"
						});
					}
				}

				var hasOutputPath = !string.IsNullOrEmpty(args.outputDirectory);
				var outputDir = args.outputDirectory;
				if (!hasOutputPath) outputDir = Path.Combine(args.ConfigService.BaseDirectory, unrealProjectData.SourceFilesPath);

				var allFilesToCreate = unrealFileDescriptors.Select(fd => Path.Join(outputDir, $"{fd.FileName}")).ToList();
				// We need to run the "generate project files if any file was created"
				var needsProjectFilesRebuild = !allFilesToCreate.All(File.Exists);
				// We always clean up the output directory's AutoGen folders  --- every file we create is in the AutoGen folder.
				var outputDirInfo = new DirectoryInfo(outputDir);
				if (outputDirInfo.Exists)
				{
					var autoGenDirs = outputDirInfo.GetDirectories("AutoGen", SearchOption.AllDirectories);
					foreach (DirectoryInfo directoryInfo in autoGenDirs)
					{
						// Because Microsoft does not give us a callback that runs after one or more projects have been recompiled in a solution and instead compiles all projects in-parallel), there is a chance
						// that multiple instances of this command run simultaneously. When that happens, the directory might not be accessible --- in that case, we just have to busy wait until we can actually delete the directory.
						// This guarantees that, when compiling the entire solution, the last instance of this command will do a clean rebuild of the microservice clients (which means it'll use the up-to-date dlls of
						// all the projects in the solution).
						bool successfulDelete;
						do
						{
							try
							{
								Directory.Delete(directoryInfo.ToString(), true);
								successfulDelete = true;
							}
							catch
							{
								successfulDelete = false;
							}
						} while (!successfulDelete);
					}
				}

				var writeFiles = new List<Task>();
				for (int i = 0; i < allFilesToCreate.Count; i++)
				{
					var fileIdx = i;
					string filePath = allFilesToCreate[fileIdx];
					var path = Path.GetDirectoryName(filePath);
					if (path == null) throw new CliException($"Parent path for file {filePath} is null. If you're a customer seeing this, report a bug.");

					// Because Microsoft does not give us a callback that runs after one or more projects have been recompiled in a solution and instead compiles all projects in-parallel), there is a chance
					// that multiple instances of this command run simultaneously. When that happens, the directory might not be accessible --- in that case, we just have to busy wait until we can actually delete the directory.
					// This guarantees that, when compiling the entire solution, the last instance of this command will do a clean rebuild of the microservice clients (which means it'll use the up-to-date dlls of
					// all the projects in the solution).
					bool successfulCreate;
					do
					{
						try
						{
							Directory.CreateDirectory(path);
							successfulCreate = true;
						}
						catch
						{
							successfulCreate = false;
						}
					} while (!successfulCreate);

					writeFiles.Add(Task.Run(() =>
					{
						// Because Microsoft does not give us a callback that runs after one or more projects have been recompiled in a solution and instead compiles all projects in-parallel), there is a chance
						// that multiple instances of this command run simultaneously. When that happens, the directory might not be accessible --- in that case, we just have to busy wait until we can actually delete the directory.
						// This guarantees that, when compiling the entire solution, the last instance of this command will do a clean rebuild of the microservice clients (which means it'll use the up-to-date dlls of
						// all the projects in the solution).
						bool successfulWrite;
						do
						{
							try
							{
								File.WriteAllText(filePath, unrealFileDescriptors[fileIdx].Content);
								successfulWrite = true;
							}
							catch
							{
								successfulWrite = false;
							}
						} while (!successfulWrite);
					}));
				}

				await Task.WhenAll(writeFiles);

				// Run the Regenerate Project Files utility for the project (so that create files are automatically updated in IDEs).
				if (needsProjectFilesRebuild) 
					MachineHelper.RunUnrealGenerateProjectFiles(Path.Combine(args.ConfigService.BaseDirectory, unrealProjectData.Path));
			}
		}
		
		Log.Verbose($"generate-client total ms {sw.ElapsedMilliseconds} - done generating");
	}
	
	Task GenerateFile(MicroserviceDescriptor service, GeneratedFileDescriptor descriptor, GenerateClientFileCommandArgs args, string projectPath)
	{
		/*
		 * By rule of thumb:
		 *  if the service was inside the /BeamableServices folder, then generate it in the standard /Assets/Beamable/Autogen folder
		 *  but if the service was inside the /Packages folder, then generate it in a custom folder, /Packages/XYZ/Beamable/Autogenerated
		 *  and if the service is in neither, then DO NOT write the file out.
		 *   because in this case, it is likely a read-only file system anyway. 
		 */
		
		// TODO: 
		var fullSourcePath = Path.GetFullPath(service.SourcePath);
		var fullProjectPath = Path.GetFullPath(projectPath);
		
		var isChildOfUnityProject = fullSourcePath.StartsWith(fullProjectPath);

		if (isChildOfUnityProject)
		{
			const int trailingDirectorySeparatorCount = 1;
			var relativeSourcePath = fullSourcePath.Substring(fullProjectPath.Length + trailingDirectorySeparatorCount );
			var isPackage = relativeSourcePath.StartsWith("Packages");
			var isPackageCache = relativeSourcePath.StartsWith("Library");
		}
		
		var outputDirectory = Path.Combine(projectPath, "Assets", "Beamable", "Autogenerated", "Microservices");
		Directory.CreateDirectory(outputDirectory);

		Log.Verbose($"Writing File to dir=[{outputDirectory}]");

		var outputPath = Path.Combine(outputDirectory, $"{descriptor.FileName}");
		Log.Verbose($"Writing File to path=[{outputPath}]");

		if (File.Exists(outputPath))
		{
			var existingContent = File.ReadAllText(outputPath);
			if (string.Compare(existingContent, descriptor.Content, CultureInfo.InvariantCulture,
				    CompareOptions.IgnoreSymbols) == 0)
			{
				Log.Verbose("Not writing, because content is the same");
				return Task.CompletedTask;
			}
		}

		File.WriteAllText(outputPath, descriptor.Content);
		this.SendResults(new GenerateClientFileEvent
		{
			beamoId = service.Name,
			filePath = outputPath
		});
		

		// don't need to write anything, because the files are identical.
		return Task.CompletedTask;
	}
}
