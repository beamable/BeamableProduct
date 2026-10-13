using Beamable.Server;
using cli.Services;
using cli.Services.Web;
using Newtonsoft.Json.Linq;
using System.CommandLine;
using static Beamable.Common.Constants.Features.PortalExtension;

namespace cli.Portal;

public class PortalExtensionAddDependencyCommandArgs : CommandArgs
{
	public string ExtensionName;
	public string DependencyName;
}

public class PortalExtensionAddDependencyCommand : AppCommand<PortalExtensionAddDependencyCommandArgs>, IEmptyResult
{
	public override bool IsForInternalUse => true;
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
		var extensions =
			args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.Where(p =>
				p.Protocol == BeamoProtocolType.PortalExtension).Select(s => s.PortalExtensionDefinition);

		var extension = extensions.FirstOrDefault(p => p.Name == args.ExtensionName);

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

			if (extension.MicroserviceDependencies.Contains(microservice.BeamoId))
			{
				throw new CliException($"The microservice: [{microservice.BeamoId}] was already added to extension: [{args.ExtensionName}]");
			}

			extension.MicroserviceDependencies.Add(microservice.BeamoId);

			if (root[EXTENSION_BEAMABLE_PROPERTY_NAME] == null)
			{
				throw new CliException(
					$"Field {EXTENSION_BEAMABLE_PROPERTY_NAME} expected in extension package.json file");
			}

			root[EXTENSION_BEAMABLE_PROPERTY_NAME][EXTENSION_DEPENDENCIES_PROPERTY_NAME] =
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
		var isZoneExtension = IsZoneExtension(extensionPath);
		var clientsOutputDirectory = Path.Combine(extensionPath, "beamable/clients");
		var generators = new List<WebClientCodeGenerator>();
		var skippedDependencies = new List<string>();

		foreach ((string beamId, HttpMicroserviceLocalProtocol localProtocol) in manifest.HttpMicroserviceLocalProtocols)
		{
			if (!dependencies.Contains(beamId))
			{
				continue;
			}

			if (localProtocol.OpenApiDoc == null)
			{
				Log.Warning($"Client generation for {beamId} is being skipped because there is no API doc. Try running {beamId} once to make it available and try again.");
				skippedDependencies.Add(beamId);
				continue;
			}

			// The client binds to the SDK type that routes to the service's own scope, whatever the extension's scope
			// is: a zone extension calls a realm service through assumeRealm's BeamServer (a BeamBase), the same choice
			// `generate-portal-extension-clients` makes. `project run` regenerates through here on every start.
			var service = manifest.ServiceDefinitions.FirstOrDefault(d => d.BeamoId == beamId);
			if (service?.IsZoneScoped == true && !isZoneExtension)
			{
				Log.Warning($"Realm-scoped portal extension at {extensionPath} depends on zone-scoped microservice {beamId}; a realm extension has no zone SDK to call it with.");
			}

			var generator = new WebClientCodeGenerator(localProtocol.OpenApiDoc, "ts", WebClientCodeGenerator.AugmentTypeFor(service));
			generator.GenerateClientCode(clientsOutputDirectory);
			generators.Add(generator);
		}

		WriteClientTypes(extensionPath, generators, skippedDependencies);
	}

	/// <summary>
	/// Writes the extension's <c>beamable/clients/types/index.ts</c> from the clients of all its dependencies. If a
	/// dependency's client could not be generated, its existing client file may still import types from
	/// <c>index.ts</c>, so the file is left as-is rather than rewritten without them.
	/// </summary>
	public static void WriteClientTypes(string extensionPath, List<WebClientCodeGenerator> generators, List<string> skippedDependencies)
	{
		if (skippedDependencies.Count > 0)
		{
			Log.Warning($"Client types for extension at {extensionPath} were not regenerated because these dependencies have no API doc: {string.Join(", ", skippedDependencies)}.");
			return;
		}

		WebClientCodeGenerator.GenerateClientTypes(generators, Path.Combine(extensionPath, "beamable/clients/types"));
	}

	public static List<string> GetDependenciesFromPath(string extensionPath)
	{
		var packagePath = Path.Combine(extensionPath, "package.json");
		string jsonContent = File.ReadAllText(packagePath);

		JObject root = JObject.Parse(jsonContent);

		JToken deps = root[EXTENSION_BEAMABLE_PROPERTY_NAME][EXTENSION_DEPENDENCIES_PROPERTY_NAME];

		if (deps is { Type: JTokenType.Array })
		{
			return deps.ToObject<List<string>>();
		}

		return new List<string>();
	}

	/// <summary>True when the extension's package.json declares <c>"beamable": { "serviceScope": "zone" }</c>.</summary>
	public static bool IsZoneExtension(string extensionPath)
	{
		var root = JObject.Parse(File.ReadAllText(Path.Combine(extensionPath, "package.json")));
		var scope = root[EXTENSION_BEAMABLE_PROPERTY_NAME]?["serviceScope"]?.ToString();
		return string.Equals(scope?.Trim(), "zone", StringComparison.OrdinalIgnoreCase);
	}
}
