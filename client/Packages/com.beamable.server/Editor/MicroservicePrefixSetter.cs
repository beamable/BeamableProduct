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
			if (DockerCommand.DockerNotInstalled)
			{
				return string.Empty;
			}

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

			return string.Empty;

		}
	}

	// [InitializeOnLoadAttribute]
	[Obsolete("This type is not used anymore.")]
	public class MicroservicePrefixSetter
	{
	}
}
