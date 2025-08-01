using Beamable.Common;
using Beamable.Editor.BeamCli.Commands;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beamable.Common.BeamCli.Contracts;
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	public partial class UsamService
	{
		public string GetKeyForAutoGenerateClientSetting(string beamoId)
		{
			return $"beam.{_ctx.BeamCli.Cid}.{_ctx.BeamCli.Pid}.{beamoId}.enableClientAutoGen";
		}
		
		
		public bool ShouldServiceAutoGenerateClient(string beamoId)
		{
			var key = GetKeyForAutoGenerateClientSetting(beamoId);
			return EditorPrefs.GetBool(key, true);
		}

		public void ToggleServiceAutoGenerateClient(string beamoId)
		{
			var key = GetKeyForAutoGenerateClientSetting(beamoId);
			var existing = EditorPrefs.GetBool(key, true);
			EditorPrefs.SetBool(key, !existing);
		}
		
		public async Promise GenerateClient(BeamManifestServiceEntry service)
		{
			if (_generateClientCommand != null)
			{
				_generateClientCommand.Cancel();
				_generateClientCommand = null;
			}

			AddLog(service.beamoId, CliLogMessage.FromStringNow("Starting client generation..."));

			var idArr = new string[] {service.beamoId};
			
			// the client-code generation requires a built dll; so building the latest code
			//  ensures we have the latest client generated
			AddLog(service.beamoId, CliLogMessage.FromStringNow(" building latest code..."));

			await Build(new ProjectBuildArgs {ids = idArr});

			AddLog(service.beamoId, CliLogMessage.FromStringNow(" generating client code..."));
			if (!ShouldServiceAutoGenerateClient(service.beamoId))
			{
				// the auto-gen is not going to pick this up, so we need to generate it manually.
				if (_config.UseOldMicroserviceGenerator)
				{
					await GenerateClient(new ProjectGenerateClientArgs
					{
						outputLinks = true,
						ids = idArr,
						exactIds = true,
						// the interface around this command has not aged well.
						//  it actually generates clients for ALL services
						source = "not-important"
					});
				}
				else
				{
					var args = new ProjectGenerateClientOapiArgs
					{
						outputDir = Path.Combine(Application.dataPath, "Beamable", "Autogenerated", "Microservices"),
						ids = idArr,
						exactIds = true,
					};
					await GenerateClient(args);
				}
				
			}
			
			// _config.UseOldMicroserviceGenerator ? GenerateClient() : GenerateClientFromOAPI();
		}

		async Promise GenerateClient(ProjectGenerateClientArgs args)
		{
			var outputs = new List<BeamGenerateClientFileEvent>();

			{ // filter ids by those services that are actually writable.
				// if a service is readonly, then it should already have a client alongside it.
				args.ids = args.ids.Where(x =>
				{
					var service = latestManifest.services.FirstOrDefault(s => s.beamoId == x);
					var isWritable = service != null && !service.IsReadonlyPackage;
					return isWritable;
				}).ToArray();
			}
			
			{ // update the hint path structure
				var hints = new List<string>();
				foreach (var kvp in _assemblyUtil.beamoIdToClientHintPath)
				{
					hints.Add($"{kvp.Key}={kvp.Value}");
				}

				args.outputPathHints = hints.ToArray();
			}

			_generateClientCommand = _webCommandFactory.processCommands.ProjectGenerateClient(args);
			_generateClientCommand.OnError(cb =>
			{
				Debug.LogError($"Failed to generate clients. message=[{cb.data.message}] ");
			});
			if (args.ids?.Length == 1)
			{
				_generateClientCommand.OnLog(cb =>
				{
					AddLog(args.ids[0], cb.data);
				});
			}
		
			
			_generateClientCommand.OnStreamGenerateClientFileEvent(cb =>
			{
				outputs.Add(cb.data);
			});
			
			await _generateClientCommand.Run();

			if (outputs.Count > 0)
			{
				AssetDatabase.Refresh();
			}
		}
		
		
		public async Promise GenerateClient()
		{
			var idArr = latestManifest.services
			                          .Where(x => ShouldServiceAutoGenerateClient(x.beamoId))
			                          .Select(x => x.beamoId)
			                          .ToArray();
			await GenerateClient(new ProjectGenerateClientArgs
			{
				outputLinks = true,
				ids = idArr,
				exactIds = true,
				// the interface around this command has not aged well.
				//  it actually generates clients for ALL services
				source = "not-important"
			});
		}

		async Promise GenerateClient(ProjectGenerateClientOapiArgs args)
		{
			_generateClientOapiWrapper = _webCommandFactory.processCommands.ProjectGenerateClientOapi(args);
			_generateClientOapiWrapper.OnError(cb =>
			{
				Debug.LogError($"Failed to generate clients. message=[{cb.data.message}] ");
			});
			
			await _generateClientOapiWrapper.Run();
			
			AssetDatabase.Refresh();
			
		}
		
		public async Promise GenerateClientFromOAPI()
		{
			var idArr = latestManifest.services
			                          .Where(x => ShouldServiceAutoGenerateClient(x.beamoId))
			                          .Select(x => x.beamoId)
			                          .ToArray();
			var args = new ProjectGenerateClientOapiArgs
			{
				outputDir = Path.Combine(Application.dataPath, "Beamable", "Autogenerated", "Microservices"),
				ids = idArr,
				exactIds = true,
			};
			await GenerateClient(args);
		}


		public void ListenForBuildChanges()
		{
			latestListenTaskId++;
			var beamoIdToWriteTime = new Dictionary<string, long>();

			_dispatcher.Run("usam-build-generation", Run(latestListenTaskId));
			
			IEnumerator Run(int taskId)
			{ //TODO THis is triggering again twice after domain reload, need to investigate it
				var updates = new Dictionary<string, long>();
				while (taskId == latestListenTaskId)
				{
					yield return new WaitForSecondsRealtime(.5f); // wait half a second...

					if (latestManifest?.services == null) continue;


					var anyUpdates = false;
					updates.Clear();
					foreach (var service in latestManifest.services)
					{
						if (service.IsReadonlyPackage) continue; // changes in read-only projects are ignored. 
						
						var path = service.buildDllPath;
						if (string.IsNullOrEmpty(path)) continue;

						if (!File.Exists(path)) continue;
						
						var writeTime = File.GetLastWriteTime(path).ToFileTime();
						if (beamoIdToWriteTime.TryGetValue(service.beamoId, out var lastWriteTime))
						{
							if (writeTime > lastWriteTime)
							{
								anyUpdates = true;
							}
						}
						else
						{
							anyUpdates = true;
						}

					}

					if (anyUpdates)
					{
						var commandPromise = _config.UseOldMicroserviceGenerator ? GenerateClient() : GenerateClientFromOAPI();
						yield return commandPromise.ToYielder();

						foreach (var service in latestManifest.services)
						{
							var path = service.buildDllPath;
							if (string.IsNullOrEmpty(path)) continue;
							if (!File.Exists(path)) continue;
						
							var writeTime = File.GetLastWriteTime(path).ToFileTime();
							beamoIdToWriteTime[service.beamoId] = writeTime;
						}
					}
				}
			}
		}
	}
}
