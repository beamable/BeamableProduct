// using Beamable.Common;
// using Beamable.Common.BeamCli;
// using Beamable.Common.Dependencies;
// using Beamable.Editor.BeamCli;
// using Beamable.Editor.BeamCli.Commands;
// using Beamable.Editor.BeamCli.Extensions;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
//
// namespace Beamable.Server.Editor
// {
// 	public class MicroserviceDiscovery : IBeamableDisposable, ILoadWithContext
// 	{
//
// 		private Promise _gotAnyDataPromise;
//
// 		private Dictionary<string, HashSet<string>> _nameToAvailableRoutingKeys = new Dictionary<string, HashSet<string>>();
// 		private ProjectPsWrapper _command;
//
// 		public MicroserviceDiscovery()
// 		// public MicroserviceDiscovery(IBeamableRequester requester, BeamCli cli)
// 		{
//
// 			Start().Error(Debug.LogError);
// 		}
//
// 		public async Promise Start()
// 		{
// 			await BeamEditorContext.Default.InitializePromise;
// 			var cli = BeamEditorContext.Default.ServiceScope.GetService<BeamCli>();
//
// 			_command = cli.Command.ProjectPs(new ProjectPsArgs());
// 			_command.OnStreamCheckStatusServiceResult(Handle);
//
// 			_gotAnyDataPromise = new Promise();
//
// 			var available = await cli.IsAvailable();
// 			if (!available)
// 			{
// 				_gotAnyDataPromise.CompleteSuccess();
// 				return;
// 			}
// 			await _command.Run();
// 		}
//
// 		void Handle(ReportDataPoint<BeamCheckStatusServiceResult> data)
// 		{
// 			var status = data.data;
// 			_nameToAvailableRoutingKeys.Clear();
//
// 			foreach (var service in status.services)
// 			{
// 				if (service.serviceType != "service") continue;
// 				
// 				foreach (var route in service.availableRoutes)
// 				{
// 					var hasLocal = route.instances.Any(i => i.IsLocal());
// 					if (!hasLocal) continue; // skip this route, because it is not a local instance. 
//
// 					if (!_nameToAvailableRoutingKeys.TryGetValue(service.service, out var routingKeys))
// 					{
// 						routingKeys = _nameToAvailableRoutingKeys[service.service] = new HashSet<string>();
// 					}
//
// 					routingKeys.Add(route.routingKey);
// 				}
// 			}
// 			_gotAnyDataPromise.CompleteSuccess();
// 		}
// 		
// 		public bool TryIsRunning(string serviceName, out string prefix)
// 		{
// 			prefix = null;
// 			if (_nameToAvailableRoutingKeys.TryGetValue(serviceName, out var routingKeys))
// 			{
// 				prefix = routingKeys.FirstOrDefault() ?? "";
// 				return true;
// 			}
//
// 			return false;
// 		}
//
// 		public Promise OnDispose()
// 		{
// 			return Promise.Success;
// 		}
//
// 	}
// }
