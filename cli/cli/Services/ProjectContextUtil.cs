using System.Diagnostics;
using System.Text.Json;
using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Server;
using Beamable.Server.Common;
using cli.Utils;
using CliWrap;
using Microsoft.Build.Evaluation;
using Newtonsoft.Json;
using microservice.Extensions;
using Microsoft.OpenApi.Exceptions;
using Microsoft.OpenApi.Readers;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace cli.Services;

/// <summary>
/// Note about <see cref="ProjectCollection"/>: <see cref="ProjectCollection.LoadProject(string)"/> has different semantics than if you give it a XML FileStream.
/// Giving it the csproj File Path will correctly load up the MSBuild-specific properties, whereas if calling the FileStream version that doesn't happen.
/// </summary>
public static class ProjectContextUtil
{

	public static Promise<ServiceManifest> _existingManifest;
	private static DateTimeOffset _existingManifestCacheExpirationTime;
	private static object _existingManifestLock = new();
	private static readonly TimeSpan _existingManifestCacheTime = TimeSpan.FromSeconds(10);
	public static bool EnableManifestCache { get; set; } = true;
	public static void EvictManifestCache()
	{
		lock (_existingManifestLock)
		{
			_existingManifest = null;
			_existingManifestCacheExpirationTime = DateTimeOffset.Now;
		}
	}

	public static async Task SerializeSourceGenConfigToDisk(string rootFolder, BeamoServiceDefinition selectedService)
	{
		var serializedSourceGenConfig = System.Text.Json.JsonSerializer.Serialize(selectedService.FederationsConfig, new JsonSerializerOptions { IncludeFields = true});
		
		var projectDir = Path.GetDirectoryName(selectedService.AbsoluteProjectPath);
		var sourceGenPath = Path.Combine(projectDir, MicroserviceFederationsConfig.CONFIG_FILE_NAME);
		// Because this can be invoked from any point inside the root folder,
		// we have to figure out the absolute path to the file so we can call File.Write/Read apis correctly. 
		sourceGenPath = Path.Combine(rootFolder, sourceGenPath);
		await File.WriteAllTextAsync(sourceGenPath, serializedSourceGenConfig);
	}
	
	public static async Task<BeamoLocalManifest> GenerateLocalManifest(
		string dotnetPath, 
		BeamoService beamo,
		ConfigService configService,
		HashSet<string> ignoreIds,
		BeamActivity rootActivity,
		bool useCache=true,
		bool fetchServerManifest=true)
	{
		ServiceManifest remote = new ServiceManifest();
		using var activity = rootActivity.CreateChild("generateManifest");
		
		if (fetchServerManifest)
		{
			lock (_existingManifestLock)
			{
				var now = DateTimeOffset.Now;
				var ttlValid = now < _existingManifestCacheExpirationTime;
				var hasValue = _existingManifest != null;

				if (!useCache || !EnableManifestCache || !ttlValid || !hasValue)
				{
					_existingManifest = beamo.GetCurrentManifest();
					_existingManifestCacheExpirationTime = now + _existingManifestCacheTime;
					Log.Verbose("cached manifest is a miss.");
				}
				else
				{
					Log.Verbose("using cached manifest.");
				}
			}
			remote = await _existingManifest;
		}

		configService.GetProjectSearchPaths(out var rootFolder, out var searchPaths);
		var pathsToIgnore = configService.LoadPathsToIgnoreFromFile();
		var sw = new Stopwatch();
		sw.Start();

		foreach (var id in FindIgnoredBeamoIds(searchPaths))
		{
			ignoreIds.Add(id);
		}
		var allProjects = FindCsharpProjects(rootFolder, searchPaths, pathsToIgnore).ToArray();
		sw.Stop();
		Log.Verbose($"Gathering csprojs took {sw.Elapsed.TotalMilliseconds} ");
		sw.Restart();
		var typeToProjects = allProjects
			.GroupBy(p => p.properties.ProjectType)
			.ToDictionary(kvp => kvp.Key, kvp => kvp.ToList());
		var fileNameToProject = allProjects.ToDictionary(kvp => kvp.fileNameWithoutExtension);
		var manifest = new BeamoLocalManifest
		{
			LocallyIgnoredBeamoIds = ignoreIds,
			ServiceDefinitions = new List<BeamoServiceDefinition>(),
			HttpMicroserviceLocalProtocols = new BeamoLocalProtocolMap<HttpMicroserviceLocalProtocol>{},
			EmbeddedMongoDbLocalProtocols = new BeamoLocalProtocolMap<EmbeddedMongoDbLocalProtocol>(){},
			EmbeddedMongoDbRemoteProtocols = new BeamoRemoteProtocolMap<EmbeddedMongoDbRemoteProtocol>(),
			HttpMicroserviceRemoteProtocols = new BeamoRemoteProtocolMap<HttpMicroserviceRemoteProtocol>(),
			ServiceGroupToBeamoIds = new Dictionary<string, string[]>(),
		};


		// extract the "service" types, and convert them into beamo domain model
		if (!typeToProjects.TryGetValue("service", out var serviceProjects))
		{
			serviceProjects = new List<CsharpProjectMetadata>();
		}
		if (!typeToProjects.TryGetValue("storage", out var storageProjects))
		{
			storageProjects = new List<CsharpProjectMetadata>();
		}

		foreach (var serviceProject in serviceProjects)
		{
			var definition = ProjectContextUtil.ConvertProjectToServiceDefinition(serviceProject);
			if (ignoreIds.Contains(definition.BeamoId)) continue;
			
			var protocol = ProjectContextUtil.ConvertProjectToLocalHttpProtocol(serviceProject, fileNameToProject);
			manifest.ServiceDefinitions.Add(definition);
			manifest.HttpMicroserviceLocalProtocols.Add(definition.BeamoId, protocol);

			manifest.HttpMicroserviceRemoteProtocols.Add(definition.BeamoId, new HttpMicroserviceRemoteProtocol());
		}

		foreach (var storageProject in storageProjects)
		{
			var definition = ProjectContextUtil.ConvertProjectToStorageDefinition(storageProject, fileNameToProject);
			if (ignoreIds.Contains(definition.BeamoId)) continue;

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
					AbsoluteProjectPath = null,
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
					AbsoluteProjectPath = null,
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

		// Let's make sure all the service definitions have a paired SourceGenConfig file
		var microservicesOnly = manifest.ServiceDefinitions.Where(sd =>
		{
			var isMicroservice = sd.Protocol == BeamoProtocolType.HttpMicroservice;
			var isLocal = sd.IsLocal;
			return isMicroservice && isLocal;
		});
		await Task.WhenAll(microservicesOnly.Select(sd =>
		{
			var projectDir = Path.GetDirectoryName(sd.AbsoluteProjectPath);
			var sourceGenPath = Path.Combine(projectDir, MicroserviceFederationsConfig.CONFIG_FILE_NAME);
			
			// Because this can be invoked from any point inside the root folder,
			// we have to figure out the absolute path to the file so we can call File.Write/Read apis correctly. 
			sourceGenPath = Path.Combine(rootFolder, sourceGenPath);
			
			if (!File.Exists(sourceGenPath))
				return File.WriteAllTextAsync(sourceGenPath, "{}");

			return Task.CompletedTask;
		}));
		
		// Let's load all the SourceGenConfig files
		var sourceGenFiles = await Task.WhenAll(microservicesOnly.Select(sd =>
		{
			var projectDir = Path.GetDirectoryName(sd.AbsoluteProjectPath);
			var sourceGenPath = Path.Combine(projectDir, MicroserviceFederationsConfig.CONFIG_FILE_NAME);
			
			// Because this can be invoked from any point inside the root folder,
			// we have to figure out the absolute path to the file so we can call File.Write/Read apis correctly.
			sourceGenPath = Path.Combine(rootFolder, sourceGenPath);
			
			return File.ReadAllTextAsync(sourceGenPath);
		}));
		
		// Now we can deserialize and set it in the service definition
		foreach (var (sd, cfg) in microservicesOnly.Zip(sourceGenFiles))
		{
			try
			{
				sd.FederationsConfig = System.Text.Json.JsonSerializer.Deserialize<MicroserviceFederationsConfig>(cfg, new JsonSerializerOptions(){ IncludeFields = true });
			}
			catch (Exception e)
			{
				var projectDir = Path.GetDirectoryName(sd.AbsoluteProjectPath);
				var sourceGenPath = Path.Combine(projectDir, MicroserviceFederationsConfig.CONFIG_FILE_NAME);
			
				// Because this can be invoked from any point inside the root folder,
				// we have to figure out the absolute path to the file so we can call File.Write/Read apis correctly.
				sourceGenPath = Path.Combine(rootFolder, sourceGenPath);
				
				Log.Fatal(e, "Failed to load source gen config");
				throw new CliException($"Failed to parse {nameof(MicroserviceFederationsConfig)} at {sourceGenPath}. Please make sure the source gen config is valid json.");
			}
		}

		manifest.ServiceGroupToBeamoIds =
			ResolveServiceGroups(manifest.ServiceDefinitions, manifest.HttpMicroserviceLocalProtocols);
		
		
		sw.Stop();
		Log.Verbose($"Finishing manifest took {sw.Elapsed.TotalMilliseconds} ");
		
		activity.SetStatus(ActivityStatusCode.Ok);
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


	private static Dictionary<string, DateTime> _pathToLastWriteTime = new Dictionary<string, DateTime>();
	private static Dictionary<string, CsharpProjectMetadata> _pathToMetadata =
		new Dictionary<string, CsharpProjectMetadata>();
	
	static bool TryGetCachedProject(string path, out CsharpProjectMetadata metadata)
	{
		metadata = null;

		if (!_pathToLastWriteTime.TryGetValue(path, out var cachedWriteTime))
		{
			// we've never seen this file before!
			return false;
		}
		
		var lastWriteTime = File.GetLastWriteTime(path);
		if (lastWriteTime >= cachedWriteTime)
		{
			// the file has been modified since we last looked at it, so we should break the cache
			return false;
		}

		if (!_pathToMetadata.TryGetValue(path, out metadata))
		{
			// unexpected, but if we don't have the associated metadata, then obviously break the cache.
			return false;
		}

		// the metadata has been retrieved from the cache!
		return true;
	}

	static void CacheProjectNow(string path, CsharpProjectMetadata metadata)
	{
		_pathToLastWriteTime[path] = DateTime.Now;
		_pathToMetadata[path] = metadata;
	}

	public static HashSet<string> FindIgnoredBeamoIds(List<string> searchPaths)
	{

		// .beamignore files are files with beamoIds per line.
		// All beamoIds listed in these files need to be ignored
		// from the resulting local manifest. 

		var beamoIdsToIgnore = new HashSet<string>();
		foreach (var searchPath in searchPaths)
		{
			var somePaths = Directory.GetFiles(searchPath, "*.beamignore", SearchOption.AllDirectories);
			foreach (var path in somePaths)
			{
				var lines = File.ReadAllLines(path);
				foreach (var line in lines)
				{
					beamoIdsToIgnore.Add(line.Trim());
				}
			}
		}

		return beamoIdsToIgnore;
	}

	public static CsharpProjectMetadata[] FindCsharpProjects(string rootFolder, List<string> searchPaths, List<string> pathsToIgnore)
	{
		var sw = new Stopwatch();
		sw.Start();

		var pathList = new List<string>();
		foreach (var searchPath in searchPaths)
		{
			var somePaths = Directory.GetFiles(searchPath, "*.csproj", SearchOption.AllDirectories);
			pathList.AddRange(somePaths);
		}

		var filteredPaths = new List<string>();

		foreach (var path in pathList)
		{
			var canBeAdded = true;
			foreach (var pathToIgnore in pathsToIgnore)
			{
				if (path.StartsWith(pathToIgnore))
				{
					canBeAdded = false;
					break;
				}
			}

			if(canBeAdded) filteredPaths.Add(path);
		}

		var paths = filteredPaths.ToArray();
		
		var projects = new CsharpProjectMetadata[paths.Length];

		for (var i = 0 ; i < paths.Length; i ++)
		{
			var path = paths[i];

			if (TryGetCachedProject(path, out var metadata))
			{
				projects[i] = metadata;
				continue;
			}

			var fileReader = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
				projects[i] = null;
				CacheProjectNow(path, projects[i]);
				continue;
			}
			
			
			var pathDir = Path.GetDirectoryName(path);

			if (string.IsNullOrEmpty(pathDir))
			{
				throw new CliException($"Expected a valid directory name from path = [{path}], but got nothing instead.");
			}

			var projectRelativePath = Path.GetRelativePath(rootFolder, pathDir);
			Log.Verbose($"Found csproj=[{path}] - {sw.ElapsedMilliseconds}");
			projects[i] = new CsharpProjectMetadata
			{
				relativePath = Path.GetRelativePath(rootFolder, path),
				absolutePath = Path.GetFullPath(path),
				fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path)
			};

			var buildEngine = new ProjectCollection();
			buildEngine.IsBuildEnabled = true;
			var buildProject = buildEngine.LoadProject(Path.GetFullPath(path));

			// Log.Verbose($"loaded csproj - {sw.ElapsedMilliseconds}");

			string NullifyIfEmpty(string str)
			{
				if (string.IsNullOrEmpty(str)) return null;
				return str;
			}
			
			projects[i].properties = new MsBuildProjectProperties()
			{
				BeamId = buildProject.GetPropertyValue(CliConstants.PROP_BEAMO_ID),
				Enabled = buildProject.GetPropertyValue(CliConstants.PROP_BEAM_ENABLED),
				ProjectType = buildProject.GetPropertyValue(CliConstants.PROP_BEAM_PROJECT_TYPE),
				ServiceGroupString = buildProject.GetPropertyValue(CliConstants.PROP_BEAM_SERVICE_GROUP),
				StorageDataVolumeName = NullifyIfEmpty(buildProject.GetPropertyValue(CliConstants.PROP_BEAM_STORAGE_DATA_VOLUME_NAME)),
				StorageFilesVolumeName = NullifyIfEmpty(buildProject.GetPropertyValue(CliConstants.PROP_BEAM_STORAGE_FILES_VOLUME_NAME)),
				MongoBaseImage = NullifyIfEmpty(buildProject.GetPropertyValue(CliConstants.PROP_BEAM_STORAGE_MONGO_BASE_IMAGE)),
			};

			{ // load up the settings
				var beamableSettings = buildProject.GetItems("BeamableSetting").ToList();
				var beamableSettingsDict = new Dictionary<string, string>();
				foreach (var setting in beamableSettings)
				{
					var key = setting.EvaluatedInclude;
					var valueTypeHint = setting.GetMetadataValue("ValueTypeHint");
					var value = setting.GetMetadataValue("Value");
					if (valueTypeHint == "json")
					{
						value = JsonPrettify(value);
					}
					beamableSettingsDict[key] = value;
				}

				projects[i].msbuildProject = buildProject;
				projects[i].beamableSettings = new BuiltSettings(beamableSettingsDict);
				static string JsonPrettify(string json)
				{
					try
					{
						using var jDoc = JsonDocument.Parse(json);
						return System.Text.Json.JsonSerializer.Serialize(jDoc,
							new JsonSerializerOptions { WriteIndented = true, });
					}
					catch
					{
						// in the event of an error, ignore it and just return the original value.
						return json;
					}
				}
			}
			

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
			CacheProjectNow(path, projects[i]);
		}

		Log.Verbose("filtering csproj files only took " + sw.ElapsedMilliseconds);

		var result = projects.Where(p => p != null).ToArray();
		
		Log.Verbose("csproj linq non-null files took " + sw.ElapsedMilliseconds);

		sw.Stop();
		return result;
	}

	private const string MongoImage = "mongo:7.0";
	public static EmbeddedMongoDbLocalProtocol ConvertProjectToLocalMongoProtocol(CsharpProjectMetadata project,
		BeamoServiceDefinition beamoServiceDefinition, Dictionary<string, CsharpProjectMetadata> absPathToProject,
		ConfigService configService)
	{
		var protocol = new EmbeddedMongoDbLocalProtocol();
		
		// TODO: we could extract these as options in the Csproj file.
		protocol.BaseImage = project.properties.MongoBaseImage ?? MongoImage;
		protocol.RootUsername = "beamable";
		protocol.RootPassword = "beamable";
		protocol.Metadata = project;
		
		
		{ 
			// set the docker volume names. 
			//  the default values exist as _legacy_ naming to support CLI 2. 
			//  However, going forward, the names should be specified in the Storage Template
			protocol.FilesVolumeName =
				project.properties.StorageFilesVolumeName ?? $"{beamoServiceDefinition.BeamoId}_files";
			protocol.DataVolumeName =
				project.properties.StorageDataVolumeName ?? $"{beamoServiceDefinition.BeamoId}_data";
		}
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
	
	public static HttpMicroserviceLocalProtocol ConvertProjectToLocalHttpProtocol(CsharpProjectMetadata project, Dictionary<string, CsharpProjectMetadata> absPathToProject)
	{
		var protocol = new HttpMicroserviceLocalProtocol();
		protocol.DockerBuildContextPath = ".";
		protocol.AbsoluteDockerfilePath = Path.Combine(Path.GetDirectoryName(project.absolutePath), "Dockerfile");
		protocol.Metadata = project;
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

		string outDirDirectory = project.msbuildProject.GetPropertyValue(Beamable.Common.Constants.OPEN_API_DIR_PROPERTY_KEY).LocalizeSlashes();
		string openApiPath = protocol.ExpectedOpenApiDocPath = Path.Join(project.msbuildProject.DirectoryPath, outDirDirectory, Beamable.Common.Constants.OPEN_API_FILE_NAME);
		if (File.Exists(openApiPath))
		{
			var openApiStringReader = new OpenApiStringReader();
			var fileContent = File.ReadAllText(openApiPath);
			var openApiDocument = openApiStringReader.Read(fileContent, out var diagnostic);
			foreach (var warning in diagnostic.Warnings)
			{
				Log.Warning("found warning for {path}. {message} . from {pointer}", openApiPath, warning.Message,
					warning.Pointer);
				throw new OpenApiException($"invalid document {openApiPath} - {warning.Message} - {warning.Pointer}");
			}

			foreach (var error in diagnostic.Errors)
			{
				Log.Error("found ERROR for {path}. {message} . from {pointer}", openApiPath, error.Message,
					error.Pointer);
				throw new OpenApiException($"invalid document {openApiPath} - {error.Message} - {error.Pointer}");
			}
			protocol.OpenApiDoc = openApiDocument;
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
		definition.AbsoluteProjectPath = project.absolutePath;
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
		definition.AbsoluteProjectPath = project.absolutePath;
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

		[JsonProperty(CliConstants.PROP_BEAM_STORAGE_DATA_VOLUME_NAME)]
		public string StorageDataVolumeName;
		
		[JsonProperty(CliConstants.PROP_BEAM_STORAGE_FILES_VOLUME_NAME)]
		public string StorageFilesVolumeName;
		
		[JsonProperty(CliConstants.PROP_BEAM_STORAGE_MONGO_BASE_IMAGE)]
		public string MongoBaseImage;
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

	/// <summary>
	/// Set an msbuild property in the given service definition
	/// </summary>
	/// <param name="definition"></param>
	/// <param name="propBeamEnabled"></param>
	/// <param name="false"></param>
	public static string ModifyProperty(BeamoServiceDefinition definition, string propertyName, string propertyValue)
	{
		var buildEngine = new ProjectCollection();
		var buildProject = buildEngine.LoadProject(definition.AbsoluteProjectPath);
		Log.Verbose($"setting project=[{definition.AbsoluteProjectPath}] property=[{propertyName}] value=[{propertyValue}]");

		var prop = buildProject.SetProperty(propertyName, propertyValue);
		
		buildProject.Save(definition.AbsoluteProjectPath);
		
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
		var rawText = File.ReadAllText(definition.AbsoluteProjectPath);

		var formattedText = FixMissingBreaklineInProject(searchTerm, rawText);;
		File.WriteAllText(definition.AbsoluteProjectPath, formattedText);

		return prop.UnevaluatedValue;
	}

	public static List<string> GetAllIncludedFiles(BeamoServiceDefinition definition, ConfigService configService)
	{
		var buildEngine = new ProjectCollection();
		Project buildProject = buildEngine.LoadProject(definition.AbsoluteProjectPath);

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

	public static List<string> GetAllIncludedDlls(BeamoServiceDefinition definition, ConfigService configService)
	{
		var buildEngine = new ProjectCollection();
		Project buildProject = buildEngine.LoadProject(definition.AbsoluteProjectPath);

		return GetAllIncludedDlls(buildProject, configService);
	}

	public static List<string> GetAllIncludedDlls(Project project, ConfigService configService)
	{
		var buildEngine = new ProjectCollection();
		var results = new List<string>();
		const string REFERENCE_TYPE = "Reference";
		const string PROJECT_REFERENCE_TYPE = "ProjectReference";

		//First check if there are other projects being referenced here, so we run through them too
		var projs = project.GetItemsIgnoringCondition(PROJECT_REFERENCE_TYPE).ToArray();
		foreach (ProjectItem item in projs)
		{
			var name = Path.GetFileName(item.EvaluatedInclude);
			var relativePath =
				configService.GetPathFromRelativeToService(item.EvaluatedInclude, project.DirectoryPath);
			var combinedPath = Path.Combine(relativePath, name);
			var refProject = buildEngine.LoadProject(combinedPath);

			var partialResult = GetAllIncludedDlls(refProject, configService);
			results.AddRange(partialResult);
		}

		var references = project.GetItemsIgnoringCondition(REFERENCE_TYPE).ToArray();

		foreach (var dllReference in references)
		{
			ProjectMetadata hintPathMetadata = dllReference.Metadata.FirstOrDefault(m => m.Name.Equals("HintPath"));
			if (hintPathMetadata == null) continue;
			var dllPath = hintPathMetadata.EvaluatedValue;
			var name = Path.GetFileName(dllPath);
			var relativePath =
				configService.GetPathFromRelativeToService(dllPath, project.DirectoryPath);
			var fixedPath = Path.Combine(relativePath, name);
			results.Add(fixedPath);
		}

		return results;
	}


	public static async Task UpdateUnityProjectReferences(
		CommandArgs args,
		string solutionPath,
		BeamoServiceDefinition definition, 
		List<string> projectsPaths, 
		List<string> assemblyNames)
	{
		const string ITEM_TYPE = "ProjectReference";

		var buildEngine = new ProjectCollection();
		var buildProject = buildEngine.LoadProject(definition.AbsoluteProjectPath);
		var fullSlnPath = Path.GetFullPath(solutionPath);

		var references = buildProject.GetItemsIgnoringCondition(ITEM_TYPE).ToArray();
		for (int i = references.Length - 1; i >= 0; i--)
		{
			var reference = references[i];
			ProjectMetadata metaData = reference.Metadata.FirstOrDefault(m => m.Name.Equals(CliConstants.UNITY_ASSEMBLY_ITEM_NAME));
			if (metaData != null)
			{
				buildProject.RemoveItem(reference);

				var projPath = reference.UnevaluatedInclude;
				Log.Verbose("Removing reference to " + projPath);
				var slnProjPath = Path.Combine(definition.AbsoluteProjectDirectory, projPath);
				var slnArgStr = $"sln {fullSlnPath.EnquotePath()} remove {slnProjPath.EnquotePath()}";
				Log.Verbose($"removing assembly from solution, arg=[{slnArgStr}]");
				var command = CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath,slnArgStr)
					.WithValidation(CommandResultValidation.None)
					.WithStandardErrorPipe(PipeTarget.ToDelegate(err =>
					{
						Log.Error($"Could not remove unity ref from sln. err=[{err}]");
					}));
				await command.ExecuteAsync();
			}
		}

		for (int i = 0; i < assemblyNames.Count; i++)
		{
			var assemblyName = assemblyNames[i];
			var projectPath = projectsPaths[i];// TODO: Chris removed the replace on Oct 20th; not sure why it was there. Unity should auto-correct these paths already. //.Replace("/", "\\");

			buildProject.AddItem(ITEM_TYPE, projectPath,
				new Dictionary<string, string> { { CliConstants.UNITY_ASSEMBLY_ITEM_NAME, assemblyName } });

			var slnProjPath = Path.Combine(definition.AbsoluteProjectDirectory, projectPath);
			var slnArgStr = $"sln {fullSlnPath.EnquotePath()} add {slnProjPath.EnquotePath()} -s \"UnityAssemblies (shared)\"";
			Log.Verbose($"adding assembly to solution, arg=[{slnArgStr}]");
			var command = CliExtensions.GetDotnetCommand(args.AppContext.DotnetPath,slnArgStr)
				.WithValidation(CommandResultValidation.None)
				.WithStandardErrorPipe(PipeTarget.ToDelegate(err =>
				{
					Log.Error($"Could not add unity ref to sln. err=[{err}]");
				}));
			await command.ExecuteAsync();
		}

		buildProject.FullPath = definition.AbsoluteProjectPath;
		buildProject.Save();

		var rawText = File.ReadAllText(definition.AbsoluteProjectPath);

		//TODO improve this, it's kind of dumb right now running the filter
		// as many times as there were project references added
		for (int i = 0; i < assemblyNames.Count; i++)
		{
			rawText = FixMissingBreaklineInProject($"><{ITEM_TYPE}", rawText);
		}

		File.WriteAllText(definition.AbsoluteProjectPath, rawText);
	}

	public static void UpdateProjectDlls(BeamoServiceDefinition definition, List<string> dllPaths, List<string> dllNames)
	{
		var buildEngine = new ProjectCollection();
		const string REFERENCE_TYPE = "Reference";

		var refProject = buildEngine.LoadProject(definition.AbsoluteProjectPath);

		for (int i = 0; i < dllNames.Count; i++)
		{
			refProject.AddItem(REFERENCE_TYPE, dllNames[i], new Dictionary<string, string> { { CliConstants.HINT_PATH_ITEM_TAG, dllPaths[i] } });
		}

		refProject.FullPath = definition.AbsoluteProjectPath;
		refProject.Save();
	}

	private static string FixMissingBreaklineInProject(string searchQuery, string fileContents)
	{
		var brokenIndex = fileContents.IndexOf(searchQuery, StringComparison.Ordinal);
		if (brokenIndex == -1){
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

[Serializable]
[DebuggerDisplay("{fileNameWithoutExtension} : {relativePath}")]
public class CsharpProjectMetadata
{
	public string relativePath;
	public string absolutePath;
	public string fileNameWithoutExtension;

	public ProjectContextUtil.MsBuildProjectProperties properties;
	public List<ProjectContextUtil.MsBuildProjectReference> projectReferences;
	public BuiltSettings beamableSettings;
	public Project msbuildProject;
}
