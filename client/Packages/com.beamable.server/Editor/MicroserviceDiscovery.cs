using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Server.Editor
{
	public class MicroserviceDiscovery : IBeamableDisposable, ILoadWithContext
	{

		private Promise _gotAnyDataPromise;

		private Dictionary<string, BeamServiceDiscoveryEvent> _nameToStatus = new Dictionary<string, BeamServiceDiscoveryEvent>();
		private ProjectPsWrapper _command;

		public MicroserviceDiscovery()
		// public MicroserviceDiscovery(IBeamableRequester requester, BeamCli cli)
		{

			Start().Error(Debug.LogError);
		}

		public async Promise Start()
		{
			await BeamEditorContext.Default.InitializePromise;
			var cli = BeamEditorContext.Default.ServiceScope.GetService<BeamCli>();

			_command = cli.Command.ProjectPs(new ProjectPsArgs());
			_command.OnStreamServiceDiscoveryEvent(Handle);

			_gotAnyDataPromise = new Promise();

			var available = await cli.IsAvailable();
			if (!available)
			{
				_gotAnyDataPromise.CompleteSuccess();
				return;
			}
			await _command.Run();
		}

		void Handle(ReportDataPoint<BeamServiceDiscoveryEvent> data)
		{
			var entry = data.data;
			if (entry.isRunning)
			{
				_nameToStatus[entry.service] = entry;
			}
			else
			{
				_nameToStatus.Remove(entry.service);
			}
			_gotAnyDataPromise.CompleteSuccess();
		}

		public bool TryIsRunning(string serviceName, out string prefix)
		{
			prefix = null;
			if (_nameToStatus.TryGetValue(serviceName, out var status))
			{
				prefix = status.prefix;
				return true;
			}

			return false;
		}

		public Promise OnDispose()
		{
			return Promise.Success;
		}

	}
}
