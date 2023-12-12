using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Semantics;
using Beamable.Editor;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Dotnet;
using Beamable.Editor.UI.Model;
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

		public Promise OnReady { get; private set; }
		public Action<string, BeamTailLogMessage> OnLogMessage;
		public bool IsDockerRunning { get; private set; }

		public List<IBeamoServiceDefinition> ServiceDefinitions { get; private set; } =
			new List<IBeamoServiceDefinition>();

		private static readonly List<string> IgnoreFolderSuffixes = new List<string> { "~", "obj", "bin" };
		private List<BeamServiceSignpost> _services;
		private List<Promise> _logsCommands = new List<Promise>();

		private const string BEAMABLE_PATH = "Assets/Beamable/";
		private static readonly string StandaloneMicroservicesPath = $"{BEAMABLE_PATH}StandaloneMicroservices~/";

		public CodeService(BeamCommands cli, BeamableDispatcher dispatcher)
		{
			_cli = cli;
			_dispatcher = dispatcher;
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

			if (string.IsNullOrEmpty(version?.version) || version.version.Contains("0.0.0"))
			{
				LogVerbose("Could not detect current version, skipping");
				return;
			}

			var versions = _cli.ProjectVersion(new ProjectVersionArgs { requestedVersion = version?.version });
			versions.OnStreamProjectVersionCommandResult(result =>
			{
				LogVerbose($"Versions updated: {result.data.packageVersions[0]}");
			});
			await versions.Run().Error(LogExceptionVerbose);
		}

		public async Promise RefreshServices()
		{
			LogVerbose("refresh services start");
			try
			{
				var ps = _cli.ServicesPs(new ServicesPsArgs() { json = false, remote = true });
				ps.OnStreamServiceListResult(cb =>
				{
					IsDockerRunning = cb.data.IsDockerRunning;
					LogVerbose($"Found {cb.data.BeamoIds.Count} remote services");
					_dispatcher.Schedule(() => PopulateData(cb.data));
				});
				await ps.Run();
			}
			catch
			{
				LogVerbose("Could not list remote services, skip", true);
				return;
			}

			if (!IsDockerRunning)
			{
				LogVerbose("Docker is not running, skip", true);
				return;
			}

			try
			{
				var ps = _cli.ServicesPs(new ServicesPsArgs { json = false, remote = false });
				ps.OnStreamServiceListResult(cb =>
				{
					IsDockerRunning = cb.data.IsDockerRunning;
					LogVerbose($"Found {cb.data.BeamoIds.Count} local services");
					_dispatcher.Schedule(() => PopulateData(cb.data));
				});
				await ps.Run();
			}
			catch
			{
				LogVerbose("Could not list local services, skip", true);
				return;
			}

			LogVerbose("refresh services end");
		}

		private void PopulateData(BeamServiceListResult objData)
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
							dockerfilePath = service?.relativeDockerFile
						}
					});
					dataIndex = ServiceDefinitions.Count - 1;
					ServiceDefinitions[dataIndex].Builder = new BeamoServiceBuilder() { BeamoId = name };
				}

				ServiceDefinitions[dataIndex].ShouldBeEnabledOnRemote = objData.ShouldBeEnabledOnRemote[i];
				if (objData.IsLocal)
				{
					ServiceDefinitions[dataIndex].ImageId = objData.ImageIds[i];
					ServiceDefinitions[dataIndex].Builder.IsRunning = objData.RunningState[i];
				}
				else
				{
					ServiceDefinitions[dataIndex].IsRunningOnRemote =
						objData.RunningState[i] ? BeamoServiceStatus.Running : BeamoServiceStatus.NotRunning;
				}
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
			var args = new ProjectNewArgs {solutionName = service, quiet = true, name = service, output = outputPath};
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
			
			LogVerbose($"Finished creation of service {serviceName}");
		}

		public async Promise GenerateClientCode(string id)
		{
			//TODO change this to receive dotnet service from the CodeService initialization
			var dotnetService = BeamEditorContext.Default.ServiceScope.GetService<DotnetService>();

			var path = $"Assets/Beamable/StandaloneMicroservices~/{id}/";
			var buildCommand = $"build {path}";
			await dotnetService.Run(buildCommand);
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
				logs.OnStreamTailLogMessage(point =>
				{
					_dispatcher.Schedule(() => OnLogMessage?.Invoke(definition.BeamoId, point.data));
				});
				_logsCommands.Add(logs.Run());
			}
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

		public static async Promise SetManifest(BeamCommands cli, List<BeamServiceSignpost> files)
		{
			var args = new ServicesSetLocalManifestArgs();
			args.localHttpNames = new string[files.Count];
			args.localHttpContexts = new string[files.Count];
			args.localHttpDockerFiles = new string[files.Count];
			// TODO: add some validation to check that these files actually make sense
			for (var i = 0; i < files.Count; i++)
			{
				args.localHttpNames[i] = files[i].name;
				args.localHttpContexts[i] = files[i].assetRelativePath;
				args.localHttpDockerFiles[i] = files[i].relativeDockerFile;
			}

			var command = cli.ServicesSetLocalManifest(args);
			await command.Run().Error(LogExceptionVerbose);
		}

		public static List<BeamServiceSignpost> GetBeamServices()
		{
			var files = GetSignpostFiles(".beamservice");
			var data = GetSignpostData<BeamServiceSignpost>(files);
			return data;
		}

		public static List<T> GetSignpostData<T>(IEnumerable<string> files)
		{
			var output = new List<T>();
			foreach (var file in files)
			{
				var json = File.ReadAllText(file);
				var data = JsonUtility.FromJson<T>(json);
				output.Add(data);
			}

			return output;
		}

		private static IEnumerable<string> GetSignpostFiles(string extension)
		{
			var files = new HashSet<string>();

			ScanDirectoryRecursive("Assets", extension, IgnoreFolderSuffixes, files);
			ScanDirectoryRecursive("Packages", extension, IgnoreFolderSuffixes, files);
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

		public async Promise Run(IEnumerable<string> beamoIds)
		{
			try
			{
				var cmd = _cli.ServicesRun(new ServicesRunArgs() { ids = beamoIds.ToArray() });
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
