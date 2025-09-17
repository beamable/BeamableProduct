using Beamable;
using Beamable.Common;
using Beamable.Server;
using Beamable.Server.Editor;
using Beamable.Tooling.Common.OpenAPI;
using cli.Dotnet;
using cli.Services;
using CliWrap;
using microservice.Common;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using microservice.Extensions;
using MongoDB.Bson;

namespace cli.Commands.Project;

public class GenerateOApiCommandArgs : CommandArgs
{
	public List<string> services;
}

public class GenerateOApiCommandOutput
{
	public string service;
	public bool isBuilt;
	public string openApi;
}

public class GenerateOApiCommand : StreamCommand<GenerateOApiCommandArgs, GenerateOApiCommandOutput>
{
	public GenerateOApiCommand() : base("oapi", "Generate the Open API specification for the project")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
	}

	public override async Task Handle(GenerateOApiCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, ref args.services);

		foreach (var service in args.services)
		{
			var def = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.FirstOrDefault(d => d.BeamoId == service);

			var outputPath = $"{service}_openApi.json";
			var command = CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath,
					$"run {def.AbsoluteProjectPath} -- --generate-oapi")
				.WithEnvironmentVariables(new Dictionary<string, string>
				{
					["OPEN_API_OUTPUT_PATH"] = outputPath
				});
			await command.ExecuteAsyncAndLog();
			
			SendResults(new GenerateOApiCommandOutput
			{
				isBuilt = true,
				openApi = outputPath,
				service = service
			});
			// var result = ProjectCommand.IsProjectBuiltMsBuild(args, service);
			// if (!result.isBuilt)
			// {
			// 	Log.Information($"service=[{service}] is not built.");
			// 	SendResults(new GenerateOApiCommandOutput
			// 	{
			// 		service = service,
			// 		isBuilt = false
			// 	});
			// 	continue;
			// }
			//
			// var assembly = LoadDotnetMicroserviceAssembly(service, result.path);
			// var microservices = LoadDotnetMicroserviceTypesFromAssembly(assembly);
			// foreach (var (microservice, attribute) in microservices)
			// {
			// 	var doc = generator.Generate(microservice.Type, attribute, new AdminRoutes
			// 	{
			// 		MicroserviceAttribute = attribute,
			// 		MicroserviceType = microservice.Type
			// 	});
			// 	
			// 	var outputString = doc.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
			// 	Log.Information(outputString);
			// 	SendResults(new GenerateOApiCommandOutput
			// 	{
			// 		isBuilt = result.isBuilt,
			// 		service = service,
			// 		openApi = outputString
			// 	});
			//
			// }

		}

	}

	static List<(MicroserviceDescriptor, MicroserviceAttribute)> LoadDotnetMicroserviceTypesFromAssembly(Assembly assembly)
	{
		var allTypes = assembly.GetExportedTypes();
		var output = new List<(MicroserviceDescriptor, MicroserviceAttribute)>();
		foreach (var type in allTypes)
		{
			if (!type.IsSubclassOf(typeof(Microservice))) continue;
			var attribute = type.GetCustomAttribute<MicroserviceAttribute>();
			if (attribute == null) continue;

			var descriptor = new MicroserviceDescriptor
			{
				Name = attribute.MicroserviceName,
				AttributePath = attribute.SourcePath,
				Type = type
			};
			output.Add((descriptor, attribute));
		}

		return output;
	}

	static Assembly LoadDotnetMicroserviceAssembly(string serviceName, string assemblyPath)
	{
		var absolutePath = Path.GetFullPath(assemblyPath);
		var absoluteDir = Path.GetDirectoryName(absolutePath);
		var loadContext = new AssemblyLoadContext($"load-assembly-{serviceName}", false);
		loadContext.Resolving += (context, name) =>
		{
			var assemblyPath = Path.Combine(absoluteDir, $"{name.Name}.dll");
			try
			{
				Log.Verbose($"loading dll name=[{name.Name}] version=[{name.Version}]");
				if (assemblyPath != null)
					return context.LoadFromAssemblyPath(assemblyPath);
				return null;
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

		return loadContext.LoadFromAssemblyPath(absolutePath);
	}

	static async Task<ProjectBuildStatusReport> IsProjectBuilt(CommandArgs args, string serviceName)
	{
		if (!args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols.TryGetValue(serviceName, out var service))
		{
			throw new CliException($"service does not exist, service=[{serviceName}]");
		}
		var canBeBuiltLocally = args.BeamoLocalSystem.VerifyCanBeBuiltLocally(serviceName);
		if (!canBeBuiltLocally)
		{
			return new ProjectBuildStatusReport() { path = null, isBuilt = false };
		}
		Log.Debug($"Found service definition, ctx=[{service.DockerBuildContextPath}] dockerfile=[{service.AbsoluteDockerfilePath}]");

		var dockerfilePath = service.AbsoluteDockerfilePath;
		var projectPath = Path.GetDirectoryName(dockerfilePath);
		Log.Debug($"service path=[{projectPath}]");
		var commandStr = $"msbuild {projectPath.EnquotePath()} -t:GetTargetPath -verbosity:diag";
		Log.Debug($"running {args.AppContext.DotnetPath} {commandStr}");
		var stdOutBuilder = new StringBuilder();
		var result = await CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath, commandStr)
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuilder))
			.WithValidation(CommandResultValidation.None)
			.ExecuteAsync();
		Log.Verbose("dotnet program exited with " + result.ExitCode);
		var stdOut = stdOutBuilder.ToString();
		var lines = stdOut.Split(Environment.NewLine);
		Log.Verbose("msbuild logs\n" + stdOut);
		var outputPathLine = lines.Select(l => l.ToLowerInvariant().Trim()).FirstOrDefault(l => l.StartsWith("finaloutputpath") && l.EndsWith(".dll"));

		if (string.IsNullOrEmpty(outputPathLine))
			throw new CliException(
				$"service could not identify output path. service=[{serviceName}] command=[{commandStr}]");

		var report = new ProjectBuildStatusReport
		{
			path = outputPathLine.Substring("finaloutputpath = ".Length).Trim(),
		};
		report.isBuilt = File.Exists(report.path);

		Log.Debug($"found output path, path=[{report.path}] exists=[{report.isBuilt}]");

		return report;
	}

	public struct ProjectBuildStatusReport
	{
		public bool isBuilt;
		public string path;
	}
}
