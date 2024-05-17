using cli.Services;
using CliWrap;

namespace cli;

public class ServicesGenerateLocalManifestCommandArgs : CommandArgs
{
	
}

[Serializable]
public class ServicesGenerateLocalManifestCommandOutput
{
	public BeamoLocalManifest manifest;
}
public class ServicesGenerateLocalManifestCommand : AtomicCommand<ServicesGenerateLocalManifestCommandArgs, ServicesGenerateLocalManifestCommandOutput>, IStandaloneCommand
{
	public ServicesGenerateLocalManifestCommand() : base("generate-manifest", "Generate a local manifest by scraping local files")
	{
	}

	public override void Configure()
	{
		
	}

	public override async Task<ServicesGenerateLocalManifestCommandOutput> GetResult(ServicesGenerateLocalManifestCommandArgs args)
	{
		var rootFolder = args.ConfigService.BaseDirectory;

	
		// find all local project files...
		var allProjects = (await ProjectContextUtil.FindCsharpProjects(args.AppContext.DotnetPath, rootFolder)).ToArray();
		var typeToProjects = allProjects
			.GroupBy(p => p.properties.ProjectType)
			.ToDictionary(kvp => kvp.Key, kvp => kvp.ToList());
		var absPathToProject = allProjects.ToDictionary(kvp => kvp.absolutePath);
		var manifest = new BeamoLocalManifest
		{
			ServiceDefinitions = new List<BeamoServiceDefinition>(),
			HttpMicroserviceLocalProtocols = new BeamoLocalProtocolMap<HttpMicroserviceLocalProtocol>{}
		};
		
		// extract the "service" types, and convert them into beamo domain model
		if (!typeToProjects.TryGetValue("service", out var serviceProjects))
		{
			serviceProjects = new List<ProjectContextUtil.CsharpProjectMetadata>();
		}
		if (!typeToProjects.TryGetValue("storage", out var storageProjects))
		{
			storageProjects = new List<ProjectContextUtil.CsharpProjectMetadata>();
		}

		foreach (var serviceProject in serviceProjects)
		{
			var definition = ProjectContextUtil.ConvertProjectToServiceDefinition(serviceProject, absPathToProject);
			manifest.ServiceDefinitions.Add(definition);
		}

		foreach (var storageProject in storageProjects)
		{
			var definition = ProjectContextUtil.ConvertProjectToStorageDefinition(storageProject, absPathToProject);
			manifest.ServiceDefinitions.Add(definition);
		}
		
		return new ServicesGenerateLocalManifestCommandOutput
		{
			manifest = manifest
		};
	}
}
