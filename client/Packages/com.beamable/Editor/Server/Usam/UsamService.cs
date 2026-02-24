using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Reflection;
using Beamable.Common.Semantics;
using Beamable.Editor;
using Beamable.Editor.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	
	[Serializable]
	public class ServiceRoutingSetting
	{
		public string beamoId;

		public RoutingOption selectedOption = null;

		public List<RoutingOption> options = new List<RoutingOption>();
		
		public bool IsSelectedValid => options.Any(x => x.routingKey == selectedOption?.routingKey && x?.instance?.primaryKey == selectedOption?.instance?.primaryKey);

	}
	
	public enum RoutingOptionType
	{
		REMOTE, 
		LOCAL,
		AUTO,
		FRIEND
	}

	
	[Serializable]
	public class RoutingOption
	{
		public string display;
		public string routingKey;
		public RoutingOptionType type;
		public BeamServiceInstance instance;
		
	}
	
	
	public partial class UsamService 
		: IStorageHandler<UsamService>, Beamable.Common.Dependencies.IServiceStorable
		, ILoadWithContext
	{
		[Serializable]
		public struct NamedLogView
		{
			public string beamoId;
			public LogView logView;
			public List<CliLogMessage> logs;
		}

		public static List<IFederationId> CompiledFederationIds = TypeCache.GetTypesDerivedFrom<IFederationId>()
		                                                                   .Where(type => !type.IsAbstract && !type.IsInterface)
		                                                                   .Select(
			                                                                   type => (IFederationId)Activator.CreateInstance(type))
		                                                                   .ToList();
		
		
		private StorageHandle<UsamService> _handle;
		private BeamCommands _cli;
		public BeamCommands Cli => _cli;
		public BeamableDispatcher _dispatcher;

		public BeamShowManifestCommandOutput latestManifest;
		public List<BeamServiceStatus> latestStatus;
		public BeamDockerStatusCommandOutput latestDockerStatus;
		
		public Dictionary<string, AssemblyDefinitionAsset> allAssemblyAssets;

		/// <summary>
		/// Controls where traffic will be directed
		/// </summary>
		public List<ServiceRoutingSetting> routingSettings = new List<ServiceRoutingSetting>();
		
		/// <summary>
		/// This is a counter to track how many times the <see cref="ListenForBuildChanges"/> method has been called.
		/// Internal to the method, this field is used to stop old invocations of the on-going coroutine
		/// </summary>
		public int latestListenTaskId;

		/// <summary>
		/// This is a counter to track how many times the <see cref="WaitReload"/> method has been called.
		/// If reload is called twice, then two callbacks will appear.. but only the callback for the most
		/// recent invocation will actually update the local data.
		/// This is to reduce flicker in the application. It may come at the cost of longer load times
		/// if the reload command is being called too often. 
		/// </summary>
		public int latestReloadTaskId;

		public List<NamedLogView> _namedLogs = new List<NamedLogView>();

		
		[NonSerialized]
		public bool hasReceivedManifestThisDomain;
		
		[NonSerialized]
		private ProjectPsWrapper _watchCommand;
		
		[NonSerialized]
		private ServicesDockerStatusWrapper _listenDockerCommand;
		
		[NonSerialized]
		private ProjectStorageSnapshotWrapper _snapshotCommand;
		
		[NonSerialized]
		public Promise receivedAnyDockerStateYet = new Promise();

		[NonSerialized]
		private Dictionary<string, ProjectLogsWrapper> _serviceToLogCommand =
			new Dictionary<string, ProjectLogsWrapper>();

		[NonSerialized]
		private Dictionary<string, ServiceCliAction> _serviceToAction = new Dictionary<string, ServiceCliAction>();
		
		[NonSerialized]
		private Promise _watchPromise;
		
		
		
		[NonSerialized]
		private ProjectGenerateClientWrapper _generateClientCommand;
		[NonSerialized]
		private ProjectGenerateClientOapiWrapper _generateClientOapiWrapper;

		private BeamEditorContext _ctx;
		private ProjectStorageRestoreWrapper _restoreCommand;
		private ProjectStorageEraseWrapper _eraseCommand;
		private CommonAreaService _commonArea;

		public List<BeamCheckResultsForBeamoId> _requiredUpgrades = new List<BeamCheckResultsForBeamoId>();

		public UsamAssemblyService AssemblyService => _assemblyUtil;
		private UsamAssemblyService _assemblyUtil;

		private AssemblyDefinitionAsset _commonAssemblyAsset;
		private MicroserviceConfiguration _config;

		private BeamWebCommandFactory _webCommandFactory;

		public const string SERVICES_FOLDER = "BeamableServices/";
		public const string SERVICES_SLN_PATH = SERVICES_FOLDER + "beamableServices.sln";

		public enum ServiceCliActionType
		{
			Running, Stopping, Creating
		}
		public class ServiceCliAction
		{
			public float progressRatio;
			public bool isFailed;
			public bool isComplete;
			public ServiceCliActionType label;
			public BeamCommandWrapper command;
		}
		
		public UsamService(
			BeamEditorContext ctx,
			BeamCommands cli, 
			CommonAreaService commonArea,
			BeamableDispatcher dispatcher,
			ReflectionCache editorCache,
			MicroserviceConfiguration config,
			BeamWebCommandFactory webCommandFactory)
		{
			_config = config;
			_commonArea = commonArea;
			_ctx = ctx;
			_dispatcher = dispatcher;
			_cli = cli;
			_webCommandFactory = webCommandFactory;
			
			_commonArea.EnsureAreas(out _commonAssemblyAsset);
			_assemblyUtil = new UsamAssemblyService(this);
		}

		public bool TryGetStatus(string beamoId, out BeamServiceStatus status)
		{
			status = null;
			if (latestStatus == null) return false;
			
			for (var i = 0; i < latestStatus.Count; i++)
			{
				if (latestStatus[i].service == beamoId)
				{
					status = latestStatus[i];
					return true;
				}
			}

			return false;
		}

		public bool TryGetLogs(string beamoId, out NamedLogView log)
		{
			log = default;
			for (var i = 0; i < _namedLogs.Count; i++)
			{
				if (_namedLogs[i].beamoId == beamoId)
				{
					log = _namedLogs[i];
					return true;
				}
			}

			return false;
		}
		
		public void ReceiveStorageHandle(StorageHandle<UsamService> handle)
		{
			_handle = handle;
		}

		public void OnBeforeSaveState()
		{
		}

		public void OnAfterLoadState()
		{
			latestManifest ??= new BeamShowManifestCommandOutput();

			latestManifest.services ??= new List<BeamManifestServiceEntry>();

			LoadAllAssemblies();
			_assemblyUtil.Reload();
			Reload();
		}
		
		
		
		public async Promise SetMicroserviceChanges(string serviceName, 
		                                            List<AssemblyDefinitionAsset> assemblyDefinitions, 
		                                            List<string> dependencies)
		{
			var service = latestManifest.services.FirstOrDefault(s => s.beamoId == serviceName);
			if (service == null)
			{
				throw new ArgumentException($"Invalid service name was passed: {serviceName}");
			}
			
			await UpdateServiceStoragesDependencies(service, dependencies);

			await UpdateServiceReferences(service.beamoId, service.csprojPath, assemblyDefinitions);

			UsamLogger.Log($"Finished updating microservice [{serviceName}] data");
		}

		public async Promise SetStorageChanges(string storageName, List<AssemblyDefinitionAsset> assemblyDefinitions)
		{
			UsamLogger.Log($"Starting updating storage changes");

			var storage = latestManifest.storages.FirstOrDefault(s => s.beamoId == storageName);
			if (storage == null)
			{
				throw new ArgumentException($"Invalid storage name was passed: {storageName}");
			}

			await UpdateServiceReferences(storage.beamoId, storage.csprojPath, assemblyDefinitions);

			UsamLogger.Log($"Finished updating storage [{storageName}] changes");
		}

		public string GetClientFileCandidatePath(string beamoId)
		{
			if (_assemblyUtil.beamoIdToClientHintPath.TryGetValue(beamoId, out var hintPath))
			{
				return hintPath;
			}
			return Path.Combine(Constants.Features.Services.AUTOGENERATED_CLIENT_PATH, $"{beamoId}Client.cs");

		}
		
		
		public async Promise UpdateServiceStoragesDependencies(BeamManifestServiceEntry service, List<string> dependencies)
		{
			var serviceName = service.beamoId;
			var currentDependencies = service.storageDependencies;

			var dependenciesToRemove = currentDependencies.Where(dep => !dependencies.Contains(dep)).ToList();
			var dependenciesToAdd = dependencies.Where(dep => !currentDependencies.Contains(dep)).ToList();

			//TODO: can this be made asynchronous? not sure since all are changing the same csproj file

			foreach (string dep in dependenciesToRemove)
			{
				UsamLogger.Log($"Removing dependency [{dep}] from service [{serviceName}]");
				var removeCommand = _cli.ProjectDepsRemove(new ProjectDepsRemoveArgs()
				{
					microservice = serviceName, dependency = dep
				});
				await removeCommand.Run();
			}

			foreach (string dep in dependenciesToAdd)
			{
				UsamLogger.Log($"Adding dependency [{dep}] to service [{serviceName}]");
				var addCommand = _cli.ProjectDepsAdd(new ProjectDepsAddArgs()
				{
					microservice = serviceName, dependency = dep
				});
				await addCommand.Run();
			}
		}
		
		public async Promise UpdateServiceReferences(string beamoId, string csprojPath, List<AssemblyDefinitionAsset> assemblyDefinitions, bool shouldRefresh = true)
		{
			UsamLogger.Log($"Starting updating references");

			var pathsList = new List<string>();
			var namesList = new List<string>();
			foreach (AssemblyDefinitionAsset asmdef in assemblyDefinitions)
			{
				namesList.Add(asmdef.name);
				var pathFromRootFolder = CsharpProjectUtil.GenerateCsharpProjectFilename(asmdef.name);
				var pathToService = csprojPath;
				pathsList.Add(PackageUtil.GetRelativePath(pathToService, pathFromRootFolder));
			}
			
			var updateCommand = _cli.UnityUpdateReferences(new UnityUpdateReferencesArgs()
			{
				service = beamoId,
				paths = pathsList.ToArray(),
				names = namesList.ToArray(),
				sln = SERVICES_SLN_PATH
			});
			await updateCommand.Run();

			if (shouldRefresh)
			{
				CsProjUtil.OnPreGeneratingCSProjectFiles(this);
			}
		}

		public void OpenDockerInstallPage()
		{
			var command = _cli.ServicesDockerStart(new ServicesDockerStartArgs
			{
				linksOnly = true
			});
			command.OnStreamStartDockerCommandOutput(cb =>
			{
				var links = cb.data;
				Application.OpenURL(links.dockerDesktopUrl);
				Application.OpenURL(links.downloadUrl);
			});
			command.Run();
		}

		public void StartDocker(Action<bool> afterStarted)
		{
			var command = _cli.ServicesDockerStart(new ServicesDockerStartArgs());
			command.OnStreamStartDockerCommandOutput(cb =>
			{
				var running = cb.data.alreadyRunning || cb.data.attempted;
				afterStarted?.Invoke(running);
			});
			var _ = command.Run();
		}

		public void ListenForDocker()
		{
			if (_listenDockerCommand != null)
			{
				_listenDockerCommand.Cancel();
				_listenDockerCommand = null;
			}
			_listenDockerCommand = _cli.ServicesDockerStatus(new ServicesDockerStatusArgs {watch = true});

			_listenDockerCommand.OnStreamDockerStatusCommandOutput(cb =>
			{
				latestDockerStatus = cb.data;
				receivedAnyDockerStateYet.CompleteSuccess();
			});
			var _ = _listenDockerCommand.Run();
		}

		
		public void ListenForStatus()
		{
			if (_ctx.BeamCli.IsLoggedOut) return;
			
			if (_watchCommand != null)
			{
				_watchCommand.Cancel();
				_watchCommand = null;
			}
			_watchCommand = _cli.ProjectPs(new ProjectPsArgs {watch = true,});
			_watchCommand.OnStreamCheckStatusServiceResult(cb =>
			{
				latestStatus = cb.data.services;
				foreach (var status in latestStatus)
				{
					var shouldListenForLogs = false;
					foreach (var route in status.availableRoutes)
					{
						if (route.knownToBeRunning)
						{
							foreach (var instance in route.instances)
							{
								var isLocal = instance.latestRemoteEvent?.service == null;
								if (isLocal)
								{
									shouldListenForLogs = true;
								}
							}
						}
					}

					if (shouldListenForLogs)
					{
						ListenForLogs(status.service);
					}

					UpdateRoutingOptions(status);
				}

			});
			var _ = _watchCommand.Run();
			// TODO: add re-try logic
		}

		void UpdateRoutingOptions(BeamServiceStatus status)
		{
			if (!TryGetRoutingSetting(status.service, out var setting))
			{
				setting = new ServiceRoutingSetting
				{
					beamoId = status.service, selectedOption = null, options = new List<RoutingOption>()
				};
				routingSettings.Add(setting);
			}
			
			// populate all the options
			setting.options.Clear();
			RoutingOption localOption = null;
			RoutingOption remoteOption = null;

			var autoOption = new RoutingOption
			{
				display = null, 
				routingKey = latestManifest.localRoutingKey, 
				type = RoutingOptionType.AUTO
			};
			setting.options.Add(autoOption);
			
			
			for (var i = 0; i < status.availableRoutes.Count; i++)
			{
				var route = status.availableRoutes[i];
				if (route.instances.Count == 0)
				{
					continue;
				}

				// Take the first instance, which is technical a bug.
				//  it is possible that a federation could be backed by more than 1 instance,
				//  but the instances have different emails. 
				//  if this ever happens, this code is glitched, because we are assuming that
				//  all instances of a routing key share the same author. 
				var instance = route.instances[0];

				if (route.routingKey == latestManifest.localRoutingKey)
				{
					localOption = new RoutingOption
					{
						display = "local",
						type = RoutingOptionType.LOCAL,
						instance = instance,
						routingKey = latestManifest.localRoutingKey
					};
					setting.options.Add(localOption);
				} else if (string.IsNullOrEmpty(route.routingKey))
				{
					// this is the remote-deployed service, because it has an empty routingKey
					remoteOption = new RoutingOption
					{
						display = "realm", 
						type = RoutingOptionType.REMOTE,
						instance = instance
					};
					setting.options.Add(remoteOption);
				}
				else
				{
					setting.options.Add(new RoutingOption
					{
						display = instance.startedByAccountEmail,
						type = RoutingOptionType.FRIEND,
						routingKey = route.routingKey,
						instance = instance
					});
				}
			}

			{
				if (localOption == null)
				{
					setting.options.Add(new RoutingOption
					{
						display = "local",
						type = RoutingOptionType.LOCAL,
						instance = null,
						routingKey = latestManifest.localRoutingKey
					});
				}
				
				
				if (localOption != null)
				{
					autoOption.display = localOption.display;
					autoOption.instance = localOption.instance;
					// autoOption.routingKey = localOption.routingKey;
				} else if (remoteOption != null)
				{
					autoOption.display = remoteOption.display;
					autoOption.instance = remoteOption.instance;
					// autoOption.routingKey = remoteOption.routingKey;
				}
			}
			
			// if the selection is no longer valid, then we jump to local first, 
			if (setting.selectedOption == null || setting.selectedOption.type == RoutingOptionType.AUTO)
			{
				setting.selectedOption = autoOption;
			}
		}
		
		public void ListenForLogs(string service)
		{
			if (_serviceToLogCommand.TryGetValue(service, out var existingCommand))
			{
				existingCommand.Cancel();
				_serviceToLogCommand.Remove(service);
			}
			
			var logCommand = _cli.ProjectLogs(new ProjectLogsArgs
			{
				service = new ServiceName(service), reconnect = true
			});
			logCommand.OnStreamTailLogMessageForClient(cb =>
			{
				AddLog(service, new CliLogMessage
				{
					message = cb.data.message,
					timestamp = cb.ts,
					logLevel = cb.data.logLevel
				});
			});
			var promise = logCommand.Run();
			_serviceToLogCommand[service] = logCommand;
		}

		private void LoadAllAssemblies()
		{
			if (allAssemblyAssets != null)
			{
				return;
			}

			var allAssets = AssemblyDefinitionHelper.EnumerateAssemblyDefinitionAssets().ToList();
			allAssemblyAssets = new Dictionary<string, AssemblyDefinitionAsset>();

			foreach (var asset in allAssets)
			{
				var info = AssemblyDefinitionHelper.ConvertToInfo(asset);
				allAssemblyAssets.Add(info.Name, asset);
			}
		}

		public void OpenMongo(string beamoId)
		{
			var command = _cli.ProjectOpenMongo(new ProjectOpenMongoArgs {serviceName = new ServiceName(beamoId)});
			command.OnLog(cb =>
			{
				AddLog(beamoId, cb.data);
			});
			var _ = command.Run();
		}
		
		public void OpenSwagger(string beamoId, bool remote=false)
		{
			try
			{
				var cmd = _cli.ProjectOpenSwagger(
					new ProjectOpenSwaggerArgs()
					{
						remote = remote, 
						serviceName = new ServiceName(beamoId),
						srcTool = "unity"
					});
				var _ = cmd.Run();
			}
			catch (Exception e)
			{
				UsamLogger.Log(e.GetType().Name, e.Message, e.StackTrace);
			}
		}

		public void OpenProject(string beamoId, string projectPath)
		{
			OpenSolution();
		}

		public bool IsLoadingLocally(BeamServiceStatus status)
		{
			var actuallyRunning = status.availableRoutes.Any(x => x.knownToBeRunning &&
			                                                      x.routingKey == latestManifest.localRoutingKey);
			return actuallyRunning != IsRunningLocally(status);
		}
		
		public bool IsRunningLocally(BeamServiceStatus status)
		{
			var actuallyRunning = status.availableRoutes.Any(x => x.knownToBeRunning &&
			                                       x.routingKey == latestManifest.localRoutingKey);
			if (TryGetExistingAction(status.service, out var progress) && !progress.isComplete)
			{
				if (progress.label == ServiceCliActionType.Running)
				{
					return true;
				} else if (progress.label == ServiceCliActionType.Stopping)
				{
					return false;
				}
			}

			return actuallyRunning;
		}

		public bool IsCreatingLocally(string beamoId)
		{
			if (TryGetExistingAction(beamoId, out var progress) && !progress.isComplete)
			{
				return progress.label == ServiceCliActionType.Creating;
			}
			return false;
		}

		public void ToggleRun(BeamManifestStorageEntry storage, BeamServiceStatus status)
		{
			var isRunning = IsRunningLocally(status);
			if (isRunning)
			{
				StopStorage(storage);
			}
			else
			{
				StartStorage(storage);
			}
		}
		
		public void ToggleRun(BeamManifestServiceEntry service, BeamServiceStatus status)
		{
			var isRunning = IsRunningLocally(status);
			if (isRunning)
			{
				StopService(service);
			}
			else
			{
				StartService(service);
			}
		}

		public bool TryGetRoutingSetting(string service, out ServiceRoutingSetting setting)
		{
			setting = routingSettings.FirstOrDefault(x => x.beamoId == service);
			return setting != null;
		}
		
		public bool TryGetExistingAction(string service, out ServiceCliAction action)
		{
			return _serviceToAction.TryGetValue(service, out action);
		}

		void CancelExistingAction(string service)
		{
			if (TryGetExistingAction(service, out var action))
			{
				action.command.Cancel();
				_serviceToAction.Remove(service);
			}
		}

		ServiceCliAction SetServiceAction(string service, ServiceCliActionType label, BeamCommandWrapper command)
		{
			CancelExistingAction(service);
			var action = _serviceToAction[service] = new ServiceCliAction
			{
				label = label,
				command = command
			};
			command.Command.OnTerminate(cb =>
			{
				action.isComplete = true;
			});
			return action;
		}

		void AddLog(BeamManifestServiceEntry service, CliLogMessage logMessage)
		{
			// TODO: remove this method
			AddLog(service.beamoId, logMessage);
		}
		
		void AddLog(string beamoId, CliLogMessage logMessage)
		{
			if (string.IsNullOrEmpty(logMessage.message)) return;
			
			if (!TryGetLogs(beamoId, out var log))
			{
				log = new NamedLogView
				{
					logs = new List<CliLogMessage>(), beamoId = beamoId, logView = new LogView
					{
						verbose = new LogLevelView
						{
							enabled = false,
						},
						debug = new LogLevelView
						{
							enabled = false
						}
					}
				};
				_namedLogs.Add(log);
			}

			var message = logMessage.message.TrimStart(new char[] {' ', '\n', '\r'});
			log.logs.Add(new CliLogMessage
			{
				message = message,
				logLevel = logMessage.logLevel,
				timestamp = logMessage.timestamp
			});
			
			
			if (_config.LogErrorsToUnityConsole  && (logMessage.logLevel.ToLowerInvariant().StartsWith("f") || logMessage.logLevel.ToLowerInvariant().StartsWith("e"))) // fatal or error
			{
				Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, $"[{beamoId}] {message}");
			}
		}
		
		void StopService(BeamManifestServiceEntry service)
		{
			var stopCommand = _cli.ProjectStop(new ProjectStopArgs
			{
				ids = new string[] {service.beamoId}, killTask = true
			});
			var action = SetServiceAction(service.beamoId, ServiceCliActionType.Stopping, stopCommand);
			stopCommand.OnLog(cb =>
			{
				AddLog(service, cb.data);
			});
			if (_serviceToLogCommand.TryGetValue(service.beamoId, out var logCommand))
			{
				logCommand.Cancel();
				_serviceToLogCommand.Remove(service.beamoId);
			}
			// stopCommand.OnStreamStopProjectCommandOutput(cb =>
			// {
			// 	if (_serviceToLogCommand.TryGetValue(cb.data.serviceName, out var logCommand))
			// 	{
			// 		logCommand.Cancel();
			// 		_serviceToLogCommand.Remove(cb.data.serviceName);
			// 	}
			// });
			stopCommand.Run();
		}

		void StartService(BeamManifestServiceEntry service)
		{
			var runCommand = _cli.ProjectRun(new ProjectRunArgs
			{
				detach = true,
				ids = new string[] {service.beamoId},
				force = true,
				watch = false,
				noClientGen = true,
			});
			var action = SetServiceAction(service.beamoId, ServiceCliActionType.Running, runCommand);
			if (TryGetLogs(service.beamoId, out var log) && log.logView.clearOnPlay)
			{
				log.logs.Clear();
				log.logView.RebuildView();
			}
			runCommand.OnStreamRunProjectResultStream(cb =>
			{
				action.progressRatio = cb.data.progressRatio;
			});
			runCommand.OnLog(cb =>
			{
				AddLog(service, cb.data);
			});
			runCommand.OnErrorRunFailErrorOutput(cb =>
			{
				action.isFailed = true;
			});
			var _ = runCommand.Run();

		}
		
		void StopStorage(BeamManifestStorageEntry storage)
		{
			var stopCommand = _cli.ProjectStop(new ()
			{
				ids = new string[] {storage.beamoId},
			});
			var action = SetServiceAction(storage.beamoId, ServiceCliActionType.Stopping, stopCommand);
			stopCommand.OnLog(cb =>
			{
				AddLog(storage.beamoId, cb.data);
			});
			stopCommand.Run();
		}
		
		void StartStorage(BeamManifestStorageEntry storage)
		{
			var runCommand = _cli.ProjectRun(new ProjectRunArgs()
			{
				ids = new[]{storage.beamoId},
			});
			var action = SetServiceAction(storage.beamoId, ServiceCliActionType.Running, runCommand);
			if (TryGetLogs(storage.beamoId, out var log) && log.logView.clearOnPlay)
			{
				log.logs.Clear();
				log.logView.RebuildView();
			}

			runCommand.OnStreamRunProjectResultStream(cb =>
			{
				action.progressRatio = cb.data.progressRatio;
			});
			
			runCommand.OnLog(cb =>
			{
				AddLog(storage.beamoId, cb.data);
			});
			runCommand.OnError(cb =>
			{
				action.isFailed = true;
			});
			var _ = runCommand.Run();

		}

		public void OpenPortalToReleaseSection()
		{
			var url = $"{BeamableEnvironment.PortalUrl}/{_ctx.BeamCli.Cid}/games/{_ctx.BeamCli.ProductionRealm.Pid}/realms/{_ctx.BeamCli.Pid}/microservices?refresh_token={_ctx.Requester.Token.RefreshToken}";
			Application.OpenURL(url);
		}

		public void EraseMongo(BeamManifestStorageEntry storage)
		{
			if (_eraseCommand != null)
			{
				_eraseCommand.Cancel();
				_eraseCommand = null;
			}
			_eraseCommand = _cli.ProjectStorageErase(new ProjectStorageEraseArgs {beamoId = storage.beamoId});
			_eraseCommand.OnMongoLogsCliLogMessage(cb =>
			{
				AddLog(storage.beamoId, cb.data);
			});
			var _ = _eraseCommand.Run();
		}

		public void RestoreMongo(BeamManifestStorageEntry storage, string inputFolder)
		{
			if (_restoreCommand != null)
			{
				_restoreCommand.Cancel();
				_restoreCommand = null;
			}
			_restoreCommand = _cli.ProjectStorageRestore(new ProjectStorageRestoreArgs
			{
				beamoId = storage.beamoId,
				input = inputFolder,
				merge = false
			});
			_restoreCommand.OnMongoLogsCliLogMessage(cb =>
			{
				AddLog(storage.beamoId, cb.data);
			});
			var _ = _restoreCommand.Run();
		}

		public void SnapshotMongo(BeamManifestStorageEntry storage, string outputFolder)
		{
			if (_snapshotCommand != null)
			{
				_snapshotCommand.Cancel();
				_snapshotCommand = null;
			}
			_snapshotCommand = _cli.ProjectStorageSnapshot(new ProjectStorageSnapshotArgs
			{
				beamoId = storage.beamoId, output = outputFolder
			});
			_snapshotCommand.OnMongoLogsCliLogMessage(cb =>
			{
				AddLog(storage.beamoId, cb.data);
			});
			var _ = _snapshotCommand.Run();
		}

		public void DeleteProject(string beamoId, string csProjPath)
		{
			// TODO: Delete auto generated client. 

			_cli.ProjectRemove(new ProjectRemoveArgs {sln = SERVICES_SLN_PATH, ids = new string[] {beamoId}})
			    .OnStreamDeleteProjectCommandOutput(_ =>
			    {
				    {
					    var hintPath = GetClientFileCandidatePath(beamoId);
					    File.Delete(hintPath);
					    File.Delete(hintPath + ".meta");
				    }
				    
				    Reload();
			    })
			    .Run();
			
		}

		public async Promise CreateStorage(string newStorageName, List<string> dependencies)
		{
			var command = _cli.ProjectNewStorage(new ProjectNewStorageArgs()
			{
				name = new ServiceName(newStorageName),
				sln = SERVICES_SLN_PATH,
				serviceDirectory = SERVICES_FOLDER,
				linkTo = dependencies.ToArray()
			});
			
			// mock out the expected results in the latestManifest&status
			var mockService = new BeamManifestStorageEntry()
			{
				beamoId = newStorageName,
				Flags = BeamManifestEntryFlags.IS_STORAGE,
				unityReferences = new List<BeamUnityAssemblyReferenceData>(),
				csprojPath = Path.Combine(SERVICES_FOLDER, newStorageName, $"{newStorageName}.csproj"),
				// TODO: other fields?
			};
			latestManifest.storages.Add(mockService);
			var mockStatus = new BeamServiceStatus
			{
				service = newStorageName, serviceType = "storage",
				availableRoutes = new List<BeamServicesForRouteCollection>()
			};
			latestStatus.Add(mockStatus);
			
			var action = SetServiceAction(newStorageName, ServiceCliActionType.Creating, command);

			var logCount = 0;
			AddLog(mockService.beamoId, new CliLogMessage
			{
				logLevel = "Info",
				message = $"Creating storage {newStorageName}",
				timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
			});
			action.progressRatio = .1f;
			command.OnLog(cb =>
			{
				if (!cb.data.logLevel.ToLowerInvariant().StartsWith("i")) return;
				logCount++;
				action.progressRatio = logCount / 4f;
				AddLog(mockService.beamoId, cb.data);
			});
			

			await command.Run().Error(_ =>
			{
				action.isFailed = true;
			});

			await UpdateServiceReferences(mockService.beamoId, mockService.csprojPath, new List<AssemblyDefinitionAsset> {_commonAssemblyAsset},
			                              shouldRefresh: false);

			Reload();
			action.progressRatio = 1;
			action.isComplete = true;
			AddLog(mockService.beamoId, new CliLogMessage
			{
				logLevel = "Info",
				message = $"Created service {newStorageName}",
				timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
			});
		}
		
		public async Promise CreateService(string newServiceName, List<string> dependencies)
		{
			var newProjectCommand = _cli.ProjectNewService(new ProjectNewServiceArgs
			{
				generateCommon = false,
				name = new ServiceName(newServiceName),
				sln = SERVICES_SLN_PATH,
				serviceDirectory = SERVICES_FOLDER,
				linkTo = dependencies.ToArray()
			});
			
			// mock out the expected results in the latestManifest&status
			var mockService = new BeamManifestServiceEntry
			{
				beamoId = newServiceName,
				csprojPath = Path.Combine(SERVICES_FOLDER, newServiceName, $"{newServiceName}.csproj"),
				unityReferences = new List<BeamUnityAssemblyReferenceData>(),
				Flags = BeamManifestEntryFlags.IS_SERVICE

				// TODO: other fields?
			};
			latestManifest.services.Add(mockService);
			var mockStatus = new BeamServiceStatus
			{
				service = newServiceName, serviceType = "service",
				availableRoutes = new List<BeamServicesForRouteCollection>()
			};
			latestStatus.Add(mockStatus);
			
			var action = SetServiceAction(newServiceName, ServiceCliActionType.Creating, newProjectCommand);

			var logCount = 0;
			AddLog(mockService, new CliLogMessage
			{
				logLevel = "Info",
				message = $"Creating service {newServiceName}",
				timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
			});
			action.progressRatio = .1f;
			newProjectCommand.OnLog(cb =>
			{
				var isVerbose = cb.data.logLevel.ToLowerInvariant().StartsWith("v");
				var isDebug = cb.data.logLevel.ToLowerInvariant().StartsWith("d");
				if (!isDebug && !isVerbose)
				{
					logCount++;
					
					// I don't actually know the number of log messages and the
					//  create-command doesn't emit progress specifically. So let's guess.
					const float guessedNumberOfInfoLogsThatIndicateLoadingProgressForServiceCreation = 8f;
					action.progressRatio = logCount / guessedNumberOfInfoLogsThatIndicateLoadingProgressForServiceCreation;
				}
				
				AddLog(mockService, cb.data);
			});

			try
			{
				
				await newProjectCommand.Run();

				action.progressRatio = .8f;
				AddLog(mockService,
				       new CliLogMessage
				       {
					       logLevel = "Info",
					       message = $"Configuring common reference {newServiceName}",
					       timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
				       });

				await UpdateServiceReferences(mockService.beamoId, mockService.csprojPath, new List<AssemblyDefinitionAsset> {_commonAssemblyAsset},
				                              shouldRefresh: false);
				

				AddLog(mockService,
				       new CliLogMessage
				       {
					       logLevel = "Info",
					       message = $"Created service {newServiceName}",
					       timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
				       });

				action.isComplete = true;
				action.progressRatio = 1f;

				await WaitReload();
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to create service. " + ex.Message);
				action.isFailed = true;
			}

		}

		

		async Promise Build(ProjectBuildArgs args)
		{
			var buildCommand = _cli.ProjectBuild(args);
			buildCommand.OnStreamBuildProjectCommandOutput(cb =>
			{
				if (!cb.data.report.isSuccess)
				{
					var errors = cb.data.report.errors;
					foreach (var error in errors)
					{
						AddLog(cb.data.service, new CliLogMessage
						{
							logLevel = "Error",
							message = error.formattedMessage,
							timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
						});
					}
				}
			});
			await buildCommand.Run();
		}

		public Promise<string> GetLocalConnectionString(BeamManifestStorageEntry service)
		{
			var p = new Promise<string>();
			_cli.ServicesGetConnectionString(new ServicesGetConnectionStringArgs
			{
				quiet = true, storageName = service.beamoId
			}).OnStreamServicesGetConnectionStringCommandOutput(cb =>
			{
				var connStr = cb.data.connectionString;
				p.CompleteSuccess(connStr);
				
			}).OnError(cb =>
			{
				Debug.LogError("connection string error: " +cb.data.message);
				p.CompleteError(new Exception(cb.data.message));
			}).Run();

			return p;
		}

		public void OpenSolution(bool onlyGenerate=false)
		{
			var _ = _cli.ProjectOpen(new ProjectOpenArgs
			{
				onlyGenerate = onlyGenerate,
				sln = SERVICES_SLN_PATH,
				fromUnity = true
			}).Run();
		}
	}
}
