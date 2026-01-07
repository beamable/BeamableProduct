using Beamable;
using Beamable.Server;
using Beamable.Server.Editor;
using Beamable.Server.Generator;
using Beamable.Tooling.Common.OpenAPI;
using cli.Services;
using cli.Unreal;
using cli.Utils;
using Microsoft.Build.Evaluation;
using Newtonsoft.Json;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using Beamable.Common;

namespace cli.Dotnet;

public class GenerateClientFileCommandArgs : CommandArgs
{
	public string microserviceAssemblyPath;
	public string outputDirectory;
	public bool outputToLinkedProjects = true;
	public List<string> outputToUnityProjects;
	public List<string> beamoIds;
	public List<string> withTags;
	public List<string> excludeTags;

	public List<string> existingFederationIds;
	public List<string> existingFederationTypeNames;

	public List<string> outputPathHints;
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
	private static readonly Dictionary<OpenApiReferenceId, ReplacementTypeInfo> BaseReplacementTypeInfos = new()
	{
		{
			"ClientPermission", new ReplacementTypeInfo
			{
				ReferenceId = "ClientPermission",
				EngineReplacementType = "FBeamClientPermission",
				EngineOptionalReplacementType =
					$"{UnrealSourceGenerator.UNREAL_OPTIONAL}BeamClientPermission",
				EngineImport = @"#include ""BeamBackend/ReplacementTypes/BeamClientPermission.h""",
			}
		},
		{
			"ExternalIdentity", new ReplacementTypeInfo
			{
				ReferenceId = "ExternalIdentity",
				EngineReplacementType = "FBeamExternalIdentity",
				EngineOptionalReplacementType =
					$"{UnrealSourceGenerator.UNREAL_OPTIONAL}BeamExternalIdentity",
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
				EngineOptionalReplacementType =
					$"{UnrealSourceGenerator.UNREAL_OPTIONAL}BeamRemoteContentManifestEntry",
				EngineImport =
					@"#include ""BeamBackend/ReplacementTypes/BeamRemoteContentManifestEntry.h""",
			}
		}
	};

	public override bool IsForInternalUse => true;

	public GenerateClientFileCommand() : base("generate-client",
		"Obsolete command, please use generate-client-oapi that used the OpenAPI specifications to generate the C# client code. The generate-client command will Generate a C# client file based on a built C# microservice dll directory using refactor")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.beamoIds = i);
		ProjectCommand.AddServiceTagsOption(this, (args, i) => args.withTags = i, (args, i) => args.excludeTags = i);
		AddArgument(new Argument<string>("source", "The .dll filepath for the built microservice"), (arg, i) => arg.microserviceAssemblyPath = i);
		AddOption(new Option<string>("--output-dir", "Directory to write the output client at"), (arg, i) => arg.outputDirectory = i);
		AddOption(new Option<bool>("--output-links", () => true, "When true, generate the source client files to all associated projects"), (arg, i) => arg.outputToLinkedProjects = i);
		AddOption(new Option<List<string>>(
				name: "--output-unity-projects",
				description: "Paths to unity projects to generate clients in", getDefaultValue: () => new List<string>()
			) { AllowMultipleArgumentsPerToken = true, Arity = ArgumentArity.ZeroOrMore },
			(args, i) => args.outputToUnityProjects = i);

		var existingFedOption = new Option<List<string>>("--existing-fed-ids", "A set of existing federation ids") { Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
		AddOption(existingFedOption, (args, i) => { });
		AddOption(
			new Option<List<string>>("--existing-fed-type-names", "A set of existing class names for federations (Obsolete)") { AllowMultipleArgumentsPerToken = true, Arity = ArgumentArity.ZeroOrMore },
			(args, opts, i) =>
			{
				var ids = opts.ParseResult.GetValueForOption(existingFedOption);
				args.existingFederationIds = ids;
				args.existingFederationTypeNames = i;
			});

		AddOption(new Option<List<string>>(
				name: "--output-path-hints",
				description: "A special format, BEAMOID=PATH, that tells the generator where to place the client. The path should be relative to the linked project root (Obsolete)"
			) { AllowMultipleArgumentsPerToken = true, Arity = ArgumentArity.ZeroOrMore },
			(args, i) => args.outputPathHints = i);
	}

	public override async Task Handle(GenerateClientFileCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, args.withTags, args.excludeTags, false, ref args.beamoIds, true);

		var sessionId = Guid.NewGuid().ToString();
		var sw = new Stopwatch();
		sw.Start();
		await args.BeamoLocalSystem.InitManifest(fetchServerManifest: false);
		Log.Verbose($"generate-client total ms {sw.ElapsedMilliseconds} - got manifest");

		var beamoIdToOutputHint = new Dictionary<string, string>();
		{
			// popuate the hint map
			foreach (var hintString in args.outputPathHints)
			{
				var parts = hintString.Split('=',
					StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length != 2)
				{
					throw new CliException($"Invalid hint path=[{hintString}], must be in form BEAMOID=PATH");
				}

				beamoIdToOutputHint[parts[0]] = parts[1];
			}
		}

		// combine all unity targets
		var unityProjectTargets = new HashSet<string>();
		if (args.outputToLinkedProjects) unityProjectTargets.UnionWith(args.ProjectService.GetLinkedUnityProjects());
		unityProjectTargets.UnionWith(args.outputToUnityProjects);
		if (unityProjectTargets.Count > 0)
		{
			// Get the list of all existing microservices
			var allServices = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
				.Where(sd => sd.Protocol is BeamoProtocolType.HttpMicroservice).ToArray();

			// Get the list of dependencies of each of these microservices
			var allDepTasks = allServices.Select(sd => args.BeamoLocalSystem.GetDependencies(sd.BeamoId)).ToList();
			var allDeps = allDepTasks.Select(deps => deps.Select(dep => dep.name));

			// Get the list of all BeamoIds whose DLLs we need to have loaded for a single pass.
			var allServicesToLoadDlls = allServices
				.Where(sd => !string.IsNullOrEmpty(sd.AbsoluteProjectDirectory)) // must have a valid local project directory.
				.Select(sd => sd.BeamoId).Union(allDeps.SelectMany(d => d)).Distinct().ToArray();

			// Get the list of all assemblies paired with their last edit time.
			Log.Verbose($"generate-client total ms {sw.ElapsedMilliseconds} - starting");

			// Check all the DLLs that currently exist.
			var checkProjBuilt = allServicesToLoadDlls.Select(beamoId =>
			{
				Project project = null;
				if (args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols.TryGetValue(beamoId,
					    out var httpLocal))
					project = httpLocal.Metadata.msbuildProject;
				else if (args.BeamoLocalSystem.BeamoManifest.EmbeddedMongoDbLocalProtocols.TryGetValue(beamoId,
					         out var dbLocal)) project = dbLocal.Metadata.msbuildProject;

				var isProjBuilt = ProjectCommand.IsProjectBuiltMsBuild(project);
				Log.Verbose(
					$"generate-client total ms {sw.ElapsedMilliseconds} - checked {beamoId} path=[{isProjBuilt.path}] built=[{isProjBuilt.isBuilt}]");
				return isProjBuilt;
			});
			List<AssemblyLoadContext> allLoadContexts = null;
			try
			{
				// Based on that, load all dlls that can be loaded.
				(var allAssemblies, allLoadContexts) = ProjectCommand.LoadProjectDll(sessionId, checkProjBuilt);
				Log.Verbose($"generate-client total ms {sw.ElapsedMilliseconds} - done type loading");

				var allTypes = allAssemblies.SelectMany(asm => asm.GetExportedTypes()).ToArray();
				var allMsTypes = allTypes.Where(t => t.IsSubclassOf(typeof(Microservice)) && t.GetCustomAttribute<MicroserviceAttribute>() != null).ToArray();
				_ = ServiceDocGenerator.LoadDotnetDeclaredSchemasFromTypes(allTypes, out var missingAttributes).Select(t => t.type).ToArray();

				var possibleFederationIdTypes = allTypes.Where(t => t.IsAssignableTo(typeof(IFederationId))).ToArray();
				var discoveredFederationNameToFullTypeName = new Dictionary<string, string>();
				foreach (var possibleFederationType in possibleFederationIdTypes)
				{
					var federationAttribute = possibleFederationType.GetCustomAttribute<FederationIdAttribute>();
					if (federationAttribute == null) continue;

					discoveredFederationNameToFullTypeName[federationAttribute.FederationId] =
						possibleFederationType.FullName;
				}

				if (missingAttributes.Count > 0)
				{
					var typesWithErr = string.Join(",",
						missingAttributes.Select(t => $"({t.Name}, {t.Assembly.GetName().Name})"));
					throw new CliException(
						$"Types [{typesWithErr}] should have {nameof(BeamGenerateSchemaAttribute)} as they are used as fields of a type with {nameof(BeamGenerateSchemaAttribute)}.",
						2, true);
				}

				foreach (var type in allMsTypes)
				{
					Log.Verbose($"Generating client for type {type.Name} links=[{args.outputToLinkedProjects}]");
					var attribute = type.GetCustomAttribute<MicroserviceAttribute>()!;
					var descriptor = new MicroserviceDescriptor { Name = attribute.MicroserviceName, AttributePath = attribute.SourcePath, Type = type, CustomClientPath = attribute.CustomAutoGeneratedClientPath };

					if (!args.beamoIds.Contains(descriptor.Name))
					{
						Log.Debug(
							$"Skipping client for name=[{descriptor.Name}] because it was not given as a project option.");
						continue;
					}

					// UNITY

					Log.Verbose($"Linked project count {unityProjectTargets.Count}");

					var existingFeds = new List<ExistingFederation>();
					foreach (var (id, typeName) in discoveredFederationNameToFullTypeName)
					{
						existingFeds.Add(new ExistingFederation { federationId = id, federationIdTypeName = typeName });
					}

					for (var i = 0; i < args.existingFederationIds.Count; i++)
					{
						var existing =
							existingFeds.FirstOrDefault(e => e.federationId == args.existingFederationIds[i]);
						if (existing != null)
						{
							// if the federation was discovered via reflection; then allow the arg to at least 
							//  override the discovered setting... 
							existing.federationIdTypeName = args.existingFederationTypeNames[i];
						}
						else
						{
							existingFeds.Add(new ExistingFederation { federationId = args.existingFederationIds[i], federationIdTypeName = args.existingFederationTypeNames[i] });
						}
					}

					var generator = new ClientCodeGenerator(descriptor, existingFeds);
					if (!string.IsNullOrEmpty(args.outputDirectory))
					{
						Directory.CreateDirectory(args.outputDirectory);
						var outputPath = Path.Combine(args.outputDirectory, $"{descriptor.Name}Client.cs");
						generator.GenerateCSharpCode(outputPath);
					}

					foreach (var unityProjectPath in unityProjectTargets)
					{
						var unityPathRoot = Path.Combine(args.ConfigService.BeamableWorkspace, unityProjectPath);
						var unityAssetPath = Path.Combine(unityPathRoot, "Assets");
						if (!Directory.Exists(unityAssetPath))
						{
							Log.Error(
								$"Could not generate [{descriptor.Name}] client linked unity project because directory doesn't exist [{unityAssetPath}]");
							continue;
						}

						beamoIdToOutputHint.TryGetValue(descriptor.Name, out var hintPath);

						GeneratedFileDescriptor fileDescriptor =
							new GeneratedFileDescriptor() { Content = generator.GetCSharpCodeString() };

						Task generationTask =
							GenerateFile(descriptor, fileDescriptor, args, unityPathRoot, hintPath);
						if (generationTask != null)
						{
							await generationTask;
							break;
						}
					}
				}
			}
			finally
			{
				if (allLoadContexts != null)
				{
					foreach (var context in allLoadContexts)
					{
						Log.Verbose($"Unloading context=[{context.Name}]");
						context.Unload();
					}
				}
			}
		}

		// Handle Unreal code-gen
		var hasUnrealLinkedProjects = args.outputToLinkedProjects && args.ProjectService.GetLinkedUnrealProjects().Count > 0;
		const string customReplacementTypesFolderName = "CustomReplacementTypes";
		if (hasUnrealLinkedProjects)
		{
			// Check if the microservice projects are built and that the OAPI exists.
			var nonExistentDocs = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols
				.Select(kvp => kvp)
				.Where(kvp => kvp.Value.OpenApiDoc == null)
				.ToArray();

			// Notify the user that they need to have the OAPI docs there.
			if (nonExistentDocs.Length > 0)
			{
				var missingServicesAndExpectedPaths = string.Join(", ", ($"{nonExistentDocs.Select(kvp => kvp.Key)} ({nonExistentDocs.Select(kvp => kvp.Value.ExpectedOpenApiDocPath)})"));
				var err = $"Missing the generated OAPI for the following Micro Services: {missingServicesAndExpectedPaths}.\n";
				err += "Please do a clean rebuild of the microservice and verify the file is at the expected location. " +
				       "If it is not, please report an issue to Beamable attaching your logs (re-run this command with '--logs v').";

				throw new CliException(err);
			}

			// Get the docs from build microservices
			var docs = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols
				.Select(asd => asd.Value.OpenApiDoc)
				.ToArray();

			// Get the list of schemas
			var orderedSchemas = SwaggerService.ExtractAllSchemas(docs, GenerateSdkConflictResolutionStrategy.RenameUncommonConflicts);

			// For each linked project, we generate the SAMS-Client source code and inject it according to that project's linking configuration 
			foreach (var unrealProjectData in args.ProjectService.GetLinkedUnrealProjects())
			{
				Log.Verbose($"generating client for project {unrealProjectData.CoreProjectName} path=[{unrealProjectData.Path}], total ms {sw.ElapsedMilliseconds}");

				var unrealGenerator = new UnrealSourceGenerator();
				var previousGenerationFilePath = Path.Combine(args.ConfigService.BeamableWorkspace, unrealProjectData.BeamableBackendGenerationPassFile);

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


				var replacementTypes = new Dictionary<OpenApiReferenceId, ReplacementTypeInfo>(BaseReplacementTypeInfos);
				
				foreach (ReplacementTypeInfo replacementTypeInfo in unrealProjectData.ReplacementTypeInfos)
				{
					replacementTypes.TryAdd(replacementTypeInfo.ReferenceId, replacementTypeInfo);
				}
				
				var unrealFileDescriptors = unrealGenerator.Generate(new SwaggerService.DefaultGenerationContext
				{
					Documents = docs,
					OrderedSchemas = orderedSchemas,
					ReplacementTypes = replacementTypes,
				});

				Log.Verbose($"completed in-memory generation of clients for project {unrealProjectData.CoreProjectName} path=[{unrealProjectData.Path}], total ms {sw.ElapsedMilliseconds}");


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
		}},
		{{
			""Name"": ""{customReplacementTypesFolderName}"",
			""Type"": ""Runtime"",
			""LoadingPhase"": ""Default""
		}},
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
							FileName =
								$"Source/{unrealProjectData.CoreProjectName}/{unrealProjectData.CoreProjectName}.Build.cs",
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
							FileName =
								$"Source/{unrealProjectData.BlueprintNodesProjectName}/{unrealProjectData.BlueprintNodesProjectName}.Build.cs",
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
							FileName =
								$"{unrealProjectData.MsBlueprintNodesHeaderPath}{unrealProjectData.BlueprintNodesProjectName}.h",
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
							FileName =
								$"{unrealProjectData.MsBlueprintNodesCppPath}{unrealProjectData.BlueprintNodesProjectName}.cpp",
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

				Log.Verbose($"completed in-memory generation of client plugin for project {unrealProjectData.CoreProjectName} path=[{unrealProjectData.Path}], total ms {sw.ElapsedMilliseconds}");

				var hasOutputPath = !string.IsNullOrEmpty(args.outputDirectory);
				var outputDir = args.outputDirectory;
				if (!hasOutputPath)
					outputDir = Path.Combine(args.ConfigService.BeamableWorkspace, unrealProjectData.SourceFilesPath);

				var allFilesToCreate = unrealFileDescriptors.Select(fd => Path.Join(outputDir, $"{fd.FileName}")).ToList();

				// We need to run the "generate project files if any file was created"
				var needsProjectFilesRebuild = !allFilesToCreate.All(File.Exists);
				Log.Verbose(
					$"decided to 're-generate project files' for project {unrealProjectData.CoreProjectName} path=[{unrealProjectData.Path}] will-regen=[{needsProjectFilesRebuild}], total ms {sw.ElapsedMilliseconds}");

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

						Log.Verbose($"cleaning existing auto-gen directory {unrealProjectData.CoreProjectName} path=[{unrealProjectData.Path}] dir=[{directoryInfo}], total ms {sw.ElapsedMilliseconds}");
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

						Log.Verbose($"writing file to auto-gen directory {unrealProjectData.CoreProjectName} path=[{unrealProjectData.Path}] dir=[{filePath}], total ms {sw.ElapsedMilliseconds}");
					}));
				}

				await Task.WhenAll(writeFiles);

				var replacementTypeFolder = new DirectoryInfo(Path.Join(outputDir, "Source", customReplacementTypesFolderName));
				if (!replacementTypeFolder.Exists)
				{
					Directory.CreateDirectory(replacementTypeFolder.FullName);
				}
				
				Log.Verbose($"completed writing auto-generated files to disk {unrealProjectData.CoreProjectName} path=[{unrealProjectData.Path}], total ms {sw.ElapsedMilliseconds}");

				// Run the Regenerate Project Files utility for the project (so that create files are automatically updated in IDEs).
				if (needsProjectFilesRebuild)
				{
					Log.Verbose($"regenerating project files for UE {unrealProjectData.CoreProjectName} path=[{unrealProjectData.Path}], total ms {sw.ElapsedMilliseconds}");
					MachineHelper.RunUnrealGenerateProjectFiles(Path.Combine(args.ConfigService.BeamableWorkspace, unrealProjectData.Path));
					Log.Verbose($"completed regeneration of project files for UE {unrealProjectData.CoreProjectName} path=[{unrealProjectData.Path}], total ms {sw.ElapsedMilliseconds}");
				}
			}
		}

		Log.Verbose($"generate-client total ms {sw.ElapsedMilliseconds} - done generating");
	}

	Task GenerateFile(MicroserviceDescriptor service,
		GeneratedFileDescriptor descriptor,
		GenerateClientFileCommandArgs args,
		string projectPath,
		string hintPath)
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
			var relativeSourcePath = fullSourcePath.Substring(fullProjectPath.Length + trailingDirectorySeparatorCount);
			var isPackage = relativeSourcePath.StartsWith("Packages");
			var isPackageCache = relativeSourcePath.StartsWith("Library");
		}

		if (string.IsNullOrEmpty(hintPath))
		{
			hintPath = service.CustomClientPath;
		}

		if (string.IsNullOrEmpty(hintPath))
		{
			hintPath = Path.Combine("Assets", "Beamable", "Autogenerated", "Microservices", $"{service.Name}Client.cs");
		}

		var outputPath = Path.Combine(projectPath, hintPath);
		var outputDirectory = Path.GetDirectoryName(outputPath);

		Directory.CreateDirectory(outputDirectory);

		Log.Verbose($"Writing File to dir=[{outputDirectory}]");

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
		this.SendResults(new GenerateClientFileEvent { beamoId = service.Name, filePath = outputPath });


		// don't need to write anything, because the files are identical.
		return Task.CompletedTask;
	}
}
