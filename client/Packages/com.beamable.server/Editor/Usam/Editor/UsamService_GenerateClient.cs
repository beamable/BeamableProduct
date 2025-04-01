using Beamable.Common;
using Beamable.Editor.BeamCli.Commands;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	public partial class UsamService
	{
		
		public bool ShouldServiceAutoGenerateClient(string beamoId)
		{
			if (!serviceToAutoGenClient.TryGetValue(beamoId, out var value))
			{
				return true;
			}

			return value;
		}

		public void ToggleServiceAutoGenerateClient(string beamoId)
		{
			var value = ShouldServiceAutoGenerateClient(beamoId);
			serviceToAutoGenClient[beamoId] = !value;
		}
		
		public async Promise GenerateClient(BeamManifestServiceEntry service)
		{
			if (_generateClientCommand != null)
			{
				_generateClientCommand.Cancel();
				_generateClientCommand = null;
			}

			var idArr = new string[] {service.beamoId};
			
			// the client-code generation requires a built dll; so building the latest code
			//  ensures we have the latest client generated
			await Build(new ProjectBuildArgs {ids = idArr});
			
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
				// the interface around this command has not aged well.
				//  it actually generates clients for ALL services
				source = "not-important"
			});
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
						var commandPromise = GenerateClient();
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
