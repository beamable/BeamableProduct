using Beamable.Common;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.DockerCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using static Beamable.Common.Constants.Features.Services;

namespace Beamable.Server.Editor
{

	public class MicroservicePrefixService : IMicroservicePrefixService
	{
		private readonly MicroserviceDiscovery _discoveryService;
		private readonly MicroserviceReflectionCache.Registry _registry;
		private readonly Dictionary<string, MicroserviceDescriptor> _nameToDescriptor;
		public MicroservicePrefixService(MicroserviceDiscovery discoveryService)
		{
			_discoveryService = discoveryService;
			_registry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
			_nameToDescriptor = _registry.Descriptors.ToDictionary(d => d.Name);
		}


		public async Promise<string> GetPrefix(string serviceName)
		{
			if (_nameToDescriptor.TryGetValue(serviceName, out var descriptor))
			{
				var command = new CheckImageCommand(descriptor)
				{
					WriteLogToUnity = false
				};
				var isRunningInDocker = await command.Start();
				if (isRunningInDocker)
				{
					return MicroserviceIndividualization.Prefix;
				}
			}

			if (_discoveryService.TryIsRunning(serviceName, out var prefix))
			{
				return prefix;
			}
			else
			{
				return "";
			}
		}
	}

	// [InitializeOnLoadAttribute]
	[Obsolete("This type is not used anymore.")]
	public class MicroservicePrefixSetter
	{
		// register an event handler when the class is initialized
		static MicroservicePrefixSetter()
		{
			// EditorApplication.playModeStateChanged += LogPlayModeState;
		}

		private static async void LogPlayModeState(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
			{
				return;
			}

			if (DockerCommand.DockerNotInstalled) return;

			try
			{
				var microserviceRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
				foreach (var service in microserviceRegistry.Descriptors.ToList())
				{
					var command = new CheckImageCommand(service)
					{
						WriteLogToUnity = false
					};
					var isRunning = await command.Start();
					if (isRunning)
					{
						MicroserviceIndividualization.UseServicePrefix(service.Name);
					}
					else
					{
						if (state == PlayModeStateChange.EnteredPlayMode)
						{
							MicroserviceLogHelper.HandleLog(service, LogLevel.INFO, USING_REMOTE_SERVICE_MESSAGE,
								MicroserviceConfiguration.Instance.LogWarningLabelColor, true, "remote_icon");
						}

						MicroserviceIndividualization.ClearServicePrefix(service.Name);
					}
				}
			}
			catch (DockerNotInstalledException)
			{
				// purposefully do nothing... If docker isn't installed; do nothing...
			}
		}
	}
}
