using Beamable.Api.CloudSaving;
using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Semantics;
using Beamable.Editor;
using Beamable.Editor.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Dotnet;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.UI.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
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
		private List<Promise> _logsCommands = new List<Promise>();
		private string _projectVersion;

		private const string BEAMABLE_PATH = "Assets/Beamable/";
		private const string MICROSERVICE_DLL_PATH = "bin/Debug/net6.0"; // is this true for all platforms and dotnet installations?
		private static readonly string StandaloneMicroservicesPath = $"{BEAMABLE_PATH}StandaloneMicroservices~/";

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
			// TODO: we need validation. What happens if the .beamservice files point to non-existent files
			SetSolution(_services);
			LogVerbose("Solution set done");

			LogVerbose("Set manifest start");
			await SetManifest(_cli, _services);
			LogVerbose("set manifest ended");

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
			LogVerbose("Completed");
		}

		public async Promise UpdateServicesVersions()
		{
			var version = new BeamVersionResults();
			var versionCommand = _cli.Version(new VersionArgs()
			{
				showVersion = true,
				showLocation = true,
				showTemplates = true,
				showType = true
			}).OnStreamVersionResults(result =>
			{
				version = result.data;
			});
			await versionCommand.Run().Error(LogExceptionVerbose);

			if (string.IsNullOrEmpty(version?.version) || version.version.Contains("1.0.0"))
			{
				LogVerbose("Could not detect current version, skipping");
				return;
			}

			var versions = _cli.ProjectVersion(new ProjectVersionArgs { requestedVersion = version?.version });
			versions.OnStreamProjectVersionCommandResult(result =>
			{
				_projectVersion = result.data.packageVersions[0];
				LogVerbose($"Versions updated: {_projectVersion}");
			});
			await versions.Run().Error(LogExceptionVerbose);
		}

		public async Promise RefreshServices()
		{
			ServiceDefinitions.Clear();
			LogVerbose("refresh remote services start");
			try
			{
				var ps = _cli.ServicesPs(new ServicesPsArgs() { json = false, remote = true });
				ps.OnStreamServiceListResult(cb =>
				{
					IsDockerRunning = cb.data.IsDockerRunning;
					LogVerbose($"Found {cb.data.BeamoIds.Count} remote services");
					_dispatcher.Schedule(() => PopulateDataWithRemote(cb.data));
				});
				await ps.Run();
			}
			catch
			{
				IsDockerRunning = false;
				LogVerbose("Could not list remote services, skip", true);
				return;
			}

			LogVerbose("refresh remote services end");

			LogVerbose("refresh local services start");

			PopulateDataWithLocal();

			LogVerbose("refresh local services end");
		}

		private void PopulateDataWithLocal()
		{
			_services = GetBeamServices();

			for (int i = 0; i < _services.Count; i++)
			{
				var name = _services[i].name;
				var dataIndex =
					ServiceDefinitions.FindIndex(definition => definition.BeamoId.Equals(name));
				if (dataIndex < 0)
				{
					ServiceDefinitions.Add(new BeamoServiceDefinition
					{
						ServiceInfo = new ServiceInfo()
						{
							name = name,
							dockerBuildPath = _services[i].assetRelativePath,
							dockerfilePath = _services[i].relativeDockerFile,
							dependencies = _services[i].dependedStorages.ToList()
						}
					});
					dataIndex = ServiceDefinitions.Count - 1;
					ServiceDefinitions[dataIndex].Builder = new BeamoServiceBuilder() { BeamoId = name };
					ServiceDefinitions[dataIndex].ServiceType = ServiceType.MicroService; //For now we only have microservice and not storages
					ServiceDefinitions[dataIndex].ShouldBeEnabledOnRemote = true; //TODO should read this from manifest or have it stored somewhere
					ServiceDefinitions[dataIndex].HasLocalSource = true;
				}
			}
		}


		private void PopulateDataWithRemote(BeamServiceListResult objData)
		{
			for (int i = 0; i < objData.BeamoIds.Count; i++)
			{
				var name = objData.BeamoIds[i];
				LogVerbose($"Handling {name} started");
				var dataIndex =
					ServiceDefinitions.FindIndex(definition => definition.BeamoId.Equals(name));
				if (dataIndex < 0)
				{
					var service = _services.FirstOrDefault(s => s.name == name);
					ServiceDefinitions.Add(new BeamoServiceDefinition
					{
						ServiceInfo = new ServiceInfo()
						{
							name = name,
							dockerBuildPath = service?.assetRelativePath,
							dockerfilePath = service?.relativeDockerFile,
							dependencies = service != null ? service.dependedStorages.ToList() : new List<string>()
						}
					});
					dataIndex = ServiceDefinitions.Count - 1;
					ServiceDefinitions[dataIndex].Builder = new BeamoServiceBuilder() { BeamoId = name };
				}

				ServiceDefinitions[dataIndex].ShouldBeEnabledOnRemote = objData.ShouldBeEnabledOnRemote[i];
				ServiceDefinitions[dataIndex].IsRunningOnRemote =
						objData.RunningState[i] ? BeamoServiceStatus.Running : BeamoServiceStatus.NotRunning;

				ServiceDefinitions[dataIndex].HasLocalSource = objData.IsLocal;
				LogVerbose($"Handling {name} ended");
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
			var projPath = signPost.relativeDockerFile.Replace("/Dockerfile", "");

			var args = new ProjectRegenerateArgs() { name = projName, output = tempPath, copyPath = projPath };
			var command = _cli.ProjectRegenerate(args);
			await command.Run();
		}

		/// <summary>
		/// Creates a new Standalone Microservice inside a hidden folder from Unity.
		/// </summary>
		/// <param name="serviceName"> The name of the Microservice to be created.</param>
		public async Promise CreateMicroservice(string serviceName)
		{
			LogVerbose($"Starting creation of service {serviceName}");

			var outputPath = $"{StandaloneMicroservicesPath}{serviceName}/";

			if (Directory.Exists(outputPath))
			{
				LogVerbose($"The service {serviceName} already exists!");
				return;
			}

			var service = new ServiceName(serviceName);
			var args = new ProjectNewArgs
			{
				solutionName = service,
				quiet = true,
				name = service,
				output = outputPath,
				version = _projectVersion
			};
			ProjectNewWrapper command = _cli.ProjectNew(args);
			await command.Run();

			string relativePath = $"{outputPath}services";
			string dockerFilePath = $"{serviceName}/Dockerfile";
			string projectFilePath = $"../{serviceName}.sln";
			var signpost = new BeamServiceSignpost()
			{
				name = serviceName,
				assetRelativePath = relativePath,
				relativeDockerFile = dockerFilePath,
				relativeProjectFile = projectFilePath
			};
			string signpostPath = $"{BEAMABLE_PATH}{serviceName}.beamservice";
			string signpostJson = JsonUtility.ToJson(signpost);

			LogVerbose($"Writing data to {serviceName}.beamservice file");
			File.WriteAllText(signpostPath, signpostJson);

			LogVerbose($"Starting the initialization of CodeService");
			// Re-initializing the CodeService to make sure all files are with the right information
			await Init();

			//Shoudln't we generate client code at the end of the creation?
			//For some reason this this line is never reached after the Init. And if put bfore Init, it doesn't work
			//await GenerateClientCode(serviceName);

			LogVerbose($"Finished creation of service {serviceName}");
		}

		public Promise RunStandaloneMicroservice(string id)
		{
			LogVerbose($"Start generating client code for service: {id}");


			var service = _services.FirstOrDefault(s => s.name == id);

			if (service == null)
			{
				LogVerbose($"The service {id} is not listed.", true);
				throw new Exception("Service is invalid.");
			}

			var microserviceFullPath = Path.GetFullPath(service.CsprojPath);
			var runCommand = $"run --project {microserviceFullPath} --property:CopyToLinkedProjects=false;GenerateClientCode=false";

			LogVerbose($"Running service: {id} using command: {runCommand}");
			_ = _dotnetService.Run(runCommand);

			return Promise.Success;
		}

		/// <summary>
		/// Build the USAM and generates the client code in the Beamable/Autogenerated folder.
		/// </summary>
		/// <param name="id">The id of the Standalone Microservice.</param>
		public async Promise GenerateClientCode(string id)
		{
			LogVerbose($"Start generating client code for service: {id}");


			var service = _services.FirstOrDefault(s => s.name == id);

			var microservicePath = Path.Combine(service.assetRelativePath, service.relativeProjectFile);
			var beamPath = BeamCliUtil.CLI_PATH.Replace(".dll", "");
			var buildCommand = $"build \"{microservicePath}\" /p:BeamableTool={beamPath} /p:GenerateClientCode=false";

			LogVerbose($"Starting build service: {id} using command: {buildCommand}");
			await _dotnetService.Run(buildCommand);

			LogVerbose($"Starting beam client code generator");

			string dllPath = $"{service.assetRelativePath}/{id}/{MICROSERVICE_DLL_PATH}/{id}.dll";
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
			var projectPs = _cli.ProjectPs().OnStreamServiceDiscoveryEvent(cb =>
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
		public static void SetSolution(List<BeamServiceSignpost> services)
		{
			// if there is nothing to add there is no need for an update
			if (services == null || services.Count == 0)
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
			var args = new ServicesSetLocalManifestArgs();
			var dependedStorages = new List<string>();
			int servicesCount = 0;

			//check how many services exist locally
			foreach (IBeamoServiceDefinition def in definitions)
			{
				if (!string.IsNullOrEmpty(def.ServiceInfo.dockerfilePath))
				{
					servicesCount++;
				}
			}

			if (servicesCount == 0)
			{
				LogVerbose("There are no services to write to a manifest!");
				return;
			}

			args.localHttpNames = new string[servicesCount];
			args.localHttpContexts = new string[servicesCount];
			args.localHttpDockerFiles = new string[servicesCount];
			args.shouldBeEnable = new string[servicesCount];

			// TODO: add some validation to check that these files actually make sense
			for (var i = 0; i < servicesCount; i++)
			{
				if (string.IsNullOrEmpty(definitions[i].ServiceInfo.dockerfilePath))
					continue;

				args.localHttpNames[i] = definitions[i].BeamoId;
				args.localHttpContexts[i] = definitions[i].ServiceInfo.dockerBuildPath;
				args.localHttpDockerFiles[i] = definitions[i].ServiceInfo.dockerfilePath;

				args.shouldBeEnable[i] = definitions[i].ShouldBeEnabledOnRemote.ToString();
				if (definitions[i].ServiceInfo.dependencies != null)
				{
					foreach (var storage in definitions[i].ServiceInfo.dependencies)
					{
						dependedStorages.Add($"{definitions[i].BeamoId}:{storage}");
					}
				}
			}
			args.storageDependencies = dependedStorages.ToArray();

			var command = cli.ServicesSetLocalManifest(args);
			await command.Run().Error(LogExceptionVerbose);
		}

		public static async Promise SetManifest(BeamCommands cli, List<BeamServiceSignpost> files)
		{
			var args = new ServicesSetLocalManifestArgs();
			var dependedStorages = new List<string>();

			if (files.Count == 0)
			{
				LogVerbose("There are no services to write to a manifest!");
				return;
			}

			args.localHttpNames = new string[files.Count];
			args.localHttpContexts = new string[files.Count];
			args.localHttpDockerFiles = new string[files.Count];
			args.shouldBeEnable = new string[files.Count];
			// TODO: add some validation to check that these files actually make sense
			for (var i = 0; i < files.Count; i++)
			{
				args.localHttpNames[i] = files[i].name;
				args.localHttpContexts[i] = files[i].assetRelativePath;
				args.localHttpDockerFiles[i] = files[i].relativeDockerFile;
				args.shouldBeEnable[i] = true.ToString(); //TODO not sure what value should be put in here, this would set all to true without new modifications
				if (files[i].dependedStorages != null)
				{
					foreach (var storage in files[i].dependedStorages)
					{
						dependedStorages.Add($"{files[i].name}:{storage}");
					}
				}
			}
			args.storageDependencies = dependedStorages.ToArray();

			var command = cli.ServicesSetLocalManifest(args);
			await command.Run().Error(LogExceptionVerbose);
		}

		public static List<BeamServiceSignpost> GetBeamServices()
		{
			var files = GetSignpostFiles(".beamservice");
			var data = GetSignpostData<BeamServiceSignpost>(files);
			return data;
		}


		public static List<T> GetSignpostData<T>(IEnumerable<string> files) where T : ISignpostData
		{
			var output = new List<T>();
			foreach (var file in files)
			{
				var json = File.ReadAllText(file);
				var data = JsonUtility.FromJson<T>(json);
				data.AfterDeserialize();
				output.Add(data);
			}

			return output;
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

		public Promise StopStandaloneMicroservice(IEnumerable<string> beamoIds)
		{
			foreach (string id in beamoIds)
			{
				//This is a bit ugly, happens that the C#MS running process name are their ids
				// Is there a better way to handle this?
				_dotnetService.SetPurposelyExit();
				foreach (Process process in Process.GetProcessesByName(id))
				{
					process.Kill();
				}
			}

			return Promise.Success;
		}

		public async Promise Stop(IEnumerable<string> beamoIds)
		{
			try
			{
				var cmd = _cli.ServicesStop(new ServicesStopArgs() { ids = beamoIds.ToArray() });
				await cmd.Run();

				foreach (string id in beamoIds)
				{
					var def = ServiceDefinitions.FirstOrDefault(d => d.BeamoId.Equals(id));
					if (def != null)
					{
						def.Builder.IsRunning = false;
					}
				}
			}
			catch (Exception e)
			{
				LogExceptionVerbose(e);
			}
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

			var path = Path.GetDirectoryName(def.ServiceInfo.dockerBuildPath);
			var fileName = $@"{path}/services/{serviceName}/{serviceName}.cs";
			EditorUtility.OpenWithDefaultApp(fileName);
		}

		public async Promise Run(IEnumerable<string> beamoIds)
		{
			var listToRun = beamoIds.ToList();
			foreach (var id in beamoIds)
			{
				var signin = _services.FirstOrDefault(signpost => signpost.name == id);
				if (signin?.dependedStorages?.Length > 0)
				{
					listToRun.AddRange(signin.dependedStorages.ToArray());
				}
			}
			try
			{

				var cmd = _cli.ServicesRun(new ServicesRunArgs() { ids = listToRun.ToArray() });
				cmd.OnLocal_progressServiceRunProgressResult(cb =>
				{
					ServiceDefinitions.FirstOrDefault(d => d.BeamoId.Equals(cb.data.BeamoId))?.Builder
									  .OnStartingProgress?.Invoke((int)cb.data.LocalDeployProgress, 100);
				});
				cmd.OnStreamServiceRunReportResult(cb =>
				{
					foreach (string id in beamoIds)
					{
						var def = ServiceDefinitions.FirstOrDefault(d => d.BeamoId.Equals(id));
						def?.Builder.OnStartingProgress?.Invoke((int)100, 100);
						def?.Builder.OnStartingFinished?.Invoke(cb.data.Success);
						if (def != null)
						{
							def.Builder.IsRunning = cb.data.Success;
						}
					}
				});
				await cmd.Run();
			}
			catch (Exception e)
			{
				LogExceptionVerbose(e);
			}
		}
	}
}
