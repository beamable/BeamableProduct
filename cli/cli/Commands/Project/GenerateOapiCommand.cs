using Beamable.Common;
using Beamable.Server;
using Beamable.Server.Editor;
using Beamable.Tooling.Common.OpenAPI;
using cli.Dotnet;
using CliWrap.Buffered;
using microservice.Common;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Serilog;
using System.Reflection;
using System.Runtime.Loader;

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

		var generator = new ServiceDocGenerator();
		foreach (var service in args.services)
		{
			var result = await ProjectCommand.IsProjectBuilt(args, service);
			if (!result.isBuilt)
			{
				Log.Information($"service=[{service}] is not built.");
				SendResults(new GenerateOApiCommandOutput
				{
					service = service,
					isBuilt = false
				});
				continue;
			}

			var assembly = LoadDotnetMicroserviceAssembly(service, result.path);
			var microservices = LoadDotnetMicroserviceTypesFromAssembly(assembly);

			foreach (var (microservice, attribute) in microservices)
			{

				var doc = generator.Generate(microservice.Type, attribute, new AdminRoutes
				{
					MicroserviceAttribute = attribute,
					MicroserviceType = microservice.Type
				});
				var outputString = doc.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);

				Log.Information(outputString);
				SendResults(new GenerateOApiCommandOutput
				{
					isBuilt = result.isBuilt,
					service = service,
					openApi = outputString
				});

			}

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
}
