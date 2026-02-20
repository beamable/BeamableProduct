using Beamable.Editor.BeamCli.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Editor.BeamCli.Extensions
{
	public static class DiscoveryExtensions
	{
		public static bool IsLocal(this BeamServiceInstance instance)
		{
			return (instance.latestHostEvent != null && instance.latestHostEvent.IsValid()) ||
			       (instance.latestDockerEvent != null && instance.latestDockerEvent.IsValid());
		}

		public static bool IsValid(this BeamDockerServiceDescriptor instance)
		{
			return !string.IsNullOrEmpty(instance.containerId) 
			       || !string.IsNullOrEmpty(instance.routingKey)
			       || !string.IsNullOrEmpty(instance.service)
			       || !string.IsNullOrEmpty(instance.serviceType); 
		}

		public static bool IsValid(this BeamHostServiceDescriptor instance)
		{
			return !string.IsNullOrEmpty(instance.service)
			       || !string.IsNullOrEmpty(instance.routingKey);
		}
		
		public static bool IsRemote(this BeamServiceInstance instance)
		{
			return instance.latestRemoteEvent != null;
		}
		
		public static bool HasAnyLocalInstances(this List<BeamServicesForRouteCollection> availableRoutes)
		{
			return availableRoutes.Any(a => a.instances.Any(i => i.IsLocal()));
		}
		public static bool HasAnyRemoteInstances(this List<BeamServicesForRouteCollection> availableRoutes)
		{
			return availableRoutes.Any(a => a.instances.Any(i => i.IsRemote()));
		}

		public static bool TryGetAvailableRoutesForService(this BeamCheckStatusServiceResult status, string beamoId, out List<BeamServicesForRouteCollection> availableRoutes)
		{
			availableRoutes = null;
			foreach (var service in status.services)
			{
				if (service.service == beamoId)
				{
					availableRoutes = service.availableRoutes;
					return true;
				}
			}
			return false;
		}
	}
}
