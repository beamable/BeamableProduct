using Beamable.Api.CloudSaving;
using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
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

		private static readonly List<string> IgnoreFolderSuffixes = new List<string> { "~", "obj", "bin" };
		private List<BeamServiceSignpost> _services;
		private List<BeamStorageSignpost> _storages;
		private List<Promise> _logsCommands = new List<Promise>();

		private const string BEAMABLE_PATH = "Assets/Beamable/";
		private const string BEAMABLE_LIB_PATH = "Library/BeamableEditor";
		private const string MICROSERVICE_DLL_PATH = "bin/Debug/net6.0"; // is this true for all platforms and dotnet installations?
		public static readonly string StandaloneMicroservicesFolderName = "StandaloneMicroservices~/";
		private static readonly string StandaloneMicroservicesPath = $"{BEAMABLE_PATH}{StandaloneMicroservicesFolderName}";
		public static readonly string LibrariesPathsFilePath = $"{BEAMABLE_LIB_PATH}/.libraries_paths";
		public static string LibrariesPathsDirectory => Path.GetDirectoryName(LibrariesPathsFilePath);

		public CodeService(BeamCommands cli, BeamableDispatcher dispatcher, DotnetService dotnetService)
		{
			_cli = cli;
			_dispatcher = dispatcher;
			_dotnetService = dotnetService;
			OnReady = Init();
		}

		[Conditional("BEAM_CODE_SERVICE_LOGS"), Conditional("BEAMABLE_DEVELOPER")]
		static void LogVerbose(string log, bool isError = false)
		{
			const string logFormat = "<b>[" + nameof(CodeService) + "]</b> {0}";
			var text = string.Format(logFormat, log);
			if (isError)
				BeamEditorContext.Default.Dispatcher.Schedule(() => Debug.LogError(text));
			else
				BeamEditorContext.Default.Dispatcher.Schedule(() => Debug.Log(text));
		}

		static void LogExceptionVerbose(Exception e) => LogVerbose(e.ToString(), true);

		public async Promise Init()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;
			LogVerbose("Running init");
			_services = GetBeamServices();
			LogVerbose("Have services");
			_storages = GetBeamStorages();
			LogVerbose("Have storages");
			// TODO: we need validation. What happens if the .beamservice files point to non-existent files
			SetSolution(_services, _storages);
			LogVerbose("Solution set done");

			LogVerbose("Setting properties file");
			await SetPropertiesFile();
			
			LogVerbose("Set manifest start");
			await SetManifest(_cli, _services, _storages);//asd
			LogVerbose("set manifest ended");

			LogVerbose("Saving all libraries referenced by services");
			await SaveReferencedLibraries();

			await RefreshServices();
			LogVerbose($"There are {ServiceDefinitions.Count} Service definitions");
			const string updatedServicesKey = "BeamUpdatedServices";
			if (!SessionState.GetBool(updatedServicesKey, false))
			{
				LogVerbose("Update services version start");
				await UpdateServicesVersions();
				SessionState.SetBool(updatedServicesKey, true);
				LogVerbose("Update services version end");
			}

			CheckMicroserviceStatus();
			ConnectToLogs();

			var oldServices = GetAllOldServices();
			if (oldServices.Count > 0)
			{
				var migrationVisualElement = new MigrationConfirmationVisualElement(oldServices);
				var popup = BeamablePopupWindow.ShowUtility(Constants.Migration.MIGRATION_POPUP_NAME, migrationVisualElement, null,
				                                            new Vector2(800, 400),  (window) =>
				                                            {
					                                            // trigger after Unity domain reload
					                                            window.Close();
				                                            });
				migrationVisualElement.OnCancelled += popup.Close;
				migrationVisualElement.OnClosed += popup.Close;
			}
			
			
			LogVerbose("Completed");
		}

		public async Promise Migrate(List<IDescriptor> allDescriptors)
		{
			AssetDatabase.DisallowAutoRefresh();

			foreach (IDescriptor descriptor in allDescriptors)
			{
				if (descriptor.ServiceType == ServiceType.MicroService)
				{
					await MigrateMicroservice((MicroserviceDescriptor)descriptor);
				}
				else
				{
					await MigrateStorage((StorageObjectDescriptor)descriptor);
				}
			}
			
			// REMOVE OLD STUFF
			var microPath = "Assets/Beamable/Microservices";
			var storagePath = "Assets/Beamable/StorageObjects";
			if (Directory.Exists(microPath))
			{
				Directory.Delete(microPath, true);
			}
			
			if (Directory.Exists(storagePath))
			{
				Directory.Delete(storagePath, true);
			}
			
			AssetDatabase.AllowAutoRefresh();
			await Init();
		}

		private async Promise MigrateStorage(StorageObjectDescriptor storageDescriptor)
		{
			var storageName = storageDescriptor.Name;
			var path = $"{StandaloneMicroservicesPath}{storageName}/";
			var storageModel = MicroservicesDataModel.Instance.Storages.FirstOrDefault(s => s.Name == storageName);
			var deps = MicroservicesDataModel.Instance.Services
			                                 .Where(model => model.Dependencies.Any(s => s.Name == storageName)).Select(model => ServiceDefinitions.FirstOrDefault(d => d.BeamoId == model.Name))
			                                 .ToList();
			Debug.Log(storageModel);
			Debug.Log(string.Join(", ", deps));
			if (!Directory.Exists(path))
			{
				Debug.Log(storageName);	
				await CreateStorage(storageName, deps, shouldInitialize: false);
			}

			var dirToDelete = Directory.GetDirectoryRoot(storageDescriptor.AttributePath);
			Directory.Delete(dirToDelete, true);
		}

		private async Promise MigrateMicroservice(MicroserviceDescriptor microserviceDescriptor)
		{
			var microserviceDir = microserviceDescriptor.SourcePath;
			var microserviceName = microserviceDescriptor.Name;
			var path = $"{StandaloneMicroservicesPath}{microserviceName}/";
			if (!Directory.Exists(path))
			{
				LogVerbose($"Migrating {microserviceName} start");
				var references = GetAssemblyDefinitionAssets(microserviceDescriptor);
				await CreateMicroService(microserviceName, null, assemblyReferences: references, shouldInitialize: false);
			}

			_services = GetBeamServices();
			var signpost = _services.FirstOrDefault(s => s.name.Equals(microserviceName));
			if (signpost == null) return;

			foreach (var file in Directory.EnumerateFiles(microserviceDir))
			{
				if (!Path.GetExtension(file).EndsWith("cs")) continue;
				var fileName = Path.GetFileName(file);
				var newFilePath = Path.Combine(signpost.CsprojPath, fileName);
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
			Directory.Delete(microserviceDir, true);
		}
		
		private static List<IDescriptor> GetAllOldServices()
		{
			List<string> servicesToIgnore = new List<string>() {"CacheDependentMS"};
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
					if (descriptor.ServiceType == ServiceType.MicroService)
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
					}
					
					allDescriptors.Add(descriptor);
				}
			}

			return allDescriptors;
		}

		public static List<AssemblyDefinitionAsset> GetAssemblyDefinitionAssets(MicroserviceDescriptor descriptor)
		{
			List<AssemblyDefinitionAsset> assets = new List<AssemblyDefinitionAsset>();
			List<string> mandatoryReferences = new List<string>() {"Unity.Beamable.Customer.Common"}; // Add the customer common asmdef even if it's not being used
			
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
					assets.Add(AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path));
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

			var librariesPaths = new LibrariesPaths() {libraries = allDependencies.Distinct().ToList()};

			var fileContent = JsonUtility.ToJson(librariesPaths);

			Directory.CreateDirectory(LibrariesPathsDirectory);
			File.WriteAllText(LibrariesPathsFilePath, fileContent);
		}

		[MenuItem("Gabriel/GetLibrariesPaths")]
		public static LibrariesPaths GetLibrariesPaths()
		{
			if (!File.Exists(LibrariesPathsFilePath))
			{
				return new LibrariesPaths();
			}
			
			var contents = File.ReadAllText(LibrariesPathsFilePath);

			return JsonUtility.FromJson<LibrariesPaths>(contents);
		}

		public async Promise UpdateServicesVersions()
		{
			var nugetVersion = GetCurrentNugetVersion();
			var versions = _cli.ProjectVersion(new ProjectVersionArgs { requestedVersion = nugetVersion });
			versions.OnStreamProjectVersionCommandResult(result =>
			{
				LogVerbose($"Versions updated: {string.Join(",", result.data.packageVersions)}");
			});
			await versions.Run().Error(LogExceptionVerbose);
		}

		public async Promise RefreshServices()
		{
			ServiceDefinitions.Clear();
			await CheckForDeletedServices();

			try
			{
				LogVerbose("refresh remote services from CLI start");
				//Get remote only information from the CLI
				var psRemote = _cli.ServicesPs(new ServicesPsArgs() { json = false, remote = true });
				psRemote.OnStreamServiceListResult(cb =>
				{
					IsDockerRunning = cb.data.IsDockerRunning;
					LogVerbose($"Found {cb.data.BeamoIds.Count} remote services");
					_dispatcher.Schedule(() => PopulateDataWithRemote(cb.data));
				});
				await psRemote.Run();
				LogVerbose("refresh remote services from CLI end");

				LogVerbose("refresh local services from CLI start");
				//Get local only information from the CLI
				var psLocal = _cli.ServicesPs(new ServicesPsArgs() { json = false, remote = false });
				psLocal.OnStreamServiceListResult(cb =>
				{
					IsDockerRunning = cb.data.IsDockerRunning;
					LogVerbose($"Found {cb.data.BeamoIds.Count} remote services");
					_dispatcher.Schedule(() => PopulateDataWithRemote(cb.data));
				});
				await psLocal.Run();
				LogVerbose("refresh local services from CLI end");
			}
			catch
			{
				IsDockerRunning = false;
				LogVerbose("Could not list remote services, skip", true);
				return;
			}



			LogVerbose("refresh local services start");

			PopulateDataWithLocal();

			LogVerbose("refresh local services end");
		}

		private void PopulateDataWithLocal()
		{
			_services = GetBeamServices();
			_storages = GetBeamStorages();

			for (int i = 0; i < _services.Count; i++)
			{
				AddServiceDefinition(_services[i].name, ServiceType.MicroService, _services[i].assetProjectPath, false);
			}

			for (int i = 0; i < _storages.Count; i++)
			{
				AddServiceDefinition(_storages[i].name, ServiceType.StorageObject, _storages[i].assetProjectPath, false);
			}
		}


		private void PopulateDataWithRemote(BeamServiceListResult objData)
		{
			for (int i = 0; i < objData.BeamoIds.Count; i++)
			{
				var name = objData.BeamoIds[i];
				LogVerbose($"Handling {name} started");

				var runningState = objData.RunningState[i] ? BeamoServiceStatus.Running : BeamoServiceStatus.NotRunning;
				var type = objData.ProtocolTypes[i].Equals("HttpMicroservice")
					? ServiceType.MicroService
					: ServiceType.StorageObject;

				var service = _services.FirstOrDefault(s => s.name == name);
				var storage = _storages.FirstOrDefault(s => s.name == name);

				string assetProjectPath = string.Empty;

				if (service != null)
				{
					assetProjectPath = service.assetProjectPath;
				}
				else if (storage != null)
				{
					assetProjectPath = storage.assetProjectPath;
				}

				AddServiceDefinition(name, type, assetProjectPath, true, runningState,
									 objData.ShouldBeEnabledOnRemote[i], objData.IsLocal, objData.Dependencies[i]);
				LogVerbose($"Handling {name} ended");
			}
		}

		private void AddServiceDefinition(string name, ServiceType type, string assetProjectPath, bool fromRemote, BeamoServiceStatus status = BeamoServiceStatus.Unknown,
										  bool shouldBeEnableOnRemote = true, bool hasLocalSource = true, string dependencies = null)
		{
			List<string> depsList = dependencies?.Split(',').ToList();

			if (depsList == null)
			{
				depsList = new List<string>();
			}

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
				ServiceDefinitions[dataIndex].HasLocalSource = hasLocalSource;
			}

			if (fromRemote)
			{
				ServiceDefinitions[dataIndex].ShouldBeEnabledOnRemote = shouldBeEnableOnRemote;
				ServiceDefinitions[dataIndex].IsRunningOnRemote = status;
				ServiceDefinitions[dataIndex].Dependencies = depsList;
			}
		}


		/// <summary>
		/// Regenerates the files: Program.cs, Dockerfile and .csproj. Then copy these files
		/// to the desired Standalone Microservice.
		/// </summary>
		/// <param name="signPost">The signpost asset that references to the project in which wants to regenerate the files.</param>
		public async Promise RegenerateProjectFiles(BeamServiceSignpost signPost)
		{
			var tempPath = $"Temp/{signPost.name}";
			var projName = new ServiceName(signPost.name);
			var projPath = signPost.CsprojPath;

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
			LogVerbose($"Starting creation of {name}");

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
		
		public async Promise UpdateServiceReferences(string serviceName, List<string> assemblyReferencesNames)
		{
			LogVerbose($"Starting updating references");
			//get a list of all references of that service
			var service = _services.FirstOrDefault(s => s.name == serviceName);
			if (service == null)
			{
				throw new ArgumentException($"Invalid service name was passed: {serviceName}");
			}

			LogVerbose($"Reading all references from service: {serviceName}");
			var results = await _dotnetService.Run($"list {service.CsprojPath} reference");

			//filter that list with only the generated projs
			var depsPaths = results.Select(s => s.Replace(Environment.NewLine, string.Empty)).Where(line => line.EndsWith(".csproj")).ToList();
			var correctedPaths = depsPaths.Select(path => path.Replace("\\", "/")).ToList();
			var existinReferences = correctedPaths.Where(path => path.Contains(CsharpProjectUtil.PROJECT_NAME_PREFIX)).ToList();


			//remove all generated projs references
			LogVerbose($"Removing all references from service: {serviceName}");
			var promises = new List<Promise<List<string>>>();
			foreach (string reference in existinReferences)
			{
				var referenceName = Path.GetFileNameWithoutExtension(reference).Replace(CsharpProjectUtil.PROJECT_NAME_PREFIX, String.Empty);
				var refPathToRemove = CsharpProjectUtil.GenerateCsharpProjectFilename(referenceName);
				LogVerbose($"Removing reference: {refPathToRemove}");
				Promise<List<string>> p =_dotnetService.Run($"remove {service.CsprojPath} reference {refPathToRemove}");
				promises.Add(p);
			}

			Promise<List<List<string>>> sequence = Promise.Sequence(promises);
			await sequence;

			//add all the references
			foreach (var newRefs in assemblyReferencesNames)
			{
				var newRefCsprojPath = CsharpProjectUtil.GenerateCsharpProjectFilename(newRefs);
				if (!File.Exists(newRefCsprojPath))
				{
					LogVerbose($"The project file for reference {newRefs} does not exist yet");
					continue;
				}
				LogVerbose($"Adding the reference: {newRefs}");
				await _dotnetService.Run($"add {service.CsprojPath} reference {newRefCsprojPath}");
			}

			LogVerbose($"Finished updating references");
		}


		public Promise RunStandaloneMicroservice(string id)
		{
			ProjectRunWrapper runCommand = _cli.ProjectRun(new ProjectRunArgs()
			{
				ids = new[] { id },
				watch = true
			});
			return runCommand.Run();
		}

		/// <summary>
		/// Build the USAM and generates the client code in the Beamable/Autogenerated folder.
		/// </summary>
		/// <param name="id">The id of the Standalone Microservice.</param>
		public async Promise GenerateClientCode(string id)
		{
			LogVerbose($"Start generating client code for service: {id}");


			var service = _services.FirstOrDefault(s => s.name == id);
			if (service == null)
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(service?.CsprojPath))
			{
				LogVerbose("No file to generate");
				return;
			}

			var beamPath = BeamCliUtil.CLI_PATH.Replace(".dll", "");
			var buildCommand = $"build \"{service.CsprojPath}\" /p:BeamableTool={beamPath} /p:GenerateClientCode=false";

			LogVerbose($"Starting build service: {id} using command: {buildCommand}");
			await _dotnetService.Run(buildCommand);

			LogVerbose($"Starting beam client code generator");

			string dllPath = $"{service.CsprojPath}/{MICROSERVICE_DLL_PATH}/{id}.dll";
			string outputPath = Constants.Features.Services.AUTOGENERATED_CLIENT_PATH;

			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
			}

			var generateClientArgs = new ProjectGenerateClientArgs() { source = dllPath, outputDir = outputPath, outputLinks = false };

			ProjectGenerateClientWrapper command = _cli.ProjectGenerateClient(generateClientArgs);
			await command.Run();

			LogVerbose($"Finished generating client code for service: {id}");
		}

		/// <summary>
		/// Get the information of if this service is local or remote.
		/// </summary>
		/// <param name="serviceName">The name of the service</param>
		/// <returns>Returns true if the service is local or false if remote.</returns>
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
			foreach (IBeamoServiceDefinition definition in ServiceDefinitions)
			{
				var logs = _cli.ProjectLogs(new ProjectLogsArgs
				{
					service = new ServiceName(definition.BeamoId),
					reconnect = true
				});
				logs.OnStreamTailLogMessageForClient(point =>
				{
					_dispatcher.Schedule(() => OnLogMessage?.Invoke(definition.BeamoId, point.data));
				});
				_logsCommands.Add(logs.Run());
			}
		}

		public void CheckMicroserviceStatus()
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
					def.Builder.IsRunning = cb.data.isRunning;
				}
			});
			projectPs.Run();
		}


		/// <summary>
		/// Update the sln file to add references to known beam services.
		/// This may cause a script reload if the sln file needs to regenerate
		/// </summary>
		/// <param name="services"></param>
		public static void SetSolution(List<BeamServiceSignpost> services, List<BeamStorageSignpost> storages)
		{
			// if there is nothing to add there is no need for an update
			if ((services == null || services.Count == 0) && (storages == null || storages.Count == 0))
				return;

			// find the local sln file
			var slnPath = FindFirstSolutionFile();
			if (string.IsNullOrEmpty(slnPath) || !File.Exists(slnPath))
			{
				LogVerbose("Beam. No script file, so reloading scripts");
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
				beamPath = "$(SolutionDir)../cli/cli/bin/Debug/net6.0/Beamable.Tools";
			}
			var command = _cli.ProjectGenerateProperties(new ProjectGeneratePropertiesArgs()
			{
				output = ".",
				beamPath = beamPath,
				solutionDir = "\"$([System.IO.Path]::GetDirectoryName(`$(DirectoryBuildPropsPath)`))\"",
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

		/// <summary>
		/// Set the local manifest with the current information in the ServiceDefinitions variable.
		/// </summary>
		public static async Promise SetManifest(BeamCommands cli, List<IBeamoServiceDefinition> definitions)
		{
			ServicesSetLocalManifestArgs args = new ServicesSetLocalManifestArgs();
			//check how many services exist locally
			int servicesCount = definitions.Count(definition => !string.IsNullOrEmpty(definition.ServiceInfo.projectPath));

			var services = new List<string>();
			var storagesPath = new List<string>();
			var storagesNames = new List<string>();
			var disabledServices = new List<string>();
			// TODO: add some validation to check that these files actually make sense

			for (var i = 0; i < servicesCount; i++)
			{
				if (string.IsNullOrEmpty(definitions[i].ServiceInfo.projectPath))
					continue;
				switch (definitions[i].ServiceType)
				{
					case ServiceType.MicroService:
						services.Add(definitions[i].ServiceInfo.projectPath);
						break;
					case ServiceType.StorageObject:
						storagesPath.Add(definitions[i].ServiceInfo.projectPath);
						storagesNames.Add(definitions[i].BeamoId);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (!definitions[i].ShouldBeEnabledOnRemote)
				{
					disabledServices.Add(definitions[i].BeamoId);
				}
			}

			if (services.Count > 0) args.services = services.ToArray();
			if (storagesPath.Count > 0) args.storagePaths = storagesPath.ToArray();
			if (storagesNames.Count > 0) args.storageNames = storagesNames.ToArray();
			if (disabledServices.Count > 0) args.disabledServices = disabledServices.ToArray();

			var command = cli.ServicesSetLocalManifest(args);
			try
			{
				await command.Run();
			}
			catch (Exception e)
			{
				LogExceptionVerbose(e);
			}
		}

		public static async Promise SetManifest(BeamCommands cli, List<BeamServiceSignpost> servicesFiles, List<BeamStorageSignpost> storagesFiles)
		{
			var args = new ServicesSetLocalManifestArgs();

			var services = new List<string>();
			var storagesPaths = new List<string>();
			var storagesNames = new List<string>();
			// TODO: add some validation to check that these files actually make sense

			for (var i = 0; i < servicesFiles.Count; i++)
			{
				services.Add(servicesFiles[i].CsprojPath);
			}

			for (var i = 0; i < storagesFiles.Count; i++)
			{
				storagesPaths.Add(storagesFiles[i].CsprojPath);
				storagesNames.Add(storagesFiles[i].name);
			}

			if (services.Count > 0) args.services = services.ToArray();
			if (storagesPaths.Count > 0) args.storagePaths = storagesPaths.ToArray();
			if (storagesNames.Count > 0) args.storageNames = storagesNames.ToArray();

			var command = cli.ServicesSetLocalManifest(args);
			await command.Run().Error(LogExceptionVerbose);
		}

		public static List<BeamServiceSignpost> GetBeamServices()
		{
			var files = GetSignpostFiles(".beamservice");
			var data = GetSignpostData<BeamServiceSignpost>(files);
			return data;
		}

		public static List<BeamStorageSignpost> GetBeamStorages()
		{
			var files = GetSignpostFiles(".beamstorage");
			var data = GetSignpostData<BeamStorageSignpost>(files);
			return data;
		}

		public static List<T> GetSignpostData<T>(IEnumerable<string> files) where T : ISignpostData
		{
			var output = new List<T>();
			foreach (var file in files)
			{
				var json = File.ReadAllText(file);
				var data = JsonUtility.FromJson<T>(json);
				data.AfterDeserialize(file);
				output.Add(data);
			}

			return output;
		}

		private async Promise CheckForDeletedServices()
		{
			bool foundDeletedService = false;

			LogVerbose("Checking for deleted microservices");
			for (int i = _services.Count - 1; i > -1; i--)
			{
				var name = _services[i].name;
				var sourcePath = $"{StandaloneMicroservicesPath}{name}/";
				var signpostPath = $"{BEAMABLE_PATH}{name}.beamservice";

				if (File.Exists(signpostPath))
				{
					if (!Directory.Exists(sourcePath))
					{
						LogVerbose($"The file {name}.beamservice exists but there is no source code for it.");
					}

					continue;
				}

				foundDeletedService = true;
				_services.RemoveAt(i);
			}

			LogVerbose("Checking for deleted storages");
			for (int i = _storages.Count - 1; i > -1; i--)
			{
				var name = _storages[i].name;
				var sourcePath = $"{StandaloneMicroservicesPath}{name}/";
				var signpostPath = $"{BEAMABLE_PATH}{name}.beamstorage";

				if (File.Exists(signpostPath))
				{
					if (!Directory.Exists(sourcePath))
					{
						LogVerbose($"The file {name}.beamstorage exists but there is no source code for it.");
					}
					continue;
				}

				foundDeletedService = true;
				_storages.RemoveAt(i);
			}

			if (foundDeletedService)
			{
				await SetManifest(_cli, _services, _storages);
			}
		}

		private async Promise CreateStorage(string storageName, List<IBeamoServiceDefinition> additionalReferences, bool shouldInitialize = true)
		{
			var service = new ServiceName(storageName);
			var slnPath = FindFirstSolutionFile();
			var relativePath = Path.Combine(StandaloneMicroservicesPath, storageName);
			if (Directory.Exists(relativePath))
			{
				LogVerbose($"{storageName} already exists!");
				return;
			}

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

			var signpost = new BeamStorageSignpost()
			{
				name = storageName,
				assetProjectPath = relativePath.Replace(StandaloneMicroservicesPath, string.Empty)
			};

			string signpostPath = $"{BEAMABLE_PATH}{storageName}.beamstorage";
			string signpostJson = JsonUtility.ToJson(signpost);

			LogVerbose($"Writing data to {storageName}.beamstorage file");
			File.WriteAllText(signpostPath, signpostJson);

			LogVerbose($"Starting the initialization of CodeService");
			// Re-initializing the CodeService to make sure all files are with the right information
			if (shouldInitialize)
			{
				await Init();
			}

			//Shoudln't we generate client code at the end of the creation?
			//For some reason this this line is never reached after the Init. And if put bfore Init, it doesn't work
			//await GenerateClientCode(serviceName);

			LogVerbose($"Finished creation of storage {storageName}");
		}

		/// <summary>
		/// Given the current SDK version, we should be able to figure out which version of Nuget packages we need.
		/// Note: if we're developing the SDK, the version will be 0.0.0, and the current local version of Nuget package is 0.0.123-local.
		/// </summary>
		/// <returns></returns>
		public static string GetCurrentNugetVersion()
		{
			var version = BeamableEnvironment.SdkVersion.ToString();
			if (version == "0.0.0")
			{
				version = "0.0.123";
			}

			return version;
		}

		private async Promise CreateMicroService(string serviceName, List<IBeamoServiceDefinition> dependencies, List<AssemblyDefinitionAsset> assemblyReferences = null, bool shouldInitialize = true)
		{
			var service = new ServiceName(serviceName);
			var slnPath = FindFirstSolutionFile();
			var fullPath = Path.Combine(StandaloneMicroservicesPath, serviceName);
			if (Directory.Exists(fullPath))
			{
				LogVerbose($"{serviceName} already exists!");
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

			BeamServiceSignpost signpost = new BeamServiceSignpost()
			{
				name = serviceName,
				assetProjectPath = fullPath.Replace(StandaloneMicroservicesPath, string.Empty),
				assemblyReferences = assemblyReferences?.ToArray()
			};

			string signpostPath = $"{BEAMABLE_PATH}{serviceName}.beamservice";
			string signpostJson = JsonUtility.ToJson(signpost);

			LogVerbose($"Writing data to {serviceName}.beamservice file");
			File.WriteAllText(signpostPath, signpostJson);

			LogVerbose($"Starting the initialization of CodeService");
			// Re-initializing the CodeService to make sure all files are with the right information
			if (shouldInitialize)
			{
				await Init();
			}

			//Shoudln't we generate client code at the end of the creation?
			//For some reason this this line is never reached after the Init. And if put bfore Init, it doesn't work
			//await GenerateClientCode(serviceName);

			LogVerbose($"Finished creation of service {serviceName}");
		}


		private static IEnumerable<string> GetSignpostFiles(string extension)
		{
			var files = new HashSet<string>();

			ScanDirectoryRecursive("Assets", extension, IgnoreFolderSuffixes, files);
			ScanDirectoryRecursive("Packages", extension, IgnoreFolderSuffixes, files);
			ScanDirectoryRecursive(Path.Combine(new[] { "Library", "PackageCache" }), extension, IgnoreFolderSuffixes, files);
			return files;
		}

		private static void ScanDirectoryRecursive(string directoryPath,
												   string targetExtension,
												   List<string> excludeFolders,
												   HashSet<string> foundFiles)
		{
			// TODO: ChatGPT wrote this, but actually, it should use a queue<string> to do a non-stack-recursive BFS over the file system
			if (!Directory.Exists(directoryPath))
			{
				return;
			}

			var directories = new Queue<string>();
			directories.Enqueue(directoryPath);

			while (directories.Count > 0)
			{
				try
				{
					var dir = directories.Dequeue();
					var folderName = Path.GetFileName(dir);

					var exclude = false;
					foreach (var excludeSuffix in excludeFolders)
					{
						if (folderName.EndsWith(excludeSuffix))
						{
							exclude = true;
							break;
						}
					}

					if (exclude) continue;

					foreach (var file in Directory.GetFiles(dir))
					{
						if (Path.GetExtension(file) == targetExtension)
						{
							foundFiles.Add(file);
						}
					}

					foreach (var subDir in Directory.GetDirectories(dir))
					{
						directories.Enqueue(subDir);
					}
				}
				catch (UnauthorizedAccessException ex)
				{
					LogVerbose($"Beam Error accessing {directoryPath}: {ex.Message}", true);
				}
			}
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
				LogExceptionVerbose(e);
			}
		}

		public void OpenMicroserviceFile(string serviceName)
		{
			IBeamoServiceDefinition def = ServiceDefinitions.FirstOrDefault(d => d.BeamoId.Equals(serviceName));

			if (def == null)
			{
				LogVerbose("Service does not exist!");
				return;
			}
			var fileName = $@"{def.ServiceInfo.projectPath}/{serviceName}.cs";
			EditorUtility.OpenWithDefaultApp(fileName);
		}
	}
}
