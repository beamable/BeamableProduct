using cli.Services;
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
		}
		catch (Exception e)
		{
			throw new CliException(
				$"Could not add dependency [{args.DependencyName}] to extension [{args.ExtensionName}]. Message = [{e.Message}] Stacktrace = [{e.StackTrace}]");
		}

		return Task.CompletedTask;
	}
}
