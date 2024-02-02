using Beamable.Common;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;
using System.CommandLine.Binding;

namespace cli;

public class ServicesSetManifestCommandArgs : CommandArgs
{
	public List<string> localHttpNames;
	public List<string> localHttpBuildContextPaths;
	public List<string> localHttpRelativeDockerfilePaths;
	public List<string> storageDependenciesPaths;
	public List<string> shouldServiceBeEnabled;
}


public class ServicesSetManifestCommand : AppCommand<ServicesSetManifestCommandArgs>, IEmptyResult
{
	public ServicesSetManifestCommand() : base("set-local-manifest",
		"Set the entire state of the local manifest, overwriting any existing entries")
	{
	}

	public override void Configure()
	{
		var names = new Option<List<string>>("--local-http-names", "Local http service names")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(names, (x, i) => x.localHttpNames = i);


		var contextPaths = new Option<List<string>>("--local-http-contexts", "Local http service docker build contexts")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(contextPaths, (x, i) => x.localHttpBuildContextPaths = i);


		var dockerFiles = new Option<List<string>>("--local-http-docker-files", "Local http service relative docker file paths")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(dockerFiles, (x, i) => x.localHttpRelativeDockerfilePaths = i);


		var storageDeps = new Option<List<string>>("--storage-paths", "Local http service required storage, use format <service-name>:<storage-name>")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(storageDeps, (x, i) => x.storageDependenciesPaths = i);

		var shouldBeEnabled = new Option<List<string>>("--should-be-enable", "If this service should be enable on remote")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(shouldBeEnabled, (x, i) => x.shouldServiceBeEnabled = i);
	}

	public override async Task Handle(ServicesSetManifestCommandArgs args)
	{
		BeamableLogger.Log("handling");

		args.BeamoLocalSystem.BeamoManifest.Clear();

		var parityMatch = (args.localHttpRelativeDockerfilePaths.Count == args.localHttpNames.Count) &&
						 (args.localHttpRelativeDockerfilePaths.Count == args.localHttpBuildContextPaths.Count) &&
						 (args.localHttpRelativeDockerfilePaths.Count == args.shouldServiceBeEnabled.Count);
		if (!parityMatch)
		{
			throw new CliException("Invalid service count, must have equal parameter counts");
		}

		foreach (var storagePath in args.storageDependenciesPaths)
		{
			var storageName = Directory.GetDirectories(storagePath).Last();
			await args.BeamoLocalSystem.AddDefinition_EmbeddedMongoDb(storageName, "mongo:latest",
				storageName, CancellationToken.None);
		}
		for (var i = 0; i < args.localHttpNames.Count; i++)
		{
			var name = args.localHttpNames[i];
			var contextPath = args.localHttpBuildContextPaths[i];
			var dockerPath = args.localHttpRelativeDockerfilePaths[i];
			var shouldBeEnabled = true;

			try
			{
				shouldBeEnabled = bool.Parse(args.shouldServiceBeEnabled[i]);
			}
			catch
			{
				//Do nothing, something that can't be formatted to bool was passed, in that case leave it to be the default
				Log.Debug($"No valid value for shouldServiceBeEnabled was passed. Using default of {shouldBeEnabled} for service {name}");
			}

			Log.Debug($"name=[{name}] path=[{contextPath}] dockerfile=[{dockerPath}]");

			_ = await args.BeamoLocalSystem.AddDefinition_HttpMicroservice(name,
				contextPath,
				dockerPath,
				CancellationToken.None,
				shouldBeEnabled);
		}
		args.BeamoLocalSystem.SaveBeamoLocalManifest();
	}
}
