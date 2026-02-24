using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Realms;
using Beamable.Common.Dependencies;
using Beamable.Server;
using cli.Commands.Project;
using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using microservice.Extensions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Exceptions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using ServiceConstants = Beamable.Common.Constants.Features.Services;

namespace cli.Services;

public partial class BeamoLocalSystem
{
	private static readonly Regex BeamoServiceIdRegex = new Regex("^[a-zA-Z0-9_]+$");

	/// <summary>
	/// A local manifest instance that we keep in sync with the <see cref="_beamoLocalManifestFile"/> json file.
	/// </summary>
	public BeamoLocalManifest BeamoManifest;

	/// <summary>
	/// The current local state of containers, associated with the <see cref="BeamoLocalManifest.ServiceDefinitions"/>, keept in sync with the <see cref="_beamoLocalRuntimeFile"/> json file.
	/// </summary>
	public readonly BeamoLocalRuntime BeamoRuntime;

	private readonly IDependencyProvider _provider;
	private readonly ConfigService _configService;

	/// <summary>
	/// The context for this service's execution. Holds the current <see cref="IAppContext.Pid"/>, <see cref="IAppContext.Cid"/> and <see cref="IAppContext.Token"/>
	/// which we use make custom requests to Beam-O's upload flow among other things.
	/// </summary>
	private readonly IAppContext _ctx;

	/// <summary>
	/// The current support API for talking to Beam-O.
	/// </summary>
	private readonly BeamoService _beamo;

	/// <summary>
	/// The realm service so we can grab the production realm's PID from whichever realm we are in.
	/// We do this so that Beam-O shares Docker images across realms to save space.
	/// </summary>
	private IRealmsApi _realmApi;

	/// <summary>
	/// The requester used to make requests to the beamale API.
	/// This is used to get information from microservices that are running locally during the deploy.
	/// </summary>
	private IBeamableRequester _beamableRequester;

	/// <summary>
	/// An instance of the docker client so that it can communicate with Docker for Windows and Docker for Mac ---- really, it should talk to any docker daemon.
	/// </summary>
	private readonly DockerClient _client;
	public DockerClient Client => _client;

	/// <summary>
	/// <see cref="StartListeningToDocker"/> and <see cref="StopListeningToDocker"/>.
	/// </summary>
	private Task _dockerListeningThread;

	/// <summary>
	/// <see cref="StartListeningToDocker"/> and <see cref="StopListeningToDocker"/>.
	/// </summary>
	private readonly CancellationTokenSource _dockerListeningThreadCancel;

	public BeamoLocalSystem(IDependencyProvider provider, ConfigService configService, IAppContext ctx, IRealmsApi realmsApi, BeamoService beamo, IBeamableRequester beamableRequester)
	{
		_provider = provider;
		_configService = configService;
		_ctx = ctx;
		_beamo = beamo;
		_realmApi = realmsApi;
		_beamableRequester = beamableRequester;

		// We use a 60 second timeout because the Docker Daemon is VERY slow... If you ever see an "The operation was cancelled" message that happens inconsistently,
		// try changing this value before going down the rabbit hole.
		var uri = GetLocalDockerEndpoint(configService);
		_client = new DockerClientConfiguration(uri, new AnonymousCredentials(), TimeSpan.FromSeconds(60))
			.CreateClient();

		// Load or create the local runtime data
		BeamoRuntime = new BeamoLocalRuntime() { ExistingLocalServiceInstances = new List<BeamoServiceInstance>(8)};

		// Make a cancellation token source to cancel the docker event stream we listen for updates. See StartListeningToDocker.
		_dockerListeningThreadCancel = new CancellationTokenSource();
	}
	

	public async Task InitManifest(bool useManifestCache=true, bool fetchServerManifest=true)
	{
		// Load or create the local manifest
		if (!_configService.DirectoryExists.GetValueOrDefault(false))
		{
			Log.Verbose("Beamo is initializing local manifest, but since no beamable folder exists, an empty manifest is being produced. ");
			BeamoManifest = new BeamoLocalManifest
			{
				ServiceDefinitions = new List<BeamoServiceDefinition>(),
				PortalExtensionDefinitions = new List<PortalExtensionDefinition>(),
				HttpMicroserviceLocalProtocols = new BeamoLocalProtocolMap<HttpMicroserviceLocalProtocol>(),
				EmbeddedMongoDbLocalProtocols = new BeamoLocalProtocolMap<EmbeddedMongoDbLocalProtocol>()
			};
			return;
		}
		
		
		BeamoManifest = await ProjectContextUtil.GenerateLocalManifest(_ctx.DotnetPath, _beamo, _configService, _ctx.IgnoreBeamoIds, _provider.GetService<BeamActivity>(), useCache: useManifestCache, fetchServerManifest);
	}
	
	private static Uri GetLocalDockerEndpoint(ConfigService config)
	{
		var custom = config.CustomDockerUri;
		if (!string.IsNullOrEmpty(custom))
		{
			
			Log.Verbose($"using custom docker uri=[{custom}]");
			return new Uri(custom);
		}
		
		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		if (isWindows)
		{
			var uri = new Uri("npipe://./pipe/docker_engine");
			Log.Verbose($"Using standard windows docker uri=[{uri}]");
			return uri;
		}

		var possibleLocations = new string[]
		{
			"/var/run/docker.sock", 
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.docker/run/docker.sock"
		};
		for (var i = 0; i < possibleLocations.Length; i++)
		{
			var location = possibleLocations[i];
			if (i == possibleLocations.Length -1 || File.Exists(location))
			{
				var uri = new Uri("unix:" + location);
				Log.Verbose($"Using standard unix docker uri=[{uri}]");
				return uri;
			}
		}

		throw new CliException($"No docker address found. Use the {ConfigService.ENV_VAR_DOCKER_URI} environment variable to set a Docker Uri.");
	}

	public void SaveBeamoLocalRuntime() {
		// TODO: remove this.
	}

	/// <summary>
	/// Checks to see if the service id matches the <see cref="BeamoServiceIdRegex"/>.
	/// </summary>
	public static bool ValidateBeamoServiceId_ValidCharacters(string beamoServiceId) =>
		BeamoServiceIdRegex.IsMatch(beamoServiceId);

	/// <summary>
	/// Get list of <see cref="BeamoId"/>s that this service depends on.
	/// </summary>
	/// <param name="beamoServiceId">The identifier of the Beamo service.</param>
	/// <returns>Returns a list of <see cref="BeamoId"/>s that this service depends on.</returns>
	public List<DependencyData> GetDependencies(string beamoServiceId, bool listAll = false)
	{
		if (!BeamoManifest.HttpMicroserviceLocalProtocols.TryGetValue(beamoServiceId,
			    out HttpMicroserviceLocalProtocol microservice))
		{
			return new List<DependencyData>(); // For now we only support dependencies for microservices depending on storages
		}

		List<DependencyData> dependencies = new List<DependencyData>();

		foreach (var name in microservice.StorageDependencyBeamIds)
		{
			bool hasDefinition = BeamoManifest.ServiceDefinitions.Any(definition => definition.BeamoId == name);

			if (!hasDefinition)
			{
				continue;
			}

			var definition = BeamoManifest.ServiceDefinitions.Find(s => s.BeamoId == name);

			dependencies.Add(new DependencyData()
			{
				name = name,
				projPath =  definition.ProjectDirectory,
				dllName = name, // TODO: We should have a better way to get this, for now we assume it's the same as the reference project name
				type = "storage"
			});
		}

		if (listAll)
		{
			foreach (var path in microservice.GeneralDependencyProjectPaths)
			{
				var name = Path.GetFileNameWithoutExtension(path);

				dependencies.Add(new DependencyData()
				{
					name = name,
					projPath = Path.GetDirectoryName(path),
					dllName = name, // TODO: We should have a better way to get this, for now we assume it's the same as the reference project name
					type = "library"
				});
			}

			foreach (UnityAssemblyReferenceData unityAsmdefReference in microservice.UnityAssemblyDefinitionProjectReferences)
			{
				var name = Path.GetFileNameWithoutExtension(unityAsmdefReference.Path);
				var microPath = Path.GetDirectoryName(microservice.AbsoluteDockerfilePath);
				var relativeToContextPath = _configService.GetPathFromRelativeToService(unityAsmdefReference.Path, microPath);

				dependencies.Add(new DependencyData()
				{
					name = name,
					projPath = relativeToContextPath,
					dllName = unityAsmdefReference.AssemblyName,
					type = "unity-asmdef"
				});
			}
		}

		return dependencies;
	}

	/// <summary>
	/// Removes the dependency between a microservice and a storage.
	/// </summary>
	/// <param name="project">The microservice to have the dependency removed from</param>
	/// <param name="dependency">The storage to remove as a dependency from the microservice</param>
	public async Task RemoveProjectDependency(BeamoServiceDefinition project, BeamoServiceDefinition dependency)
	{
		if (project.Protocol != BeamoProtocolType.HttpMicroservice ||
			dependency.Protocol != BeamoProtocolType.EmbeddedMongoDb)
		{
			throw new CliException(
				$"Currently the only supported dependencies are {nameof(BeamoProtocolType.HttpMicroservice)} depending on {nameof(BeamoProtocolType.EmbeddedMongoDb)}");
		}

		var relativeProjectPath = _configService.GetRelativeToBeamableWorkspacePath(project.ProjectDirectory);
		var projectPath = Path.Combine(relativeProjectPath, $"{project.BeamoId}.csproj");
		var dependencyPath = Path.Combine(_configService.GetRelativeToBeamableWorkspacePath(dependency.ProjectDirectory), $"{dependency.BeamoId}.csproj");

		var command = $"remove {projectPath.EnquotePath()} reference {dependencyPath.EnquotePath()}";
		var (cmd, result) = await CliExtensions.RunWithOutput(_ctx.DotnetPath, command);
		if (cmd.ExitCode != 0)
		{
			throw new CliException($"Failed to remove project dependency, output of \"dotnet {command}\": {result}");
		}

		await UpdateDockerFile(project);
	}

	/// <summary>
	/// Add a storage as a dependency of a microservice
	/// </summary>
	/// <param name="project">The microservice that the dependency will be added to</param>
	/// <param name="dependency">The storage to be the microservice dependency</param>
	public async Task AddProjectDependency(BeamoServiceDefinition project, string relativePath)
	{
		var projectPath = _configService.GetRelativeToExecutionPath(project.ProjectDirectory);
		var dependencyPath = relativePath;
		var command = $"add {projectPath.EnquotePath()} reference {dependencyPath.EnquotePath()}";
		var (cmd, result) = await CliExtensions.RunWithOutput(_ctx.DotnetPath, command);
		if (cmd.ExitCode != 0)
		{
			throw new CliException($"Failed to add project dependency, output of \"dotnet {command}\": {result}");
		}

		await UpdateDockerFile(project);
	}

	/// <summary>
	/// Retrieves the dependencies of each Beamo service defined in the BeamoManifest.
	/// </summary>
	/// <param name="projectExtension">The extension of the project file (default: 'csproj').</param>
	/// <returns>A dictionary where the key is a BeamoServiceDefinition and the value is a list of its dependencies.</returns>
	public Dictionary<BeamoServiceDefinition, List<DependencyData>> GetAllBeamoIdsDependencies(string projectExtension = "csproj", bool getAll = false)
	{
		var allBeamoIdsDependencies = new Dictionary<BeamoServiceDefinition, List<DependencyData>>();
		foreach (var definition in BeamoManifest.ServiceDefinitions)
		{
			if (!allBeamoIdsDependencies.ContainsKey(definition))
			{
				var entry = GetDependencies(definition.BeamoId, getAll);
				allBeamoIdsDependencies.Add(definition, entry);
			}
		}

		return allBeamoIdsDependencies;
	}
	
	
	public void SetBeamGroups(params UpdateGroupArgs[] args)
	{
		foreach (var arg in args)
		{
			if (!BeamoManifest.TryGetDefinition(arg.Name, out var definition))
			{
				throw new CliException($"Invalid service name: {arg.Name}");
			}
			var groups = definition.ServiceGroupTags.Union(arg.ToAddGroups).Except(arg.ToRemoveGroups).ToArray();
			if (definition.ServiceGroupTags.SequenceEqual(groups))
			{
				continue;
			}

			var relativeProjectPath = definition.AbsoluteProjectPath;
			var projectFile = File.ReadAllText(relativeProjectPath);
			XDocument doc = XDocument.Parse(projectFile);

			// Find the BeamServiceGroup element
			XElement beamServiceGroupElement = doc.Descendants("BeamServiceGroup").FirstOrDefault();
			var newGroupValue = string.Join(';',groups);
			if (beamServiceGroupElement != null)
			{
				beamServiceGroupElement.Value = newGroupValue;
			}
			else
			{
				// Find the PropertyGroup element with Label="Beamable Settings"
				XElement propertyGroupElement = doc.Descendants("PropertyGroup")
					.FirstOrDefault(e => (string)e.Attribute("Label") == "Beamable Settings");
				if (propertyGroupElement == null)
				{
					throw new CliException("Beamable Settings not found in project file.");
				}
				propertyGroupElement.Add(new XElement("BeamServiceGroup", newGroupValue));
			}

			var result = doc.ToString().Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>",string.Empty);
			File.WriteAllText(relativeProjectPath, result);
			
		}
	}

	public Promise UpdateDockerFile(BeamoServiceDefinition serviceDefinition)
	{
		return Promise.Success;
	}

	/// <summary>
	/// Verifies, by expanding the dependency DAG from root, we don't see root again until we have walked through all dependencies.
	/// </summary>
	private bool ValidateBeamoService_NoCyclicalDependencies(BeamoServiceDefinition root, List<BeamoServiceDefinition> registeredDependencies)
	{
		var depsToVisit = new Stack<BeamoServiceDefinition>();
		depsToVisit.Push(root);

		// TODO: Keep track of path you are taking in the tree
		var rootSeenCount = 0;
		while (depsToVisit.Count > 0)
		{
			var checking = depsToVisit.Pop();

			// Add to this counter whenever we see the root.
			rootSeenCount += checking.BeamoId == root.BeamoId ? 1 : 0;

			// If we see it more than once it means we have a cyclical dependency.
			if (rootSeenCount > 1)
				break;

			// Pushes dependencies of this service onto the stack
			var deps = (GetDependencies(checking.BeamoId)).Select(
					depId => registeredDependencies.FirstOrDefault(sd => sd.BeamoId == depId.name)
				)
				.Where(a => a != null)
				.ToList();

			foreach (var depService in deps)
				depsToVisit.Push(depService);
		}

		return rootSeenCount == 1;
	}
	
	/// <summary>
	/// Tries to run the given update task on the <see cref="IBeamoLocalProtocol"/> of the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/>.
	/// </summary>
	/// <param name="cancellationToken">A token that we pass to the given task. Can be used to cancel the task, if needed.</param>
	/// <typeparam name="TLocal">The <see cref="IBeamoLocalProtocol"/> that the service definition with the given <paramref name="beamoId"/> is expected to contain.</typeparam>
	/// <returns>Whether or not the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/> was found.</returns>
	public async Task<bool> TryUpdateLocalProtocol<TLocal>(string beamoId, LocalProtocolModifier<TLocal> localProtocolModifier, CancellationToken cancellationToken)
		where TLocal : class, IBeamoLocalProtocol
	{
		var containerIdx = BeamoManifest.ServiceDefinitions.FindIndex(container => container.BeamoId == beamoId);
		var foundContainer = containerIdx != -1;

		if (foundContainer)
		{
			var sd = BeamoManifest.ServiceDefinitions[containerIdx];
			var type = sd.Protocol;
			switch (type)
			{
				case BeamoProtocolType.HttpMicroservice:
				{
					await localProtocolModifier(sd, BeamoManifest.HttpMicroserviceLocalProtocols[sd.BeamoId] as TLocal).WaitAsync(cancellationToken);
					break;
				}
				case BeamoProtocolType.EmbeddedMongoDb:
				{
					await localProtocolModifier(sd, BeamoManifest.EmbeddedMongoDbLocalProtocols[sd.BeamoId] as TLocal).WaitAsync(cancellationToken);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		return foundContainer;
	}

}

/// <summary>
/// A function that takes in the <see cref="BeamoServiceDefinition"/> plus it's associated Local Protocol so that it can make changes to the protocol instance.
/// </summary>
/// <typeparam name="TLocal">The type of <see cref="IBeamoLocalProtocol"/> associated with the expected <see cref="BeamoServiceDefinition.Protocol"/>.</typeparam>
public delegate Task LocalProtocolModifier<in TLocal>(BeamoServiceDefinition owner, TLocal protocol) where TLocal : IBeamoLocalProtocol;

/// <summary>
/// A function that takes in the <see cref="BeamoServiceDefinition"/> plus it's associated Remote Protocol so that it can make changes to the protocol instance.
/// </summary>
/// <typeparam name="TRemote">The type of <see cref="IBeamoRemoteProtocol"/> associated with the given <see cref="BeamoServiceDefinition.Protocol"/>.</typeparam>
public delegate Task RemoteProtocolModifier<in TRemote>(BeamoServiceDefinition owner, TRemote protocol) where TRemote : IBeamoRemoteProtocol;

/// <summary>
/// A "typedef" around a <see cref="string"/> to <see cref="IBeamoLocalProtocol"/> <see cref="Dictionary{TKey,TValue}"/>.
/// </summary>
public class BeamoLocalProtocolMap<T> : Dictionary<string, T> where T : IBeamoLocalProtocol
{
}

/// <summary>
/// A "typedef" around a <see cref="string"/> to <see cref="IBeamoRemoteProtocol"/> <see cref="Dictionary{TKey,TValue}"/>.
/// </summary>
public class BeamoRemoteProtocolMap<T> : Dictionary<string, T> where T : IBeamoRemoteProtocol
{
}

/// <summary>
/// Our representation of <see cref="BeamoServiceDefinition"/> and the data they need to correctly enforce their defined <see cref="BeamoProtocolType"/> (stored in dictionaries.
/// </summary>
public class BeamoLocalManifest
{
	public string[] LocalBeamoIds
	{
		get
		{
			var ids = new string[HttpMicroserviceLocalProtocols.Count + EmbeddedMongoDbLocalProtocols.Count];
			var i = 0;
			foreach (var local in HttpMicroserviceLocalProtocols)
			{
				ids[i++] = local.Key;
			}
			foreach (var local in EmbeddedMongoDbLocalProtocols)
			{
				ids[i++] = local.Key;
			}

			return ids;
		}
	}

	/// <summary>
	/// The keys are service groups, specified through the &lt;BeamServiceGroup&gt; property.
	/// The values are the fully resolved list of beamoIds that are part of the group.
	///
	/// <para>
	/// If a service defines itself as part of a group, then all the service's dependencies are also
	/// part of the group.
	/// </para>
	/// </summary>
	public Dictionary<string, string[]> ServiceGroupToBeamoIds;
	
	/// <summary>
	/// This list contains all the <see cref="BeamoServiceDefinition"/> that the current machine knows about. TODO: At a minimum, this list is kept in sync with already deployed services?
	/// </summary>
	public List<BeamoServiceDefinition> ServiceDefinitions;

	/// <summary>
	/// This list contains all the <see cref="PortalExtensionDefinition"/> that the current project knows about.
	/// </summary>
	public List<PortalExtensionDefinition> PortalExtensionDefinitions;

	/// <summary>
	/// This list contains the concatenation of all found `.beamignore` files in the workspace.
	/// Each element is a beamoId. 
	/// </summary>
	public HashSet<string> LocallyIgnoredBeamoIds;
	
	/// <summary>
	/// These are map individual <see cref="BeamoServiceDefinition.BeamoId"/>s to their protocol data. Since we don't allow changing protocols we don't ever need to move the services' protocol data between these.
	/// </summary>
	public BeamoLocalProtocolMap<HttpMicroserviceLocalProtocol> HttpMicroserviceLocalProtocols;

	/// <summary>
	/// These are map individual <see cref="BeamoServiceDefinition.BeamoId"/>s to their protocol data. Since we don't allow changing protocols we don't ever need to move the services' protocol data between these.
	/// </summary>
	public BeamoRemoteProtocolMap<HttpMicroserviceRemoteProtocol> HttpMicroserviceRemoteProtocols;


	/// <summary>
	/// These are map individual <see cref="BeamoServiceDefinition.BeamoId"/>s to their protocol data. Since we don't allow changing protocols we don't ever need to move the services' protocol data between these.
	/// </summary>
	public BeamoLocalProtocolMap<EmbeddedMongoDbLocalProtocol> EmbeddedMongoDbLocalProtocols;

	/// <summary>
	/// These are map individual <see cref="BeamoServiceDefinition.BeamoId"/>s to their protocol data. Since we don't allow changing protocols we don't ever need to move the services' protocol data between these.
	/// </summary>
	public BeamoRemoteProtocolMap<EmbeddedMongoDbRemoteProtocol> EmbeddedMongoDbRemoteProtocols;

	public void Clear()
	{
		ServiceDefinitions.Clear();
		HttpMicroserviceLocalProtocols.Clear();
		HttpMicroserviceRemoteProtocols.Clear();
		EmbeddedMongoDbLocalProtocols.Clear();
		EmbeddedMongoDbRemoteProtocols.Clear();
	}

	/// <summary>
	/// Tries to get the definition of a Beamo service based on the provided BeamoId.
	/// </summary>
	/// <param name="beamoId">The BeamoId of the service.</param>
	/// <param name="definition">When this method returns, contains the BeamoServiceDefinition associated with the specified BeamoId, if found; otherwise, null.</param>
	/// <returns>
	/// true if the service definition is found; otherwise, false.
	/// </returns>
	public bool TryGetDefinition(string beamoId, out BeamoServiceDefinition definition)
	{
		definition = ServiceDefinitions.FirstOrDefault(definition => definition.BeamoId == beamoId);

		return definition != null;
	}
}

public class PortalExtensionDefinition
{
	public string Name;
	public string Version;
	public string Type;

	public string RelativePath;
	public string AbsolutePath;
	public List<string> MicroserviceDependencies;
}

public class BeamoServiceDefinition
{
	public bool IsInRemote;
	public bool IsLocal => !string.IsNullOrEmpty(ProjectDirectory);

	public enum ProjectLanguage { CSharpDotnet, }

	/// <summary>
	/// The id that this service will be know, both locally and remotely.
	/// </summary>
	public string BeamoId;

	/// <summary>
	/// The type of the project for this --- for now, can only be <see cref="ProjectLanguage.CSharpDotnet"/>, in the future, we might have different flavor MSs.
	/// TODO: When we start supporting different languages for microservices, this will be allowed to change --- for now, it is always <see cref="ProjectLanguage.CSharpDotnet"/>.
	/// </summary>
	public ProjectLanguage Language = ProjectLanguage.CSharpDotnet;

	/// <summary>
	/// The protocol this service respects.
	/// </summary>
	public BeamoProtocolType Protocol;

	/// <summary>
	/// The <see cref="MicroserviceFederationsConfig"/> for this microservice.
	/// </summary>
	public MicroserviceFederationsConfig FederationsConfig 
		// create a default instance so that downstream callers don't need to check for isLocal over and over again. 
		= new MicroserviceFederationsConfig();

	public static async Task<MicroserviceFederationsConfig> ReloadFederationsData(string openApiPath)
	{
		// string openApiPath = definition.OpenApiPath;
		if (File.Exists(openApiPath))
		{
			var openApiStringReader = new OpenApiStringReader();
			var fileContent = await File.ReadAllTextAsync(openApiPath);
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

			if (!openApiDocument.Extensions.TryGetValue(ServiceConstants.MICROSERVICE_FEDERATED_COMPONENTS_V2_KEY,
				    out var ext) ||
			    ext is not OpenApiArray { Count: > 0 } federationIds)
			{
				return new MicroserviceFederationsConfig();
			}

			Dictionary<string, List<FederationInstanceConfig>> foundFederationsAndInterfaces = new();
			foreach (IOpenApiAny openApiAny in federationIds)
			{
				// federationId: Is the Federation ID set in the FederationId attribute in the C# Federation Class. Ex: "default"
				// interfaceFullname: Is the fullname of the Interface on which that federation uses

				// We can skip this federation if there is an error when parsing or if the OpenApi is invalid
				// If not an OpenApiObject OR
				// federationId doesn't exist OR
				// interfaceFullName doesn't exist
				if (openApiAny is not OpenApiObject obj)
					continue;
				if (!obj.TryGetValue(ServiceConstants.MICROSERVICE_FEDERATED_COMPONENTS_V2_FEDERATION_ID_KEY,
					    out var extId) ||
				    extId is not OpenApiString { Value: var federationId })
					continue;
				if (!obj.TryGetValue(ServiceConstants.MICROSERVICE_FEDERATED_COMPONENTS_V2_INTERFACE_KEY,
					    out var extInterface) ||
				    extInterface is not OpenApiString { Value: var interfaceFullName })
					continue;
				if (!obj.TryGetValue(ServiceConstants.MICROSERVICE_FEDERATED_COMPONENTS_V2_FEDERATION_CLASS_NAME_KEY,
					    out var extFedClassName) ||
				    extFedClassName is not OpenApiString { Value: var federationClassName })
					continue;

				string interfaceNameOnly = interfaceFullName.Split('.').Last();
				var federationInstanceConfig = new FederationInstanceConfig
				{
					Interface = interfaceNameOnly,
					ClassName = federationClassName
				};

				if (foundFederationsAndInterfaces.TryGetValue(federationId, out var interfaces))
				{

					interfaces.Add(federationInstanceConfig);
				}
				else
				{
					foundFederationsAndInterfaces[federationId] = new List<FederationInstanceConfig>
						{ federationInstanceConfig };
				}
			}

			var federationsConfig =
				new FederationsConfig(
					foundFederationsAndInterfaces.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray()));

			return new MicroserviceFederationsConfig() { Federations = federationsConfig };

		}
		else
		{
			return new MicroserviceFederationsConfig();
		}
	}

	/// <summary>
	/// Gets the truncated version of the image id (used for deploying the service manifest to Beamo. TODO Ideally, we should make beamo use the full ID later...
	/// </summary>
	[JsonIgnore]
	public string TruncImageId
	{
		get
		{
			if (string.IsNullOrEmpty(ImageId) || Protocol != BeamoProtocolType.HttpMicroservice) return null;
			const int minimumChars = 12;

			if (ImageId.Contains(':'))
			{
				var version = ImageId.Split(':')[1];

				if (version.Length < minimumChars)
				{
					throw new CliException($"Invalid format of image Version=[{version}]. Should have at least {minimumChars} characters.");
				}
				return version.Substring(0, minimumChars);
			}
			else
			{
				return ImageId;
			}
		}
	}

	/// <summary>
	/// This is what we need for deployment.
	/// </summary>
	public string ImageId;

	/// <summary>
	/// Whether or not this service should be enabled when we deploy remotely.
	/// </summary>
	public bool ShouldBeEnabledOnRemote;

	/// <summary>
	/// A set of tags that can be used to manipulate or control the services as a group.
	/// This data is sourced from the &lt;BeamServiceGroup&gt; tag in the project's .csproj file, as
	/// a comma separated list. 
	/// </summary>
	public string[] ServiceGroupTags;

	/// <summary>
	/// Path to the directory containing project file(csproj).
	/// </summary>
	public string ProjectDirectory => ProjectPath == null ? null : Path.GetDirectoryName(ProjectPath);

	public string AbsoluteProjectDirectory =>
		AbsoluteProjectPath == null ? null : Path.GetDirectoryName(AbsoluteProjectPath);
	
	/// <summary>
	/// Path to the services csproj file. This path is relative to the beamable workspace root.
	/// Use <see cref="AbsoluteProjectPath"/> if you need to read/write the file. 
	/// </summary>
	public string ProjectPath;

	/// <summary>
	/// Absolute path to csproj file
	/// </summary>
	public string AbsoluteProjectPath;

	/// <summary>
	/// Absolute path to the microservice OpenApi specs
	/// </summary>
	public string OpenApiPath;
	
	/// <summary>
	/// Defines two services as being equal simply by using their <see cref="BeamoServiceDefinition.BeamoId"/>.
	/// </summary>
	public struct IdEquality : IEqualityComparer<BeamoServiceDefinition>
	{
		public bool Equals(BeamoServiceDefinition x, BeamoServiceDefinition y) => x.BeamoId == y.BeamoId;

		public int GetHashCode(BeamoServiceDefinition obj) => (obj.BeamoId != null ? obj.BeamoId.GetHashCode() : 0);
	}

	/// <summary>
	/// Converts a list of <see cref="DockerPortBinding"/> to the format expected by the Docker.NET API (<see cref="BeamoLocalSystem._client"/>).
	/// </summary>
	public static void BuildExposedPorts(List<DockerPortBinding> bindings, out Dictionary<string, EmptyStruct> exposedPorts) =>
		exposedPorts = bindings.ToDictionary(b => b.InContainerPort, _ => new EmptyStruct());

	/// <summary>
	/// Converts a list of <see cref="DockerPortBinding"/> to the second format expected by the Docker.NET API (<see cref="BeamoLocalSystem._client"/>).
	/// </summary>
	public static void BuildHostPortBinding(List<DockerPortBinding> bindings, out Dictionary<string, IList<PortBinding>> boundPorts) =>
		boundPorts = bindings
			.ToDictionary(b => b.InContainerPort,
				b => (IList<PortBinding>)new List<PortBinding>() { new() { HostPort = b.LocalPort } });

	/// <summary>
	/// Converts a list of <see cref="DockerEnvironmentVariable"/> to the format expected by the Docker.NET API (<see cref="BeamoLocalSystem._client"/>).
	/// </summary>
	public static void BuildEnvVars(out List<string> envs, params List<DockerEnvironmentVariable>[] envVarMaps)
	{
		envs = new List<string>();
		foreach (var envVars in envVarMaps)
			envs.AddRange(envVars.Select(envVar => $"{envVar.VariableName}={envVar.Value}"));
	}

	/// <summary>
	/// Converts a list of <see cref="DockerVolume"/> to the format expected by the Docker.NET API (<see cref="BeamoLocalSystem._client"/>).
	/// </summary>
	public static void BuildVolumes(List<DockerVolume> volumes, out List<string> boundVolumes)
	{
		boundVolumes = new List<string>(volumes.Count);
		boundVolumes.AddRange(volumes.Select(v => $"{v.VolumeName}:{v.InContainerPath}"));
	}

	/// <summary>
	/// Converts a list of <see cref="DockerBindMount"/> to the format expected by the Docker.NET API (<see cref="BeamoLocalSystem._client"/>).
	/// </summary>
	public static void BuildBindMounts(List<DockerBindMount> bindMounts, out List<string> boundMounts)
	{
		boundMounts = new List<string>(bindMounts.Count);
		boundMounts.AddRange(bindMounts.Select(bm =>
		{
			var options = bm.IsReadOnly ? ":ro" : "";
			return $"{bm.LocalPath}:{bm.InContainerPath}{options}";
		}));
	}

	/// <summary>
	/// <inheritdoc cref="GetProjectExtension"/>
	/// </summary>
	public string ProjectExtension => GetProjectExtension(Language);

	/// <summary>
	/// Given a project language, will return an individual project/module file extension ("csproj" for dotnet, "mod" for go, etc...)
	/// </summary>
	public static string GetProjectExtension(ProjectLanguage language) => language switch
	{
		ProjectLanguage.CSharpDotnet => "csproj",
		_ => throw new ArgumentOutOfRangeException()
	};

	/// <summary>
	/// <inheritdoc cref="GetSolutionExtension"/>
	/// </summary>
	public string SolutionExtension => GetSolutionExtension(Language);

	/// <summary>
	/// Given a language, return a "group of project" file extension ("sln" for dotnet, "work" for go, etc...)
	/// </summary>
	public static string GetSolutionExtension(ProjectLanguage language) => language switch
	{
		ProjectLanguage.CSharpDotnet => "sln",
		_ => throw new ArgumentOutOfRangeException()
	};
}

/// <summary>
/// Data representing a Docker Port Binding.
/// </summary>
public struct DockerPortBinding
{
	/// <summary>
	/// The port in localhost.
	/// </summary>
	public string LocalPort;

	/// <summary>
	/// The port the application inside the container cares about.
	/// </summary>
	public string InContainerPort;
}

/// <summary>
/// Data representing a Docker Named Volume.
/// </summary>
public struct DockerVolume
{
	/// <summary>
	/// The name of the volume.
	/// </summary>
	public string VolumeName;

	/// <summary>
	/// The path into the container's file system where this named volume will be bound to.
	/// </summary>
	public string InContainerPath;
}

/// <summary>
/// Data representing a Docker Bind Mount.
/// </summary>
public struct DockerBindMount
{
	/// <summary>
	/// Whether the container is allowed to write to the localhost's file system.
	/// </summary>
	public bool IsReadOnly;

	/// <summary>
	/// Path in localhost.
	/// </summary>
	public string LocalPath;

	/// <summary>
	/// Path into the container's file system where the directory in <see cref="LocalPath"/> will be mounted into.
	/// </summary>
	public string InContainerPath;
}

/// <summary>
/// Data representing an Environment Variable definition.
/// </summary>
public struct DockerEnvironmentVariable
{
	public string VariableName;
	public string Value;
}

/// <summary>
/// The type of protocol a <see cref="BeamoServiceDefinition.Protocol"/> was created with. Conceptually, a protocol is just a set of algorithms and data that solve the problem of:
///  - What protocol do I follow to get this service running locally and in Beamo?
///  - What data does this protocol need to make the service run correctly in each of these places? (<see cref="IBeamoLocalProtocol"/> and <see cref="IBeamoRemoteProtocol"/>)
///  - How do I cleanup the local running Docker Engine given what I had to do to get the service running? (Some protocols may spawn multiple docker images, for example).
///
/// So, while the <see cref="BeamoServiceDefinition"/> deals with the dependencies between the services and whether or not it should be enabled when we deploy remotely to Beamo,
/// the protocol defines how Beamo (remote or local) will actually get the service to work.
/// </summary>
public enum BeamoProtocolType
{
	// Current C#MS stuff (after we remove the WebSocket stuff)
	HttpMicroservice,

	// Current Mongo-based Data Storage
	EmbeddedMongoDb,
}

public interface IBeamoLocalProtocol
{
	public bool VerifyCanBeBuiltLocally(ConfigService configService);
}

public interface IBeamoRemoteProtocol
{
}

// Move this stuff to app context and initialization

[Serializable]
public class DependencyData
{
	public string name;
	public string projPath;
	public string dllName;
	public string type;
}

[Serializable]
public class CustomerResponse
{
	public CustomerDTO customer;
}

[Serializable]
public class CustomerDTO
{
	public List<ProjectDTO> projects;
}

[Serializable]
public class ProjectDTO
{
	public string name;
	public string secret;
}
