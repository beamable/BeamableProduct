
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Beamable.Server;
[assembly: MetadataUpdateHandler(typeof(HotReloadMetadataUpdateHandler))]

namespace Beamable.Server;

public static class HotReloadMetadataUpdateHandler
{
	public static int ReloadCount { get; private set; }

	public static List<BeamableMicroService> ServicesToRebuild = new List<BeamableMicroService>();

	public static void UpdateApplication(Type[] updatedTypes)
	{
		ReloadCount++;
		foreach (var service in ServicesToRebuild)
		{
			service.RebuildRouteTable();
		}
	}
}
