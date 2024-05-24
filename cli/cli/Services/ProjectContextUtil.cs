using Beamable.Common.BeamCli.Contracts;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;

namespace cli.Services;

public static class ProjectContextUtil
{
	public static async Task<BeamoLocalManifest> GenerateLocalManifest(
		string rootFolder,
		string dotnetPath,
		BeamoService beamo,
		ConfigService configService)
	{

		var remote = await beamo.GetCurrentManifest(); // TODO do this at the same time as file scanning.

		// find all local project files...
		var allProjects = (await ProjectContextUtil.FindCsharpProjects(dotnetPath, rootFolder)).ToArray();
		var typeToProjects = allProjects
			.GroupBy(p => p.properties.ProjectType)
			.ToDictionary(kvp => kvp.Key, kvp => kvp.ToList());
		var absPathToProject = allProjects.ToDictionary(kvp => kvp.absolutePath);
		var manifest = new BeamoLocalManifest
		{
			ServiceDefinitions = new List<BeamoServiceDefinition>(),
			HttpMicroserviceLocalProtocols = new BeamoLocalProtocolMap<HttpMicroserviceLocalProtocol> { },
			EmbeddedMongoDbLocalProtocols = new BeamoLocalProtocolMap<EmbeddedMongoDbLocalProtocol>() { },
			EmbeddedMongoDbRemoteProtocols = new BeamoRemoteProtocolMap<EmbeddedMongoDbRemoteProtocol>(),
			HttpMicroserviceRemoteProtocols = new BeamoRemoteProtocolMap<HttpMicroserviceRemoteProtocol>()

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
			var protocol = ProjectContextUtil.ConvertProjectToLocalHttpProtocol(serviceProject, definition, absPathToProject);
			manifest.ServiceDefinitions.Add(definition);
			manifest.HttpMicroserviceLocalProtocols.Add(definition.BeamoId, protocol);

			manifest.HttpMicroserviceRemoteProtocols.Add(definition.BeamoId, new HttpMicroserviceRemoteProtocol());
		}

		foreach (var storageProject in storageProjects)
		{
			var definition = ProjectContextUtil.ConvertProjectToStorageDefinition(storageProject, absPathToProject);
			var protocol = ProjectContextUtil.ConvertProjectToLocalMongoProtocol(storageProject, definition, absPathToProject, configService);
			manifest.EmbeddedMongoDbLocalProtocols.Add(definition.BeamoId, protocol);
			manifest.ServiceDefinitions.Add(definition);
			manifest.EmbeddedMongoDbRemoteProtocols.Add(definition.BeamoId, new EmbeddedMongoDbRemoteProtocol());
		}


		// add in the remote knowledge of services and storages
		foreach (var remoteService in remote.manifest)
		{
			if (!manifest.TryGetDefinition(remoteService.serviceName, out var existingDefinition))
			{
				existingDefinition = new BeamoServiceDefinition
				{
					BeamoId = remoteService.serviceName,
					Language = BeamoServiceDefinition.ProjectLanguage.CSharpDotnet,
					ProjectDirectory = null,
					Protocol = BeamoProtocolType.HttpMicroservice
				};
				manifest.ServiceDefinitions.Add(existingDefinition);
				manifest.HttpMicroserviceRemoteProtocols.Add(remoteService.serviceName, new HttpMicroserviceRemoteProtocol());

				existingDefinition.ShouldBeEnabledOnRemote = remoteService.enabled;
			}

			// overwrite existing local settings
			existingDefinition.ImageId = remoteService.imageId;
		}

		foreach (var remoteStorage in remote.storageReference)
		{
			if (!manifest.TryGetDefinition(remoteStorage.id, out var existingDefinition))
			{
				existingDefinition = new BeamoServiceDefinition
				{
					BeamoId = remoteStorage.id,
					Language = BeamoServiceDefinition.ProjectLanguage.CSharpDotnet,
					Protocol = BeamoProtocolType.EmbeddedMongoDb,
					ProjectDirectory = null
				};
				manifest.ServiceDefinitions.Add(existingDefinition);
				manifest.EmbeddedMongoDbRemoteProtocols.Add(remoteStorage.id, new EmbeddedMongoDbRemoteProtocol());

				existingDefinition.ShouldBeEnabledOnRemote = remoteStorage.enabled;
			}

			// overwrite existing settings.
			existingDefinition.ImageId = MongoImage;
		}

		return manifest;
	}


	public static async Task<CsharpProjectMetadata[]> FindCsharpProjects(string dotnetPath, string rootFolder)
	{
		if (string.IsNullOrEmpty(rootFolder))
		{
			rootFolder = ".";
		}

		var paths = Directory.GetFiles(rootFolder, "*.csproj", SearchOption.AllDirectories);
		var projects = new CsharpProjectMetadata[paths.Length];

		var propertyTasks = new List<Task<CsharpProjectBuildData>>();
		for (var i = 0; i < paths.Length; i++)
		{
			var path = paths[i];
			Log.Verbose($"Found csproj=[{path}]");
			projects[i] = new CsharpProjectMetadata
			{
				relativePath = Path.GetRelativePath(rootFolder, path),
				absolutePath = path,
				fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path),
				// properties = props
			};

			var task = GetCsharpProperties(dotnetPath, path, properties: new string[]
			{
				CliConstants.PROP_BEAMO_ID,
				CliConstants.PROP_BEAM_ENABLED,
				CliConstants.PROP_BEAM_PROJECT_TYPE,
			});
			propertyTasks.Add(task);
		}

		var propertyResults = await Task.WhenAll(propertyTasks);
		for (var i = 0; i < paths.Length; i++)
		{
			projects[i].properties = propertyResults[i].Properties;
			projects[i].projectReferences = propertyResults[i].Items.ProjectReference;
		}

		return projects;
	}

	public static async Task<CsharpProjectBuildData> GetCsharpProperties(string dotnetPath, string csharpPath, params string[] properties)
	{
		var propertyList = new HashSet<string>(properties);
		propertyList.Add("TargetFramework");
		propertyList.Add("OutDir");
		var propertyStringQuery = string.Join(",", propertyList);
		var argString = $"dotnet msbuild -getItem:ProjectReference -getProperty:{propertyStringQuery} {csharpPath}";
		var (result, buffer) = await CliExtensions.RunWithOutput(dotnetPath, argString);
		var json = buffer.ToString();
		Log.Verbose("got json: " + json);
		var instance = JsonConvert.DeserializeObject<CsharpProjectBuildData>(json);
		instance.Sanitize();
		return instance;
	}

	private const string MongoImage = "mongo:7.0";
	public static EmbeddedMongoDbLocalProtocol ConvertProjectToLocalMongoProtocol(CsharpProjectMetadata project,
		BeamoServiceDefinition beamoServiceDefinition, Dictionary<string, CsharpProjectMetadata> absPathToProject,
		ConfigService configService)
	{
		var protocol = new EmbeddedMongoDbLocalProtocol();

		// TODO: we could extract these as options in the Csproj file.
		protocol.BaseImage = MongoImage;
		protocol.RootUsername = "beamable";
		protocol.RootPassword = "beamable";
		if (configService.UseWindowsStyleVolumeNames)
		{
			protocol.DataVolumeInContainerPath = "C:/data/db";
			protocol.FilesVolumeInContainerPath = "C:/beamable";
		}
		else
		{
			protocol.DataVolumeInContainerPath = "/data/db";
			protocol.FilesVolumeInContainerPath = "/beamable";
		}


		protocol.MongoLocalPort = ""; // TODO: I don't think we actually need this, because we are getting the port via docker container inspection.

		foreach (var referencedProject in project.projectReferences)
		{
			if (!absPathToProject.TryGetValue(referencedProject.FullPath, out var knownProject))
			{
				Log.Warning($"Project=[{project.relativePath}] references project=[${referencedProject.FullPath}] but that project was not detected in the beamable folder context. ");
				continue;
			}

			var referenceType = knownProject.properties.ProjectType;
			switch (referenceType)
			{
				default:
					protocol.GeneralDependencyProjectPaths.Add(knownProject.relativePath);
					break;
			}
		}

		return protocol;
	}

	public static HttpMicroserviceLocalProtocol ConvertProjectToLocalHttpProtocol(CsharpProjectMetadata project,
		BeamoServiceDefinition beamoServiceDefinition, Dictionary<string, CsharpProjectMetadata> absPathToProject)
	{
		var protocol = new HttpMicroserviceLocalProtocol();
		protocol.DockerBuildContextPath = ".";
		protocol.RelativeDockerfilePath = Path.Combine(Path.GetDirectoryName(project.relativePath), "Dockerfile");

		protocol.CustomVolumes = new List<DockerVolume>();
		protocol.InstanceCount = 1;
		protocol.CustomBindMounts = new List<DockerBindMount>();
		protocol.CustomPortBindings = new List<DockerPortBinding>();
		protocol.CustomEnvironmentVariables = new List<DockerEnvironmentVariable>();

		foreach (var referencedProject in project.projectReferences)
		{
			if (!absPathToProject.TryGetValue(referencedProject.FullPath, out var knownProject))
			{
				Log.Warning($"Project=[{project.relativePath}] references project=[${referencedProject.FullPath}] but that project was not detected in the beamable folder context. ");
				continue;
			}

			var referenceType = knownProject.properties.ProjectType;
			switch (referenceType)
			{
				case "storage":
					protocol.StorageDependencyBeamIds.Add(ConvertBeamoId(knownProject));
					break;
				default:
					protocol.GeneralDependencyProjectPaths.Add(knownProject.relativePath);
					break;
			}
		}

		return protocol;
	}

	static string ConvertBeamoId(CsharpProjectMetadata metadata) => string.IsNullOrEmpty(metadata.properties.BeamId)
		? metadata.fileNameWithoutExtension
		: metadata.properties.BeamId;


	public static BeamoServiceDefinition ConvertProjectToServiceDefinition(CsharpProjectMetadata project, Dictionary<string, CsharpProjectMetadata> absPathToProject)
	{
		if (project.properties.ProjectType != "service")
		{
			throw new CliException(
				$"Assert failed; Trying to convert a project into a service project, but the project=[{project.absolutePath}] is of type=[{project.properties.ProjectType}]. This should have been prefiltered in the CLI. ");
		}

		var definition = new BeamoServiceDefinition();

		// the beamId will default to the csproj file name
		definition.BeamoId = ConvertBeamoId(project);

		// a service defaults to enabled, unless specifically disabled. 
		if (!bool.TryParse(project.properties.Enabled, out definition.ShouldBeEnabledOnRemote))
		{
			definition.ShouldBeEnabledOnRemote = true;
		}
		Log.Verbose($"set beam enabled for service=[{project.relativePath}] prop=[{project.properties.Enabled}] value=[{definition.ShouldBeEnabledOnRemote}]");


		// the project directory is just "where is the csproj" 
		definition.ProjectDirectory = Path.GetDirectoryName(project.relativePath);

		definition.Protocol = BeamoProtocolType.HttpMicroservice;
		definition.Language = BeamoServiceDefinition.ProjectLanguage.CSharpDotnet;




		return definition;
	}

	public static BeamoServiceDefinition ConvertProjectToStorageDefinition(CsharpProjectMetadata project, Dictionary<string, CsharpProjectMetadata> absPathToProject)
	{
		if (project.properties.ProjectType != "storage")
		{
			throw new CliException(
				$"Assert failed; Trying to convert a project into a storage project, but the project=[{project.absolutePath}] is of type=[{project.properties.ProjectType}]. This should have been prefiltered in the CLI. ");
		}

		string ConvertBeamoId(CsharpProjectMetadata metadata) => string.IsNullOrEmpty(metadata.properties.BeamId)
			? metadata.fileNameWithoutExtension
			: metadata.properties.BeamId;

		var definition = new BeamoServiceDefinition();

		// the beamId will default to the csproj file name
		definition.BeamoId = ConvertBeamoId(project);

		// a service defaults to enabled, unless specifically disabled. 
		if (!bool.TryParse(project.properties.Enabled, out definition.ShouldBeEnabledOnRemote))
		{
			definition.ShouldBeEnabledOnRemote = true;
		}

		// the project directory is just "where is the csproj" 
		definition.ProjectDirectory = Path.GetDirectoryName(project.relativePath);

		definition.Protocol = BeamoProtocolType.EmbeddedMongoDb;
		definition.Language = BeamoServiceDefinition.ProjectLanguage.CSharpDotnet;

		return definition;
	}



	public class MsBuildProjectProperties
	{
		[JsonProperty(CliConstants.PROP_BEAMO_ID)]
		public string BeamId;

		[JsonProperty(CliConstants.PROP_BEAM_ENABLED)]
		public string Enabled;

		[JsonProperty(CliConstants.PROP_BEAM_PROJECT_TYPE)]
		public string ProjectType;
	}

	public class MsBuildProjectReference
	{
		[JsonProperty("Identity")]
		public string RelativePath;

		[JsonProperty("FullPath")]
		public string FullPath;

		[JsonProperty(CliConstants.ATTR_BEAM_REF_TYPE)]
		public string BeamRefType;
	}

	public class MsBuildProjectItems
	{
		[JsonProperty("ProjectReference")]
		public List<MsBuildProjectReference> ProjectReference = new List<MsBuildProjectReference>();
	}



	[Serializable]
	public class CsharpProjectBuildData
	{
		[JsonProperty("Properties")] // the name is taken from msbuild's output.
		public MsBuildProjectProperties Properties = new MsBuildProjectProperties();

		[JsonProperty("Items")] // the name is taken from msbuild's output.
		public MsBuildProjectItems Items = new MsBuildProjectItems();

		public void Sanitize()
		{
			// TODO: clean up values of properties (like trim, lowercase, etc)
		}
	}

	[Serializable]
	[DebuggerDisplay("{fileNameWithoutExtension} : {relativePath}")]
	public class CsharpProjectMetadata
	{
		public string relativePath;
		public string absolutePath;
		public string fileNameWithoutExtension;

		public MsBuildProjectProperties properties;
		public List<MsBuildProjectReference> projectReferences;
	}

}
