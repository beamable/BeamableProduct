
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Beamable.Server;
using Serilog;
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
