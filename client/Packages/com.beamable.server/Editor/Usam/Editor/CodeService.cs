using Beamable.Api.CloudSaving;
using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Dependencies;
using Beamable.Common.Semantics;
using Beamable.Editor;
using Beamable.Editor.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Dotnet;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.UI.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Beamable.Server.Editor.Usam
{
	public class CodeService : ILoadWithContext
	{
		private readonly BeamCommands _cli;
		private readonly BeamableDispatcher _dispatcher;
		private readonly DotnetService _dotnetService;

		public Promise OnReady { get; private set; }
		public Action<string, BeamTailLogMessageForClient> OnLogMessage;
		public bool IsDockerRunning { get; private set; }

		public List<IBeamoServiceDefinition> ServiceDefinitions { get; private set; } =
			new List<IBeamoServiceDefinition>();

		public List<BeamDependencyData> Libraries { get; private set; } = new List<BeamDependencyData>();

		private static readonly List<string> IgnoreFolderSuffixes = new List<string> { "~", "obj", "bin" };
		private List<Promise> _logsCommands = new List<Promise>();

		private const string BEAMABLE_MIGRATION_CANCELLATION_LOG = "Migration was cancelled!";

		private const string BEAMABLE_PATH = "Assets/Beamable/";
		private const string BEAMABLE_LIB_PATH = "Library/BeamableEditor";
		private const string MICROSERVICE_DLL_PATH = "bin/Debug/net6.0"; // is this true for all platforms and dotnet installations?
		public static readonly string StandaloneMicroservicesFolderName = "StandaloneMicroservices~/";
		private static readonly string StandaloneMicroservicesPath = $"{BEAMABLE_PATH}{StandaloneMicroservicesFolderName}";
		public static readonly string LibrariesPathsFilePath = $"{BEAMABLE_LIB_PATH}/.libraries_paths";
		public static readonly string ServicesDefinitionsFilePath = $"{BEAMABLE_LIB_PATH}/.services_definitions";
		private IDependencyProvider _provider;
		public static string LibrariesPathsDirectory => Path.GetDirectoryName(LibrariesPathsFilePath);
		public static string ServicesDefinitionsDirectory => Path.GetDirectoryName(ServicesDefinitionsFilePath);

		public CodeService(IDependencyProvider provider, BeamCommands cli, BeamableDispatcher dispatcher, DotnetService dotnetService)
		{
			_provider = provider;
			_cli = cli;
			_dispatcher = dispatcher;
			_dotnetService = dotnetService;
			OnReady = Init();
		}


		public async Promise Init()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			UsamLogger.ResetLogTimer();

			UsamLogger.Log("Running init");

			UsamLogger.Log("Setting properties file");
			await SetPropertiesFile();

			UsamLogger.Log("Saving all libraries referenced by services");
			await SaveReferencedLibraries();

			await RefreshServices();
			UsamLogger.Log($"There are {ServiceDefinitions.Count} Service definitions");

			SetSolution();
			UsamLogger.Log("Solution set done");

			const string updatedServicesKey = "BeamUpdatedServices";
			if (!SessionState.GetBool(updatedServicesKey, false))
			{
				UsamLogger.Log("Update services version start");
				await UpdateServicesVersions();
				SessionState.SetBool(updatedServicesKey, true);
				UsamLogger.Log("Update services version end");
			}

			var _ = CheckMicroserviceStatus();
			ConnectToLogs();

			var oldServices = GetAllOldServices();
			if (oldServices.Count > 0)
			{
				var migrationVisualElement = new MigrationConfirmationVisualElement(oldServices);
				var popup = BeamablePopupWindow.ShowUtility(Constants.Migration.MIGRATION_POPUP_NAME, migrationVisualElement, null,
															new Vector2(800, 400), (window) =>
														   {
																// trigger after Unity domain reload
																window.Close();
														   });
				migrationVisualElement.OnCancelled += popup.Close;
				migrationVisualElement.OnClosed += popup.Close;
			}


			UsamLogger.Log("Completed");
			UsamLogger.StopLogTimer();
		}

		public async Promise Migrate(List<IDescriptor> allDescriptors, Action<float, string> updateCallback, CancellationToken token)
		{
			List<string> pathsToDelete = new List<string>();

			AssetDatabase.DisallowAutoRefresh();

			List<IDescriptor> microServices = allDescriptors.Where(dc => dc.ServiceType == ServiceType.MicroService).ToList();
			List<IDescriptor> storages = allDescriptors.Where(dc => dc.ServiceType == ServiceType.StorageObject).ToList();

			float progress = 0f;
			float increment = 100f / (microServices.Count + storages.Count * 2 + 1);

			var microPromises = new List<Promise<Unit>>();
			foreach (IDescriptor descriptor in microServices)
			{
				if (token.IsCancellationRequested)
				{
					UsamLogger.Log(BEAMABLE_MIGRATION_CANCELLATION_LOG);
					return;
				}

				MicroserviceDescriptor serviceDesc = (MicroserviceDescriptor)descriptor;
				pathsToDelete.Add(serviceDesc.SourcePath);
				microPromises.Add(MigrateMicroservice(serviceDesc, (message, hasProgress) =>
			   {
				   updateCallback(progress, message);
				   progress += increment * hasProgress;
			   }));
			}

			var microSequence = Promise.Sequence(microPromises);
			await microSequence;

			var storagePromises = new List<Promise<Unit>>();
			foreach (IDescriptor descriptor in storages)
			{
				if (token.IsCancellationRequested)
				{
					UsamLogger.Log(BEAMABLE_MIGRATION_CANCELLATION_LOG);
					return;
				}

				pathsToDelete.Add(Path.GetDirectoryName(descriptor.AttributePath));
				storagePromises.Add(MigrateStorage((StorageObjectDescriptor)descriptor, (message, hasProgress) =>
			   {
				   updateCallback(progress, message);
				   progress += increment * hasProgress;
			   }));
			}

			var storageSequence = Promise.Sequence(storagePromises);
			await storageSequence;

			updateCallback(progress, "Deleting old services");

			if (token.IsCancellationRequested)
			{
				UsamLogger.Log(BEAMABLE_MIGRATION_CANCELLATION_LOG);
				return;
			}

			// REMOVE OLD STUFF
			pathsToDelete.Add("Assets/Beamable/Microservices");
			pathsToDelete.Add("Assets/Beamable/StorageObjects");

			foreach (string path in pathsToDelete)
			{
				if (Directory.Exists(path))
				{
					FileUtils.DeleteDirectoryRecursively(path);
					File.Delete(path + ".meta");
				}
			}

			AssetDatabase.AllowAutoRefresh();
			updateCallback(100f, "Completed");
			EditorUtility.RequestScriptReload();
			await Init();
		}

		private async Promise MigrateStorage(StorageObjectDescriptor storageDescriptor, Action<string, int> updateProgress)
		{
			var storageName = storageDescriptor.Name;
			var path = $"{StandaloneMicroservicesPath}{storageName}/";

			updateProgress?.Invoke($"Reading dependencies of storage: {storageName}", 0);
			var depsNames = MigrationHelper.GetDependentServices(storageName);
			var deps = depsNames.Select(dp => ServiceDefinitions.FirstOrDefault(sd => sd.BeamoId == dp)).ToList();

			//If folder exists, then there was another attempt of migration and we need to restart it
			if (Directory.Exists(path))
			{
				FileUtils.DeleteDirectoryRecursively(path);
			}

			updateProgress?.Invoke($"Creating storage: {storageName}", 1);
			await CreateStorage(storageName, deps, shouldInitialize: false);

			var definition = ServiceDefinitions.FirstOrDefault(s => s.BeamoId.Equals(storageName));
			if (definition == null)
			{
				throw new Exception($"Storage: {storageName} was not found in local files");
			}

			var newExtensionsFile = Path.Combine(definition.ServiceInfo.projectPath, "StorageExtensions.cs");
			if (File.Exists(newExtensionsFile))
			{
				File.Delete(newExtensionsFile); // Delete this file because in old storages the extensions class was already inside the storage main file
			}

			var oldDir = Path.GetDirectoryName(storageDescriptor.AttributePath);

			if (string.IsNullOrEmpty(oldDir))
			{
				throw new Exception($"Old location of the storage: {storageName} was not found");
			}

			updateProgress?.Invoke($"Copying files of: {storageName}", 1);
			foreach (var file in Directory.EnumerateFiles(oldDir))
			{
				if (!Path.GetExtension(file).EndsWith("cs")) continue;
				var fileName = Path.GetFileName(file);
				var newFilePath = Path.Combine(definition.ServiceInfo.projectPath, fileName);
				if (File.Exists(newFilePath))
				{
					File.Delete(newFilePath);
				}
				File.Copy(file, newFilePath);
			}
		}

		private async Promise MigrateMicroservice(MicroserviceDescriptor microserviceDescriptor, Action<string, int> updateProgress)
		{
			var microserviceDir = microserviceDescriptor.SourcePath;
			var microserviceName = microserviceDescriptor.Name;
			var path = $"{StandaloneMicroservicesPath}{microserviceName}/";

			//If folder exists, then there was another attempt of migration and we need to restart it
			if (Directory.Exists(path))
			{
				FileUtils.DeleteDirectoryRecursively(path);
			}

			UsamLogger.Log($"Migrating {microserviceName} start");
			updateProgress?.Invoke($"Creating service {microserviceName}", 0);
			var references = GetAssemblyDefinitionAssets(microserviceDescriptor);
			await CreateMicroService(microserviceName, null, assemblyReferences: references, shouldInitialize: false);

			await RefreshServices();

			var definition = ServiceDefinitions.FirstOrDefault(s => s.BeamoId.Equals(microserviceName));
			if (definition == null)
			{
				throw new Exception($"Microservice: {microserviceName} was not found. Something wrong happened while creating it.");
			}

			updateProgress?.Invoke($"Copying files of {microserviceName}", 1);
			foreach (var file in Directory.EnumerateFiles(microserviceDir))
			{
				if (!Path.GetExtension(file).EndsWith("cs")) continue;
				var fileName = Path.GetFileName(file);
				var newFilePath = Path.Combine(definition.ServiceInfo.projectPath, fileName);
				if (File.Exists(newFilePath))
				{
					File.Delete(newFilePath);
				}
				File.Copy(file, newFilePath);

				var fileContent = File.ReadAllText(newFilePath);
				fileContent = fileContent.Replace("namespace Beamable.Microservices",
												  $"namespace Beamable.{microserviceName}");
				File.WriteAllText(newFilePath, fileContent);
			}
		}

		private static List<IDescriptor> GetAllOldServices()
		{
			List<string> servicesToIgnore = new List<string>() { "CacheDependentMS" };
			List<IDescriptor> allDescriptors = new List<IDescriptor>();
			var serviceRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
			if (serviceRegistry != null)
			{
				foreach (var descriptor in serviceRegistry.AllDescriptors)
				{
					if (servicesToIgnore.Contains(descriptor.Name))
					{
						continue;
					}

					//Check if this was already migrated
					//Right now this is not required, because we maintain all services and storages inside a hidden folder from Unity
					//However, in the future with services being able to be created anywhere, this will be necessary
					/*if (descriptor.ServiceType == ServiceType.MicroService)
					{
						var services = GetBeamServices();
						var service = services.FirstOrDefault(s => s.name.Equals(descriptor.Name));
						if (service != null)
						{
							continue;
						}
					}
					else
					{
						var storages = GetBeamStorages();
						var storage = storages.FirstOrDefault(s => s.name.Equals(descriptor.Name));
						if (storage != null)
						{
							continue;
						}
					}*/

					allDescriptors.Add(descriptor);
				}
			}

			return allDescriptors;
		}

		public static List<AssemblyDefinitionAsset> GetAssemblyDefinitionAssets(MicroserviceDescriptor descriptor)
		{
			List<AssemblyDefinitionAsset> assets = new List<AssemblyDefinitionAsset>();
			List<string> mandatoryReferences = new List<string>() { "Unity.Beamable.Customer.Common" }; // Add the customer common asmdef even if it's not being used

			var dependencies = descriptor.Type.Assembly.GetReferencedAssemblies().Select(r => r.Name).ToList();
			dependencies.AddRange(mandatoryReferences);
			foreach (var name in dependencies)
			{
				if (CsharpProjectUtil.IsValidReference(name))
				{
					var guid = AssetDatabase.FindAssets($"t:AssemblyDefinitionAsset {name}");

					if (guid.Length == 0)
					{
						continue; //there is no asset of this assembly to reference
					}

					if (guid.Length > 1)
					{
						throw new Exception($"Found more than one assembly definition with the name: {name}");
					}

					var path = AssetDatabase.GUIDToAssetPath(guid[0]);

					if (string.IsNullOrEmpty(path)) continue;

					var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
					if (asset != null && asset.name.Equals(name)) assets.Add(asset);
				}
			}

			return assets;
		}

		private async Promise SaveReferencedLibraries()
		{
			List<BeamDependencyData> allDependencies = new List<BeamDependencyData>();

			var command = _cli.ProjectDepsList(new ProjectDepsListArgs()
			{
				nonBeamo = true
			}).OnStreamListDepsCommandResults(cb =>
			{
				foreach (var serviceDependenciesPair in cb.data.Services)
				{
					allDependencies.AddRange(serviceDependenciesPair.dependencies);
				}
			});
			await command.Run();

			Libraries = allDependencies.Distinct().ToList();
		}

		public async Promise UpdateServicesVersions()
		{
			var nugetVersion = GetCurrentNugetVersion();
			var versions = _cli.ProjectVersion(new ProjectVersionArgs { requestedVersion = nugetVersion });
			versions.OnStreamProjectVersionCommandResult(result =>
			{
				UsamLogger.Log($"Versions updated: {string.Join(",", result.data.packageVersions)}");
			});
			await versions.Run().Error(ex => UsamLogger.Log(ex));
		}

		public async Promise RefreshServices()
		{
			ServiceDefinitions.Clear();
			Promise finishedPopulatingServices = new Promise();

			try
			{
				UsamLogger.Log("refresh services from CLI start");
				//Get remote only information from the CLI
				var psRemote = _cli.ServicesPs(new ServicesPsArgs() { json = false });
				psRemote.OnStreamServiceListResult(cb =>
				{
					IsDockerRunning = cb.data.IsDockerRunning;
					UsamLogger.Log($"Found {cb.data.BeamoIds.Count} services");
					PopulateDataWithRemote(cb.data);
					finishedPopulatingServices.CompleteSuccess();
				});
				await psRemote.Run();
				UsamLogger.Log("refresh services from CLI end");
			}
			catch
			{
				IsDockerRunning = false;
				UsamLogger.Log("ERROR: Could not list services, skip", true);
				return;
			}

			await finishedPopulatingServices;
		}

		private void PopulateDataWithRemote(BeamServiceListResult objData)
		{
			for (int i = 0; i < objData.BeamoIds.Count; i++)
			{
				if (!objData.ExistInLocal[i] && !objData.IsRunningRemotely[i]) //In this case the service was disabled in the remote, which means that it's irrelevant.
				{
					continue;
				}

				var name = objData.BeamoIds[i];
				UsamLogger.Log($"Handling {name} started");

				var runningState = objData.IsRunningRemotely[i] ? BeamoServiceStatus.Running : BeamoServiceStatus.NotRunning;
				var type = objData.ProtocolTypes[i].Equals("HttpMicroservice")
					? ServiceType.MicroService
					: ServiceType.StorageObject;

				string assetProjectPath = objData.ProjectPath[i];


				AddServiceDefinition(name, type, assetProjectPath, runningState,
									 objData.ShouldBeEnabledOnRemote[i], objData.ExistInLocal[i], objData.Dependencies[i]);
				UsamLogger.Log($"Handling {name} ended");
			}
		}

		private void AddServiceDefinition(string name, ServiceType type, string assetProjectPath, BeamoServiceStatus status = BeamoServiceStatus.Unknown,
										  bool shouldBeEnableOnRemote = true, bool hasLocalSource = true, string dependencies = null)
		{
			List<string> depsList = dependencies?.Split(',').ToList();

			if (depsList == null)
			{
				depsList = new List<string>();
			}

			depsList = depsList.Where(dep => !string.IsNullOrEmpty(dep)).ToList();

			var dataIndex =
				ServiceDefinitions.FindIndex(definition => definition.BeamoId.Equals(name));
			if (dataIndex < 0)
			{
				ServiceDefinitions.Add(new BeamoServiceDefinition
				{
					ServiceType = type,
					ServiceInfo = new ServiceInfo()
					{
						name = name,
						projectPath = assetProjectPath,
					}
				});
				dataIndex = ServiceDefinitions.Count - 1;
				ServiceDefinitions[dataIndex].Builder = new BeamoServiceBuilder() { BeamoId = name };

			}
			ServiceDefinitions[dataIndex].HasLocalSource = hasLocalSource;
			ServiceDefinitions[dataIndex].ShouldBeEnabledOnRemote = shouldBeEnableOnRemote;
			ServiceDefinitions[dataIndex].IsRunningOnRemote = status;
			ServiceDefinitions[dataIndex].Dependencies = depsList;
		}


		/// <summary>
		/// Regenerates the files: Program.cs, Dockerfile and .csproj. Then copy these files
		/// to the desired Standalone Microservice.
		/// </summary>
		/// <param name="signPost">The signpost asset that references to the project in which wants to regenerate the files.</param>
		public async Promise RegenerateProjectFiles(IBeamoServiceDefinition definition)
		{
			var tempPath = $"Temp/{definition.BeamoId}";
			var projName = new ServiceName(definition.BeamoId);
			var projPath = definition.ServiceInfo.projectPath;

			var args = new ProjectRegenerateArgs() { name = projName, output = tempPath, copyPath = projPath };
			var command = _cli.ProjectRegenerate(args);
			await command.Run();
		}

		/// <summary>
		/// Creates a new Standalone Service or storage inside a hidden folder from Unity.
		/// </summary>
		/// <param name="name"> The name of the Service/Storage to be created.</param>
		/// <param name="type"> The type of the Service/Storage to be created.</param>
		/// <param name="additionalReferences">A list with all references to link to this service.</param>
		public async Promise CreateService(string name, ServiceType type, List<IBeamoServiceDefinition> additionalReferences)
		{
			UsamLogger.Log($"Starting creation of {name}");

			switch (type)
			{
				case ServiceType.MicroService:
					await CreateMicroService(name, additionalReferences);
					break;
				case ServiceType.StorageObject:
					await CreateStorage(name, additionalReferences);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}

		/*public async Promise UpdateServiceReferences(BeamServiceSignpost signpost)
		{
			var serviceName = signpost.name;
			
			SolutionPostProcessor.OnPreGeneratingCSProjectFiles();
			SetSolution();
			UsamLogger.Log($"Starting updating references");
			//get a list of all references of that service
			var service = _services.FirstOrDefault(s => s.name == serviceName);
			if (service == null)
			{
				throw new ArgumentException($"Invalid service name was passed: {serviceName}");
			}

			UsamLogger.Log($"Reading all references from service: {serviceName}");
			var results = await _dotnetService.Run($"list {service.CsprojPath} reference");

			//filter that list with only the generated projs
			var depsPaths = results.Select(s => s.Replace(Environment.NewLine, string.Empty)).Where(line => line.EndsWith(".csproj")).ToList();
			var correctedPaths = depsPaths.Select(path => path.Replace("\\", "/")).ToList();
			var existinReferences = correctedPaths.Where(path => path.Contains(CsharpProjectUtil.PROJECT_NAME_PREFIX)).ToList();


			//remove all generated projs references
			UsamLogger.Log($"Removing all references from service: {serviceName}");
			var promises = new List<Promise<List<string>>>();
			foreach (string reference in existinReferences)
			{
				var referenceName = Path.GetFileNameWithoutExtension(reference).Replace(CsharpProjectUtil.PROJECT_NAME_PREFIX, String.Empty);
				var refPathToRemove = CsharpProjectUtil.GenerateCsharpProjectFilename(referenceName);
				UsamLogger.Log($"Removing reference: {refPathToRemove}");
				Promise<List<string>> p =_dotnetService.Run($"remove {service.CsprojPath} reference {refPathToRemove}");
				promises.Add(p);
			}

			Promise<List<List<string>>> sequence = Promise.Sequence(promises);
			await sequence;

			//add all the references
			foreach (var newRefs in signpost.assemblyReferences)
			{
				var newRefCsprojPath = CsharpProjectUtil.GenerateCsharpProjectFilename(newRefs.name);
				if (!File.Exists(newRefCsprojPath))
				{
					UsamLogger.Log($"The project file for reference {newRefs} does not exist yet");
					continue;
				}
				UsamLogger.Log($"Adding the reference: {newRefs}");
				await _dotnetService.Run($"add {service.CsprojPath} reference {newRefCsprojPath}");
			}

			UsamLogger.Log($"Finished updating references");
		}*/


		public Promise RunStandaloneMicroservice(string id)
		{
			var runCommand = _cli.ProjectRun(new ProjectRunArgs() { ids = new[] { id }, watch = true }).OnError(ex =>
				{
					Debug.LogError(ex.data.message);
				});
			return runCommand.Run();
		}

		/// <summary>
		/// Build the USAM and generates the client code in the Beamable/Autogenerated folder.
		/// </summary>
		/// <param name="id">The id of the Standalone Microservice.</param>
		public async Promise GenerateClientCode(string id)
		{
			UsamLogger.Log($"Start generating client code for service: {id}");


			var service = ServiceDefinitions.FirstOrDefault(s => s.BeamoId == id);
			if (service == null)
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(service.ServiceInfo.projectPath))
			{
				UsamLogger.Log("No file to generate");
				return;
			}

			var beamPath = BeamCliUtil.CLI_PATH.Replace(".dll", "");
			var buildCommand = $"build \"{service.ServiceInfo.projectPath}\" /p:BeamableTool={beamPath} /p:GenerateClientCode=false";

			UsamLogger.Log($"Starting build service: {id} using command: {buildCommand}");
			await _dotnetService.Run(buildCommand);

			UsamLogger.Log($"Starting beam client code generator");

			string dllPath = $"{service.ServiceInfo.projectPath}/{MICROSERVICE_DLL_PATH}/{id}.dll";
			string outputPath = Constants.Features.Services.AUTOGENERATED_CLIENT_PATH;

			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
			}

			var generateClientArgs = new ProjectGenerateClientArgs() { source = dllPath, outputDir = outputPath, outputLinks = false };

			ProjectGenerateClientWrapper command = _cli.ProjectGenerateClient(generateClientArgs);
			await command.Run();

			UsamLogger.Log($"Finished generating client code for service: {id}");
		}

		/// <summary>
		/// Get the information of if this service is local or not.
		/// </summary>
		/// <param name="serviceName">The name of the service</param>
		/// <returns>Returns true if the service is local or false if not.</returns>
		public bool GetServiceIsLocal(string serviceName)
		{
			foreach (IBeamoServiceDefinition service in ServiceDefinitions)
			{
				if (service.BeamoId.Equals(serviceName))
				{
					return service.HasLocalSource;
				}
			}

			var allServices = String.Join(", ", ServiceDefinitions.Select(s => s.BeamoId));
			throw new EntryPointNotFoundException(
				$"The service {serviceName} was not found! The current list of services is: {allServices}");
		}

		/// <summary>
		/// Get if this service is published already or not
		/// </summary>
		/// <param name="serviceName">The name of the service</param>
		/// <returns>Returns true if the service was published and false if not</returns>
		public bool GetServiceIsRemote(string serviceName)
		{
			foreach (var service in ServiceDefinitions.Where(service => service.BeamoId.Equals(serviceName)))
			{
				return service.IsRunningOnRemote == BeamoServiceStatus.Running;
			}

			var allServices = String.Join(", ", ServiceDefinitions.Select(s => s.BeamoId));
			throw new EntryPointNotFoundException(
				$"The service {serviceName} was not found! The current list of services is: {allServices}");
		}

		/// <summary>
		/// Get if the service should or not be enable on remote.
		/// </summary>
		/// <param name="serviceName">The name of the service</param>
		/// <returns>True if should be enable and false if not.</returns>
		public bool GetServiceShouldBeEnable(string serviceName)
		{
			foreach (IBeamoServiceDefinition service in ServiceDefinitions)
			{
				if (service.BeamoId.Equals(serviceName))
				{
					return service.ShouldBeEnabledOnRemote;
				}
			}

			var allServices = String.Join(", ", ServiceDefinitions.Select(s => s.BeamoId));
			throw new EntryPointNotFoundException(
				$"The service {serviceName} was not found! The current list of services is: {allServices}");
		}

		public void ConnectToLogs()
		{
			// TODO: re-attach if process dies
			foreach (IBeamoServiceDefinition definition in ServiceDefinitions)
			{
				var serviceId = definition.BeamoId;
				var logs = _cli.ProjectLogs(new ProjectLogsArgs
				{
					service = new ServiceName(definition.BeamoId),
					reconnect = true
				});
				logs.OnStreamTailLogMessageForClient(point =>
				{


					UsamLogger.Log("Log: " + point.data.message);
					_dispatcher.Schedule(() => OnLogMessage?.Invoke(definition.BeamoId, point.data));
				});
				_logsCommands.Add(logs.Run());
			}
		}

		public async Promise CheckMicroserviceStatus()
		{
			var projectPs = _cli.ProjectPs(new ProjectPsArgs()
			{
				watch = true
			}).OnStreamServiceDiscoveryEvent(cb =>
			{
				Debug.Log($"[{cb.data.service}] is running = {cb.data.isRunning}");

				var def = ServiceDefinitions.FirstOrDefault(d => d.BeamoId.Equals(cb.data.service));
				if (def != null)
				{
					// def.IsRunningLocally = cb.data.isRunning;
					def.Builder.IsRunning = cb.data.isRunning;
				}

			}).OnError(cb =>
			{
				Debug.LogError($"Error occured while listening for Microservice status updates. Message=[{cb.data.message}] CliStack=[{cb.data.stackTrace}]");
			});

			try
			{
				await projectPs.Run();
			}
			catch (Exception ex)
			{
				Debug.LogError($"Restarting Microservice listening process. Message=[{ex.Message}]");
				// this command needs to retry and retry and retry. It should always be running.
				_dispatcher.Schedule(() =>
				{
					var _ = CheckMicroserviceStatus();
				});
			}
		}


		/// <summary>
		/// Update the sln file to add references to known beam services.
		/// This may cause a script reload if the sln file needs to regenerate
		/// </summary>
		/// <param name="services"></param>
		public static void SetSolution()
		{
			// find the local sln file
			var slnPath = FindFirstSolutionFile();
			if (string.IsNullOrEmpty(slnPath) || !File.Exists(slnPath))
			{
				UsamLogger.Log("Beam. No script file, so reloading scripts");
				UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
				return; // once scripts reload, the current invocation of scripts end.
			}

			var contents = File.ReadAllText(slnPath);

			var generatedContent = SolutionPostProcessor.OnGeneratedSlnSolution(slnPath, contents);
			var areDifferent =
				generatedContent !=
				contents; // TODO: is there a better way to check if the solution file needs to be regenerated? This feels like it could become a bottleneck.
			if (areDifferent)
			{
				// force the sln file to be re-generated, by deleting it. // TODO: we'll need to "unlock" the file in certain VCS
				File.Delete(slnPath);
				UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
			}
		}

		private static string FindFirstSolutionFile()
		{
			var files = Directory.GetFiles(".");
			foreach (var file in files)
			{
				if (Path.GetExtension(file) == ".sln")
				{
					return file;
				}
			}

			return null;
		}

		private async Promise SetPropertiesFile()
		{
			var beamPath = BeamCliUtil.CLI_PATH.Replace(".dll", "");
			var workingDir = Path.GetDirectoryName(Directory.GetCurrentDirectory());
			if (beamPath.StartsWith(workingDir))
			{
				// when this case happens, we are developing locally, so put in a reference locally.
				beamPath = "BEAM_SOLUTION_DIR/../cli/cli/bin/Debug/net8.0/Beamable.Tools";
			}
			else
			{
				beamPath = "BEAM_SOLUTION_DIR/" + beamPath;
			}
			var command = _cli.ProjectGenerateProperties(new ProjectGeneratePropertiesArgs()
			{
				output = ".",
				beamPath = beamPath,
				solutionDir = CliConstants.GENERATE_PROPS_SLN_NEXT_TO_PROPS,
				buildDir = "/Temp/beam/USAMBuilds"
			});


			await command.Run();

		}

		/// <summary>
		/// Update all service definitions with new enable state from editor window.
		/// </summary>
		/// <param name="allServices">All models of services that are not archived.</param>
		public void UpdateServicesEnableState(List<IEntryModel> allServices)
		{
			foreach (IEntryModel service in allServices)
			{
				IBeamoServiceDefinition localDefinition = ServiceDefinitions.Find((s) => s.BeamoId == service.Name);
				if (localDefinition != null)
				{
					localDefinition.ShouldBeEnabledOnRemote = service.Enabled;
				}
			}
		}

		private async Promise CreateStorage(string storageName, List<IBeamoServiceDefinition> additionalReferences, bool shouldInitialize = true)
		{
			var service = new ServiceName(storageName);
			var slnPath = FindFirstSolutionFile();
			var relativePath = Path.Combine(StandaloneMicroservicesPath, storageName);
			if (Directory.Exists(relativePath))
			{
				UsamLogger.Log($"{storageName} already exists!");
				return;
			}

			additionalReferences ??= new List<IBeamoServiceDefinition>();
			string[] deps = new string[additionalReferences.Count];
			for (int i = 0; i < additionalReferences.Count; i++)
			{
				deps[i] = additionalReferences[i].BeamoId;
			}

			var storageArgs = new ProjectNewStorageArgs
			{
				name = service,
				serviceDirectory = StandaloneMicroservicesPath,
				sln = slnPath,
				version = GetCurrentNugetVersion(),
				linkTo = deps,
			};
			var storageCommand = _cli.ProjectNewStorage(storageArgs);
			await storageCommand.Run();

			UsamLogger.Log($"Starting the initialization of CodeService");
			// Re-initializing the CodeService to make sure all files are with the right information
			if (shouldInitialize)
			{
				await Init();
			}

			UsamLogger.Log($"Finished creation of storage {storageName}");
		}

		/// <summary>
		/// Given the current SDK version, we should be able to figure out which version of Nuget packages we need.
		/// Note: if we're developing the SDK, the version will be 0.0.0, and the current local version of Nuget package is 0.0.123-local.
		/// </summary>
		/// <returns></returns>
		public static string GetCurrentNugetVersion()
		{
			var version = BeamableEnvironment.NugetPackageVersion;
			return version;
		}

		private async Promise CreateMicroService(string serviceName, List<IBeamoServiceDefinition> dependencies, List<AssemblyDefinitionAsset> assemblyReferences = null, bool shouldInitialize = true)
		{
			var service = new ServiceName(serviceName);
			var slnPath = FindFirstSolutionFile();
			var fullPath = Path.Combine(StandaloneMicroservicesPath, serviceName);
			if (Directory.Exists(fullPath))
			{
				UsamLogger.Log($"{serviceName} already exists!");
				return;
			}

			var args = new ProjectNewServiceArgs()
			{
				name = service,
				serviceDirectory = StandaloneMicroservicesPath,
				sln = slnPath,
				version = GetCurrentNugetVersion()
			};
			var command = _cli.ProjectNewService(args);
			await command.Run();

			UsamLogger.Log($"Starting the initialization of CodeService");
			// Re-initializing the CodeService to make sure all files are with the right information
			if (shouldInitialize)
			{
				await Init();
			}

			UsamLogger.Log($"Finished creation of service {serviceName}");
		}

		public Promise StopStandaloneMicroservice(string beamoId)
		{
			return StopStandaloneMicroservice(new string[] { beamoId });
		}

		public async Promise StopStandaloneMicroservice(IEnumerable<string> beamoIds)
		{
			var stop = _cli.ProjectStop(new ProjectStopArgs() { ids = beamoIds.ToArray() });
			await stop.Run();
		}

		public async Promise OpenSwagger(string beamoId, bool remote = false)
		{
			try
			{
				var cmd = _cli.ProjectOpenSwagger(
					new ProjectOpenSwaggerArgs() { remote = remote, serviceName = new ServiceName(beamoId) });
				await cmd.Run();
			}
			catch (Exception e)
			{
				UsamLogger.Log(e.GetType().Name, e.Message, e.StackTrace);
			}
		}

		public void OpenMicroserviceFile(string serviceName)
		{
			IBeamoServiceDefinition def = ServiceDefinitions.FirstOrDefault(d => d.BeamoId.Equals(serviceName));

			if (def == null)
			{
				UsamLogger.Log("Service does not exist!");
				return;
			}
			var fileName = $@"{def.ServiceInfo.projectPath}/{serviceName}.cs";
			EditorUtility.OpenWithDefaultApp(fileName);
		}
	}
}
