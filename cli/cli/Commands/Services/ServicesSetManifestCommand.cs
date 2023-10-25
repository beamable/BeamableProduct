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
			Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true
		};
		AddOption(names, (x, i) => x.localHttpNames = i);
		
		
		var contextPaths = new Option<List<string>>("--local-http-contexts", "Local http service docker build contexts")
		{
			Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true
		};
		AddOption(contextPaths, (x, i) => x.localHttpBuildContextPaths = i);
		
		
		var dockerFiles = new Option<List<string>>("--local-http-docker-files", "Local http service relative docker file paths")
		{
			Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true
		};
		AddOption(dockerFiles, (x, i) => x.localHttpRelativeDockerfilePaths = i);
	}

	public override async Task Handle(ServicesSetManifestCommandArgs args)
	{
		BeamableLogger.Log("handling");

		args.BeamoLocalSystem.BeamoManifest.Clear();

		var arityMatch = (args.localHttpRelativeDockerfilePaths.Count == args.localHttpNames.Count) &&
		                 (args.localHttpRelativeDockerfilePaths.Count == args.localHttpBuildContextPaths.Count);
		if (!arityMatch)
		{
			throw new CliException("Invalid service count, must have equal parameter counts");
		}

		for (var i = 0; i < args.localHttpNames.Count; i++)
		{
			var name = args.localHttpNames[i];
			var contextPath = args.localHttpBuildContextPaths[i];
			var dockerPath = args.localHttpRelativeDockerfilePaths[i];
		
			Log.Debug($"name=[{name}] path=[{contextPath}] dockerfile=[{dockerPath}]");
			
			var sd = await args.BeamoLocalSystem.AddDefinition_HttpMicroservice(name,
				contextPath,
				dockerPath,
				new string[] { },
				CancellationToken.None);
			
			
		}
		args.BeamoLocalSystem.SaveBeamoLocalManifest();
	}
}
