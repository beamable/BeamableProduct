using Beamable.Common;
using Beamable.Server;
using Beamable.Server.Editor;
using Beamable.Server.Generator;
using Beamable.Tooling.Common.OpenAPI;
using cli.Unreal;
using microservice.Common;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.CommandLine;
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

public class GenerateClientFileCommand : AppCommand<GenerateClientFileCommandArgs>
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

	public override Task Handle(GenerateClientFileCommandArgs args)
	{
		#region load client dll into current domain

		var absolutePath = Path.GetFullPath(args.microserviceAssemblyPath);
		var absoluteDir = Path.GetDirectoryName(absolutePath);
		AssemblyLoadContext.Default.Resolving += (context, name) =>
		{
			var assemblyPath = Path.Combine(absoluteDir, $"{name.Name}.dll");
			if (assemblyPath != null)
				return context.LoadFromAssemblyPath(assemblyPath);
			return null;
		};
		var userAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(absolutePath);

		#endregion

		var allTypes = userAssembly.GetExportedTypes();
		foreach (var type in allTypes)
		{
			if (!type.IsSubclassOf(typeof(Microservice))) continue;
			var attribute = type.GetCustomAttribute<MicroserviceAttribute>();
			if (attribute == null) continue;

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
							return generationTask;
					}
				}

				// UNREAL

				if (args.ProjectService.GetLinkedUnrealProjects().Count > 0)
				{
					var gen = new ServiceDocGenerator();
					var oapiDocument = gen.Generate(type, attribute, null);


					foreach (var unrealProjectData in args.ProjectService.GetLinkedUnrealProjects())
					{
						var unrealGenerator = new UnrealSourceGenerator();
						var docs = new List<OpenApiDocument>() { oapiDocument };
						var orderedSchemas = SwaggerService.ExtractAllSchemas(docs,
							GenerateSdkConflictResolutionStrategy.RenameUncommonConflicts);

						var previousGenerationFilePath = Path.Combine(args.ConfigService.BaseDirectory, unrealProjectData.BeamableBackendGenerationPassFile);

						// Set up the generator to generate code with the correct output path for the AutoGen folders.
						UnrealSourceGenerator.exportMacro = unrealProjectData.CoreProjectName.ToUpper() + "_API";
						UnrealSourceGenerator.blueprintExportMacro = unrealProjectData.BlueprintNodesProjectName.ToUpper() + "_API";
						UnrealSourceGenerator.headerFileOutputPath = unrealProjectData.MsCoreHeaderPath + "/";
						UnrealSourceGenerator.cppFileOutputPath = unrealProjectData.MsCoreCppPath + "/";
						UnrealSourceGenerator.blueprintHeaderFileOutputPath = unrealProjectData.MsBlueprintNodesHeaderPath + "/Public/";
						UnrealSourceGenerator.blueprintCppFileOutputPath = unrealProjectData.MsBlueprintNodesCppPath + "/Private/";
						UnrealSourceGenerator.genType = UnrealSourceGenerator.GenerationType.Microservice;
						UnrealSourceGenerator.previousGenerationPassesData = JsonConvert.DeserializeObject<PreviousGenerationPassesData>(File.ReadAllText(previousGenerationFilePath));
						UnrealSourceGenerator.currentGenerationPassDataFilePath = $"{unrealProjectData.CoreProjectName}_GenerationPass";
						var unrealFileDescriptors = unrealGenerator.Generate(new SwaggerService.DefaultGenerationContext { Documents = docs, OrderedSchemas = orderedSchemas });

						var hasOutputPath = !string.IsNullOrEmpty(args.outputDirectory);
						for (int i = 0; i < unrealFileDescriptors.Count; i++)
						{
							string outputPath;
							if (hasOutputPath)
							{
								outputPath = Path.Combine(args.outputDirectory, $"{unrealFileDescriptors[i].FileName}");
							}
							else
							{
								outputPath = Path.Combine(args.ConfigService.BaseDirectory, unrealProjectData.SourceFilesPath, $"{unrealFileDescriptors[i].FileName}");
							}

							Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
							File.WriteAllText(outputPath, unrealFileDescriptors[i].Content);
						}

						return Task.CompletedTask;
					}
				}
			}
		}

		return Task.CompletedTask;
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
