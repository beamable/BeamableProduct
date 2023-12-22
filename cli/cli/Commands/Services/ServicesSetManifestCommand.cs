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
	public List<string> storageDependencies;
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


		var storageDeps = new Option<List<string>>("--storage-dependencies", "Local http service required storage, use format <service-name>:<storage-name>")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(storageDeps, (x, i) => x.storageDependencies = i);
		
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

		var arityMatch = (args.localHttpRelativeDockerfilePaths.Count == args.localHttpNames.Count) &&
						 (args.localHttpRelativeDockerfilePaths.Count == args.localHttpBuildContextPaths.Count) &&
						 (args.localHttpRelativeDockerfilePaths.Count == args.shouldServiceBeEnabled.Count);
		if (!arityMatch)
		{
			throw new CliException("Invalid service count, must have equal parameter counts");
		}

		var storages = new Dictionary<string, List<string>>();
		foreach (string storageDependency in args.storageDependencies)
		{
			var splitted = storageDependency.Split(':');
			if (splitted.Length != 2)
			{
				throw new CliException($"Invalid storage dependency argument format: {storageDependency}");
			}

			var storage = splitted[1];
			var depService = splitted[0];

			if (!args.localHttpNames.Any(s => s.Equals(depService)))
			{
				throw new CliException($"Invalid storage dependency argument, could not find service: {depService}");
			}

			if (!storages.ContainsKey(storage))
			{
				storages.Add(storage, new List<string>());
			}
			storages[storage].Add(depService);
		}


		foreach (KeyValuePair<string, List<string>> storageWithDeps in storages)
		{
			await args.BeamoLocalSystem.AddDefinition_EmbeddedMongoDb(storageWithDeps.Key, "mongo:latest",
				new string[] { }, CancellationToken.None);
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
			}

			Log.Debug($"name=[{name}] path=[{contextPath}] dockerfile=[{dockerPath}]");

			var serviceStorages = storages.Where(pair => pair.Value.Contains(name)).Select(pair => pair.Key).ToArray();
			
			

			var sd = await args.BeamoLocalSystem.AddDefinition_HttpMicroservice(name,
				contextPath,
				dockerPath,
				serviceStorages,
				CancellationToken.None,
				shouldBeEnabled);
		}
		args.BeamoLocalSystem.SaveBeamoLocalManifest();
	}
}
