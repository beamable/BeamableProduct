using cli.Services;
using cli.Services.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.CommandLine;

namespace cli.Portal;

public class PortalExtensionAddDependencyCommandArgs : CommandArgs
{
	public string ExtensionName;
	public string DependencyName;
}

public class PortalExtensionAddDependencyCommand : AppCommand<PortalExtensionAddDependencyCommandArgs>, IEmptyResult
{

	public PortalExtensionAddDependencyCommand() : base("add-microservice", "Adds microservice as a dependency for the specified Portal Extension")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("extension", description: "The Portal Extension name that the microservice will be added to"),
			(args, i) => args.ExtensionName = i);
		AddArgument(new Argument<string>("microservice", description: "The Microservice that will be a new dependency of the specified Portal Extension"),
			(args, i) => args.DependencyName = i);
	}

	public override Task Handle(PortalExtensionAddDependencyCommandArgs args)
	{
		var extension =
			args.BeamoLocalSystem.BeamoManifest.PortalExtensionDefinitions.FirstOrDefault(p => p.Name == args.ExtensionName);

		if (extension == null)
		{
			throw new CliException($"Couldn't find a Portal Extension service with the name: [{args.ExtensionName}]");
		}

		var microservice =
			args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.FirstOrDefault(s =>
				s.BeamoId == args.DependencyName);

		if (microservice == null)
		{
			throw new CliException($"Couldn't find a Microservice with the name: [{args.DependencyName}]");
		}

		try
		{
			var packagePath = Path.Combine(extension.AbsolutePath, "package.json");
			string jsonContent = File.ReadAllText(packagePath);

			JObject root = JObject.Parse(jsonContent);

			extension.MicroserviceDependencies.Add(microservice.BeamoId);

			root[Beamable.Common.Constants.Features.PortalExtension.EXTENSION_DEPENDENCIES_PROPERTY_NAME] =
				JToken.FromObject(extension.MicroserviceDependencies);

			File.WriteAllText(packagePath, root.ToString(Newtonsoft.Json.Formatting.Indented));

			//now generate the actual client code
			GenerateDependenciesClients(extension.AbsolutePath, args.BeamoLocalSystem.BeamoManifest);

		}
		catch (Exception e)
		{
			throw new CliException(
				$"Could not add dependency [{args.DependencyName}] to extension [{args.ExtensionName}]. Message = [{e.Message}] Stacktrace = [{e.StackTrace}]");
		}

		return Task.CompletedTask;
	}

	public static void GenerateDependenciesClients(string extensionPath, BeamoLocalManifest manifest)
	{ //TODO: could also use better error handling here
		var dependencies = GetDependenciesFromPath(extensionPath);

		foreach ((string beamId, HttpMicroserviceLocalProtocol localProtocol) in manifest.HttpMicroserviceLocalProtocols)
		{
			if (!dependencies.Contains(beamId) || localProtocol.OpenApiDoc == null)
			{
				continue;
			}

			var generator = new WebClientCodeGenerator(localProtocol.OpenApiDoc, "js");
			var clientsOutputDirectory = Path.Combine(extensionPath, "beamable/clients");
			generator.GenerateClientCode(clientsOutputDirectory);
		}
	}

	public static List<string> GetDependenciesFromPath(string extensionPath)
	{
		var packagePath = Path.Combine(extensionPath, "package.json");
		string jsonContent = File.ReadAllText(packagePath);

		JObject root = JObject.Parse(jsonContent);

		JToken deps = root[Beamable.Common.Constants.Features.PortalExtension.EXTENSION_DEPENDENCIES_PROPERTY_NAME];

		if (deps is { Type: JTokenType.Array })
		{
			// Convert the JArray directly to a List<string>
			return deps.ToObject<List<string>>();
		}

		return new List<string>();
	}
}
