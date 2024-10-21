using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Dependencies;
using Beamable.Common.Reflection;
using Beamable.Common.Scheduler;
using Beamable.Common.Semantics;
using Beamable.Editor;
using Beamable.Editor.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	public class UsamService : IStorageHandler<UsamService>, Beamable.Common.Dependencies.IServiceStorable
	{
		[Serializable]
		public struct NamedLogView
		{
			public string beamoId;
			public LogView logView;
			public List<CliLogMessage> logs;
		}
		
		private StorageHandle<UsamService> _handle;
		private BeamCommands _cli;
		public BeamCommands Cli => _cli;
		public BeamableDispatcher _dispatcher;

		public BeamShowManifestCommandOutput latestManifest;
		public List<BeamServiceStatus> latestStatus;
		public BeamDockerStatusCommandOutput latestDockerStatus;

		public List<NamedLogView> _namedLogs = new List<NamedLogView>();

		[NonSerialized]
		public bool hasReceivedManifestThisDomain;
		
		[NonSerialized]
		private ProjectPsWrapper _watchCommand;
		
		[NonSerialized]
		private ServicesDockerStatusWrapper _listenDockerCommand;
		
		[NonSerialized]
		public Promise receivedAnyDockerStateYet = new Promise();

		[NonSerialized]
		private Dictionary<string, ProjectLogsWrapper> _serviceToLogCommand =
			new Dictionary<string, ProjectLogsWrapper>();

		[NonSerialized]
		private Dictionary<string, ServiceCliAction> _serviceToAction = new Dictionary<string, ServiceCliAction>();
		
		[NonSerialized]
		private Dictionary<string, Promise> _serviceToLogPromise = new Dictionary<string, Promise>();

		[NonSerialized]
		private Promise _watchPromise;

		private MicroserviceReflectionCache.Registry _microserviceCache;
		public MigrationPlan migrationPlan;
		

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
			BeamCommands cli, 
			BeamableDispatcher dispatcher,
			ReflectionCache editorCache)
		{
			_microserviceCache = editorCache.GetFirstSystemOfType<MicroserviceReflectionCache.Registry>();
			_dispatcher = dispatcher;
			_cli = cli;
			// microserviceCache.Cache.
			// SolutionPostProcessor.OnPreGeneratingCSProjectFiles();
		}

		public bool TryGetStatus(string beamoId, out BeamServiceStatus status)
		{
			status = null;
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
		}
		
		
		
		public async Promise SetMicroserviceChanges(string serviceName, List<AssemblyDefinitionAsset> assemblyDefinitions, List<string> dependencies)
		{
			var service = latestManifest.services.FirstOrDefault(s => s.beamoId == serviceName);
			if (service == null)
			{
				throw new ArgumentException($"Invalid service name was passed: {serviceName}");
			}

			UsamLogger.Log($"Starting updating storage dependencies");
			await UpdateServiceStoragesDependencies(service, dependencies);

			await UpdateServiceReferences(service, assemblyDefinitions);

			UsamLogger.Log($"Finished updating microservice [{serviceName}] data");
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
		
		public async Promise UpdateServiceReferences(BeamManifestServiceEntry service, List<AssemblyDefinitionAsset> assemblyDefinitions, bool shouldRefresh = true)
		{
			UsamLogger.Log($"Starting updating references");

			var pathsList = new List<string>();
			var namesList = new List<string>();
			foreach (AssemblyDefinitionAsset asmdef in assemblyDefinitions)
			{
				namesList.Add(asmdef.name);
				var pathFromRootFolder = CsharpProjectUtil.GenerateCsharpProjectFilename(asmdef.name);
				var pathToService = service.csprojPath;
				pathsList.Add(PackageUtil.GetRelativePath(pathToService, pathFromRootFolder));
			}
			
			var updateCommand = _cli.UnityUpdateReferences(new UnityUpdateReferencesArgs()
			{
				service = service.beamoId,
				paths = pathsList.ToArray(),
				names = namesList.ToArray()
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
					ListenForLogs(status.service);
				}

			});
			var _ = _watchCommand.Run();
			// TODO: add re-try logic
		}

		public void ListenForLogs(string service)
		{
			if (_serviceToLogCommand.TryGetValue(service, out _))
			{
				return;
			}
			
			var logCommand = _cli.ProjectLogs(new ProjectLogsArgs
			{
				service = new ServiceName(service), reconnect = true
			});
			logCommand.OnStreamTailLogMessageForClient(cb =>
			{
				if (!TryGetLogs(service, out var logs))
				{
					logs = new NamedLogView
					{
						beamoId = service, 
						logs = new List<CliLogMessage>(), 
						logView = new LogView()
					};
					_namedLogs.Add(logs);
				}
				
				// TODO: implement max size of log?
				
				logs.logs.Add(new CliLogMessage
				{
					message = cb.data.message,
					timestamp = cb.ts,
					logLevel = cb.data.logLevel
				});
			});
			var promise = logCommand.Run();

			// TODO: retry logic.
			_serviceToLogPromise[service] = promise;
			_serviceToLogCommand[service] = logCommand;
		}

		public void Reload()
		{
			var _ = WaitReload();
		}
		
		public Promise WaitReload()
		{
			LoadLegacyServices();

			var command = _cli.UnityManifest();
			command.OnStreamShowManifestCommandOutput(cb =>
			{
				hasReceivedManifestThisDomain = true;
				latestManifest = cb.data;
				CsProjUtil.OnPreGeneratingCSProjectFiles(this);
			});
			
			var p = command.Run();
			ListenForStatus();
			ListenForDocker();
			return p;
		}

		void LoadLegacyServices()
		{
			migrationPlan = UsamMigrator.CreatePlan(_microserviceCache);
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
					new ProjectOpenSwaggerArgs() { remote = remote, serviceName = new ServiceName(beamoId) });
				var _ = cmd.Run();
			}
			catch (Exception e)
			{
				UsamLogger.Log(e.GetType().Name, e.Message, e.StackTrace);
			}
		}

		public void OpenProject(string beamoId, string projectPath)
		{
			
			var sln = SERVICES_SLN_PATH;
			var fileName = $@"{Path.GetDirectoryName(projectPath)}/{beamoId}.cs";
			
			
			// first open the sln, because in most IDEs multi-solution view is not supported. 
			EditorUtility.OpenWithDefaultApp(sln);
			
			// and once enough time has passed, hopefully enough so that the IDE has focused
			//  the solution; open the actual sub class file.
			IEnumerator OpenFile()
			{
				const float hopefullyEnoughTimeForIDEToInitialize = .5f;
				yield return new WaitForSecondsRealtime(hopefullyEnoughTimeForIDEToInitialize);
				EditorUtility.OpenWithDefaultApp(fileName);
			}
			_dispatcher.Run("open-code", OpenFile());
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
					logs = new List<CliLogMessage>(), beamoId = beamoId, logView = new LogView()
				};
				_namedLogs.Add(log);
			}
				
			log.logs.Add(new CliLogMessage
			{
				message = logMessage.message.TrimStart(new char[]{' ', '\n', '\r'}),
				logLevel = logMessage.logLevel,
				timestamp = logMessage.timestamp
			});
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
			stopCommand.Run();
		}

		void StartService(BeamManifestServiceEntry service)
		{
			var runCommand = _cli.ProjectRun(new ProjectRunArgs
			{
				detach = true, ids = new string[] {service.beamoId}, watch = false, noClientGen = true,
			});
			var action = SetServiceAction(service.beamoId, ServiceCliActionType.Running, runCommand);

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
			var stopCommand = _cli.ServicesStop(new ServicesStopArgs()
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
			var runCommand = _cli.ServicesRun(new ServicesRunArgs
			{
				ids = new string[]{storage.beamoId},
			});
			var action = SetServiceAction(storage.beamoId, ServiceCliActionType.Running, runCommand);

			runCommand.OnLocal_progressServiceRunProgressResult(cb =>
			{
				action.progressRatio = (float)cb.data.LocalDeployProgress;
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

		public void DeleteProject(string beamoId, string csProjPath)
		{
			// TODO: Delete auto generated client. 

			_cli.ProjectRemove(new ProjectRemoveArgs {sln = SERVICES_SLN_PATH, ids = new string[] {beamoId}})
			    .OnStreamDeleteProjectCommandOutput(_ =>
			    {
				    Reload();
			    })
			    .Run();
			
		}

		public void CreateStorage(string newStorageName, List<string> dependencies)
		{
			var command = _cli.ProjectNewStorage(new ProjectNewStorageArgs()
			{
				name = new ServiceName(newStorageName),
				sln = SERVICES_SLN_PATH,
				serviceDirectory = SERVICES_FOLDER,
				linkTo = dependencies.ToArray()
			});
			
			// mock out the expected results in the latestManifest&status
			var mockService = new BeamManifestServiceEntry
			{
				beamoId = newStorageName,
				// TODO: other fields?
			};
			latestManifest.services.Add(mockService);
			var mockStatus = new BeamServiceStatus
			{
				service = newStorageName, serviceType = "storage",
				availableRoutes = new List<BeamServicesForRouteCollection>()
			};
			latestStatus.Add(mockStatus);
			
			var action = SetServiceAction(newStorageName, ServiceCliActionType.Creating, command);

			var logCount = 0;
			AddLog(mockService, new CliLogMessage
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
				AddLog(mockService, cb.data);
			});
			

			command.Run().Then(_ =>
			{
				Reload();
				action.progressRatio = 1;
				action.isComplete = true;
				AddLog(mockService, new CliLogMessage
				{
					logLevel = "Info",
					message = $"Created service {newStorageName}",
					timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
				});
			}).Error(_ =>
			{
				action.isFailed = true;
			});
		}
		
		public void CreateService(string newServiceName, List<string> dependencies)
		{
			var command = _cli.ProjectNewService(new ProjectNewServiceArgs
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
				// TODO: other fields?
			};
			latestManifest.services.Add(mockService);
			var mockStatus = new BeamServiceStatus
			{
				service = newServiceName, serviceType = "service",
				availableRoutes = new List<BeamServicesForRouteCollection>()
			};
			latestStatus.Add(mockStatus);
			
			var action = SetServiceAction(newServiceName, ServiceCliActionType.Creating, command);

			var logCount = 0;
			AddLog(mockService, new CliLogMessage
			{
				logLevel = "Info",
				message = $"Creating service {newServiceName}",
				timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
			});
			action.progressRatio = .1f;
			command.OnLog(cb =>
			{
				if (!cb.data.logLevel.ToLowerInvariant().StartsWith("i")) return;
				logCount++;
				action.progressRatio = logCount / 4f;
				AddLog(mockService, cb.data);
			});
			

			command.Run().Then(_ =>
			{
				Reload();
				action.progressRatio = 1;
				action.isComplete = true;
				AddLog(mockService, new CliLogMessage
				{
					logLevel = "Info",
					message = $"Created service {newServiceName}",
					timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
				});
			}).Error(_ =>
			{
				action.isFailed = true;
			});
		}
	}
}
