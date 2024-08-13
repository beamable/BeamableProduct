using Beamable.Common.BeamCli.Contracts;
using CliWrap;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

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
		var sw = new Stopwatch();
		sw.Start();
		var allProjects = ProjectContextUtil.FindCsharpProjects(rootFolder).ToArray();
		sw.Stop();
		Log.Verbose($"Gathering csprojs took {sw.Elapsed.TotalMilliseconds} ");

		var typeToProjects = allProjects
			.GroupBy(p => p.properties.ProjectType)
			.ToDictionary(kvp => kvp.Key, kvp => kvp.ToList());
		var fileNameToProject = allProjects.ToDictionary(kvp => kvp.fileNameWithoutExtension);
		var manifest = new BeamoLocalManifest
		{
			ServiceDefinitions = new List<BeamoServiceDefinition>(),
			HttpMicroserviceLocalProtocols = new BeamoLocalProtocolMap<HttpMicroserviceLocalProtocol> { },
			EmbeddedMongoDbLocalProtocols = new BeamoLocalProtocolMap<EmbeddedMongoDbLocalProtocol>() { },
			EmbeddedMongoDbRemoteProtocols = new BeamoRemoteProtocolMap<EmbeddedMongoDbRemoteProtocol>(),
			HttpMicroserviceRemoteProtocols = new BeamoRemoteProtocolMap<HttpMicroserviceRemoteProtocol>(),
			ServiceGroupToBeamoIds = new Dictionary<string, string[]>()
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
			var definition = ProjectContextUtil.ConvertProjectToServiceDefinition(serviceProject);
			var protocol = ProjectContextUtil.ConvertProjectToLocalHttpProtocol(serviceProject, fileNameToProject);
			manifest.ServiceDefinitions.Add(definition);
			manifest.HttpMicroserviceLocalProtocols.Add(definition.BeamoId, protocol);

			manifest.HttpMicroserviceRemoteProtocols.Add(definition.BeamoId, new HttpMicroserviceRemoteProtocol());
		}

		foreach (var storageProject in storageProjects)
		{
			var definition = ProjectContextUtil.ConvertProjectToStorageDefinition(storageProject, fileNameToProject);
			var protocol = ProjectContextUtil.ConvertProjectToLocalMongoProtocol(storageProject, definition, fileNameToProject, configService);
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
					ProjectPath = null,
					Protocol = BeamoProtocolType.HttpMicroservice,
					ServiceGroupTags = Array.Empty<string>()
				};
				manifest.ServiceDefinitions.Add(existingDefinition);
				manifest.HttpMicroserviceRemoteProtocols.Add(remoteService.serviceName, new HttpMicroserviceRemoteProtocol());

				existingDefinition.ShouldBeEnabledOnRemote = remoteService.enabled;
			}

			// overwrite existing local settings
			existingDefinition.ImageId = remoteService.imageId;
			existingDefinition.IsInRemote = true;
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
					ProjectPath = null,
					ServiceGroupTags = Array.Empty<string>()
				};
				manifest.ServiceDefinitions.Add(existingDefinition);
				manifest.EmbeddedMongoDbRemoteProtocols.Add(remoteStorage.id, new EmbeddedMongoDbRemoteProtocol());

				existingDefinition.ShouldBeEnabledOnRemote = remoteStorage.enabled;
			}

			// overwrite existing settings.
			existingDefinition.ImageId = MongoImage;
			existingDefinition.IsInRemote = true;
		}


		manifest.ServiceGroupToBeamoIds =
			ResolveServiceGroups(manifest.ServiceDefinitions, manifest.HttpMicroserviceLocalProtocols);

		return manifest;
	}

	/// <summary>
	/// Given a list of definitions with service-group tags, produce a dictionary that
	/// maps from service group name to the fully dependency resolved set of definitions
	/// in that group. The result dictionary's values are arrays of beamoIds.
	/// </summary>
	/// <param name="definitions"></param>
	/// <returns></returns>
	public static Dictionary<string, string[]> ResolveServiceGroups(
		List<BeamoServiceDefinition> definitions,
		BeamoLocalProtocolMap<HttpMicroserviceLocalProtocol> localServices
		)
	{
		var intermediateResult = new Dictionary<string, HashSet<string>>();

		foreach (var definition in definitions)
		{

			// add all the groups for this definition
			foreach (var group in definition.ServiceGroupTags)
			{
				if (!intermediateResult.TryGetValue(group, out var existing))
				{
					existing = intermediateResult[group] = new HashSet<string>();
				}
				existing.Add(definition.BeamoId);
			}

			// the only dependency we care about are service --> storage dependencies 
			if (definition.Protocol == BeamoProtocolType.HttpMicroservice &&
				localServices.TryGetValue(definition.BeamoId, out var service) &&
				service.StorageDependencyBeamIds?.Count > 0)
			{
				foreach (var group in definition.ServiceGroupTags)
				{
					if (!intermediateResult.TryGetValue(group, out var existing))
					{
						existing = intermediateResult[group] = new HashSet<string>();
					}

					foreach (var storageId in service.StorageDependencyBeamIds)
					{
						existing.Add(storageId);
					}
				}
			}
		}

		// convert the hashset into an array
		return intermediateResult.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
	}

	public static CsharpProjectMetadata[] FindCsharpProjects(string rootFolder)
	{
		if (string.IsNullOrEmpty(rootFolder))
		{
			rootFolder = ".";
		}

		var paths = Directory.GetFiles(rootFolder, "*.csproj", SearchOption.AllDirectories);
		var projects = new CsharpProjectMetadata[paths.Length];

		for (var i = 0; i < paths.Length; i++)
		{
			var path = paths[i];

			var fileReader = File.OpenRead(path);
			using var streamReader = new StreamReader(fileReader);
			string line = string.Empty;
			while (string.IsNullOrEmpty(line))
			{
				// read through the first whitespace of a file until the first actual content...
				line = streamReader.ReadLine()?.Trim();
			}

			if (!string.Equals("<Project Sdk=\"Microsoft.NET.Sdk\">", line,
					StringComparison.InvariantCultureIgnoreCase))
			{
				Log.Verbose($"Rejecting csproj=[{path}] due to lack of leading <Project Sdk> tag");
				projects[i] = null;
				continue;
			}


			var pathDir = Path.GetDirectoryName(path);

			if (string.IsNullOrEmpty(pathDir))
			{
				throw new CliException($"Expected a valid directory name from path = [{path}], but got nothing instead.");
			}

			var projectRelativePath = Path.GetRelativePath(rootFolder, pathDir);
			Log.Verbose($"Found csproj=[{path}]");
			projects[i] = new CsharpProjectMetadata
			{
				relativePath = Path.GetRelativePath(rootFolder, path),
				absolutePath = Path.GetFullPath(path),
				fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path)
			};

			var buildEngine = new ProjectCollection();
			buildEngine.IsBuildEnabled = true;
			var buildProject = buildEngine.LoadProject(Path.GetFullPath(path));

			projects[i].properties = new MsBuildProjectProperties()
			{
				BeamId = buildProject.GetPropertyValue(CliConstants.PROP_BEAMO_ID),
				Enabled = buildProject.GetPropertyValue(CliConstants.PROP_BEAM_ENABLED),
				ProjectType = buildProject.GetPropertyValue(CliConstants.PROP_BEAM_PROJECT_TYPE),
				ServiceGroupString = buildProject.GetPropertyValue(CliConstants.PROP_BEAM_SERVICE_GROUP)
			};

			var references = buildProject.GetItemsIgnoringCondition("ProjectReference");
			var allProjectRefs = new List<MsBuildProjectReference>();
			foreach (ProjectItem reference in references)
			{
				ProjectMetadata refTypeMetadata = reference.Metadata.FirstOrDefault(m => m.Name.Equals("BeamRefType"));
				ProjectMetadata assemblyMetadata = reference.Metadata.FirstOrDefault(m => m.Name.Equals(CliConstants.UNITY_ASSEMBLY_ITEM_NAME));
				allProjectRefs.Add(new MsBuildProjectReference()
				{
					RelativePath = reference.EvaluatedInclude,
					FullPath = Path.GetFullPath(Path.Combine(projectRelativePath, reference.EvaluatedInclude)),
					BeamRefType = refTypeMetadata?.EvaluatedValue,
					BeamUnityAssemblyName = assemblyMetadata?.EvaluatedValue
				});
			}
			projects[i].projectReferences = allProjectRefs;
		}

		return projects.Where(p => p != null).ToArray();
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

	public static HttpMicroserviceLocalProtocol ConvertProjectToLocalHttpProtocol(CsharpProjectMetadata project, Dictionary<string, CsharpProjectMetadata> absPathToProject)
	{
		var protocol = new HttpMicroserviceLocalProtocol();
		protocol.DockerBuildContextPath = ".";
		protocol.RelativeDockerfilePath = Path.Combine(Path.GetDirectoryName(project.relativePath), "Dockerfile");

		protocol.CustomVolumes = new List<DockerVolume>();
		protocol.InstanceCount = 1;
		protocol.CustomBindMounts = new List<DockerBindMount>();
		protocol.CustomPortBindings = new List<DockerPortBinding>();
		protocol.CustomEnvironmentVariables = new List<DockerEnvironmentVariable>();

		foreach (MsBuildProjectReference referencedProject in project.projectReferences)
		{
			var referencedName = Path.GetFileNameWithoutExtension(referencedProject.RelativePath);
			if (!absPathToProject.TryGetValue(referencedName, out var knownProject))
			{
				// Check if this is a Unity Assembly reference that does not have it's csproj generated yet
				if (!string.IsNullOrEmpty(referencedProject.BeamUnityAssemblyName))
				{
					protocol.UnityAssemblyDefinitionProjectReferences.Add(new UnityAssemblyReferenceData()
					{
						Path = referencedProject.RelativePath,
						AssemblyName = referencedProject.BeamUnityAssemblyName
					});
					continue;
				}

				Log.Warning($"Project=[{project.relativePath}] references project=[${referencedProject.FullPath}] but that project was not detected in the beamable folder context. ");
				continue;
			}

			var referenceType = knownProject.properties.ProjectType;
			switch (referenceType)
			{
				case "storage":
					protocol.StorageDependencyBeamIds.Add(ConvertBeamoId(knownProject));
					break;
				case "unity":
					protocol.UnityAssemblyDefinitionProjectReferences.Add(new UnityAssemblyReferenceData()
					{
						Path = referencedProject.RelativePath,
						AssemblyName = referencedProject.BeamUnityAssemblyName
					});
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

	static string[] ExtractServiceGroupTags(CsharpProjectMetadata project)
	{
		return project.properties.ServiceGroupString?.Split(CliConstants.SPLIT_OPTIONS,
				   StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			   ?? Array.Empty<string>();
	}

	public static BeamoServiceDefinition ConvertProjectToServiceDefinition(CsharpProjectMetadata project)
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
		definition.ProjectPath = project.relativePath;
		definition.Protocol = BeamoProtocolType.HttpMicroservice;
		definition.Language = BeamoServiceDefinition.ProjectLanguage.CSharpDotnet;

		definition.ServiceGroupTags = ExtractServiceGroupTags(project);


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
		definition.ProjectPath = project.relativePath;

		definition.Protocol = BeamoProtocolType.EmbeddedMongoDb;
		definition.Language = BeamoServiceDefinition.ProjectLanguage.CSharpDotnet;
		definition.ServiceGroupTags = ExtractServiceGroupTags(project);

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

		[JsonProperty(CliConstants.PROP_BEAM_SERVICE_GROUP)]
		public string ServiceGroupString;
	}

	public class MsBuildProjectReference
	{
		[JsonProperty("Identity")]
		public string RelativePath;

		[JsonProperty("FullPath")]
		public string FullPath;

		[JsonProperty(CliConstants.ATTR_BEAM_REF_TYPE)]
		public string BeamRefType;

		[JsonProperty(CliConstants.UNITY_ASSEMBLY_ITEM_NAME)]
		public string BeamUnityAssemblyName;
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

	/// <summary>
	/// Set an msbuild property in the given service definition
	/// </summary>
	/// <param name="definition"></param>
	/// <param name="propBeamEnabled"></param>
	/// <param name="false"></param>
	public static string ModifyProperty(BeamoServiceDefinition definition, string propertyName, string propertyValue)
	{
		var buildEngine = new ProjectCollection();
		var stream = File.OpenRead(definition.ProjectPath);
		var document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
		var reader = document.CreateReader(ReaderOptions.None);
		var buildProject = buildEngine.LoadProject(reader);
		Log.Verbose($"setting project=[{definition.ProjectPath}] property=[{propertyName}] value=[{propertyValue}]");

		var prop = buildProject.SetProperty(propertyName, propertyValue);

		buildProject.Save(definition.ProjectPath);

		/*
		 * if the property didn't exist, then dotnet's wisdom is to append <TheNewProperty> at the end of the last property...
		 * Ugh...
		 *
		 * <PropertyGroup>
		 *		<SomeOtherProperty>tuna</SomeOtherProperty><TheNewProperty>toast</TheNewProperty>
		 * </PropertyGroup>
		 *
		 * So, this little wizardry attempts to detect this nonsense and format the file. 
		 */

		var searchTerm = $"><{propertyName}>";
		var rawText = File.ReadAllText(definition.ProjectPath);

		var formattedText = FixMissingBreaklineInProject(searchTerm, rawText); ;
		File.WriteAllText(definition.ProjectPath, formattedText);

		return prop.UnevaluatedValue;
	}

	public static List<string> GetAllIncludedFiles(BeamoServiceDefinition definition, ConfigService configService)
	{
		var buildEngine = new ProjectCollection();
		Project buildProject = buildEngine.LoadProject(definition.ProjectPath);

		return GetAllIncludedFiles(buildProject, configService).Distinct().ToList();
	}

	public static List<string> GetAllIncludedFiles(Project project, ConfigService configService)
	{
		var results = new List<string>();

		var buildEngine = new ProjectCollection();

		const string PROJECT_REFERENCE_TYPE = "ProjectReference";
		const string COMPILE_TYPE = "Compile";

		//First check if there are other projects being referenced here, so we run through them too
		var references = project.GetItemsIgnoringCondition(PROJECT_REFERENCE_TYPE).ToArray();
		foreach (ProjectItem reference in references)
		{
			var name = Path.GetFileName(reference.EvaluatedInclude);
			var relativePath =
				configService.GetPathFromRelativeToService(reference.EvaluatedInclude, project.DirectoryPath);
			var combinedPath = Path.Combine(relativePath, name);
			var refProject = buildEngine.LoadProject(combinedPath);

			var partialResult = GetAllIncludedFiles(refProject, configService);
			results.AddRange(partialResult);
		}

		//Now goes through all compile items in this project
		var compiles = project.GetItemsIgnoringCondition(COMPILE_TYPE).ToArray();
		foreach (ProjectItem item in compiles)
		{
			if (item.UnevaluatedInclude.Equals("**/*$(DefaultLanguageSourceExtension)")) //This case is the default one of every csproj, we want to skip this
			{
				continue;
			}

			var allFiles = item.EvaluatedInclude.Split(";");
			foreach (var filePath in allFiles)
			{
				var name = Path.GetFileName(filePath);
				var relativePath =
					configService.GetPathFromRelativeToService(filePath, project.DirectoryPath);
				var fixedPath = Path.Combine(relativePath, name);
				results.Add(fixedPath);
			}
		}

		return results;
	}


	public static void UpdateUnityProjectReferences(BeamoServiceDefinition definition, List<string> projectsPaths, List<string> assemblyNames)
	{
		const string ITEM_TYPE = "ProjectReference";

		var buildEngine = new ProjectCollection();
		var buildProject = buildEngine.LoadProject(definition.ProjectPath);

		var references = buildProject.GetItemsIgnoringCondition(ITEM_TYPE).ToArray();
		for (int i = references.Length - 1; i >= 0; i--)
		{
			var reference = references[i];
			ProjectMetadata metaData = reference.Metadata.FirstOrDefault(m => m.Name.Equals(CliConstants.UNITY_ASSEMBLY_ITEM_NAME));
			if (metaData != null)
			{
				buildProject.RemoveItem(reference);
			}
		}

		for (int i = 0; i < assemblyNames.Count; i++)
		{
			var assemblyName = assemblyNames[i];
			var projectPath = projectsPaths[i].Replace("/", "\\");

			buildProject.AddItem(ITEM_TYPE, projectPath,
				new Dictionary<string, string> { { CliConstants.UNITY_ASSEMBLY_ITEM_NAME, assemblyName } });
		}

		buildProject.FullPath = definition.ProjectPath;
		buildProject.Save();

		var rawText = File.ReadAllText(definition.ProjectPath);

		//TODO improve this, it's kind of dumb right now running the filter
		// as many times as there were project references added
		for (int i = 0; i < assemblyNames.Count; i++)
		{
			rawText = FixMissingBreaklineInProject($"><{ITEM_TYPE}", rawText);
		}

		File.WriteAllText(definition.ProjectPath, rawText);
	}

	private static string FixMissingBreaklineInProject(string searchQuery, string fileContents)
	{
		var brokenIndex = fileContents.IndexOf(searchQuery, StringComparison.Ordinal);
		if (brokenIndex == -1)
		{
			return fileContents; // the glitch does not exist.
		}

		// we need to detect the amount of whitespace to insert...
		int newLineIndex =
			fileContents.LastIndexOf(Environment.NewLine, brokenIndex, brokenIndex, StringComparison.Ordinal);
		int contentIndex = fileContents.IndexOf('<', newLineIndex);
		var whiteSpace = fileContents.Substring(newLineIndex, contentIndex - newLineIndex);

		return fileContents.Insert(brokenIndex + 1, whiteSpace);
	}
}
