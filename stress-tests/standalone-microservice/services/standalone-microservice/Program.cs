using Beamable.Server;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Beamable.standalone_microservice
{
	public class Program
	{
		/// <summary>
		/// The entry point for the <see cref="StandaloneMicroservice"/> service.
		/// </summary>
		public static async Task Main()
		{
			Environment.SetEnvironmentVariable("DOTNET_DiagnosticPorts", null);
			await MicroserviceBootstrapper.Prepare<StandaloneMicroservice>();
			
			// run the Microservice code
			await MicroserviceBootstrapper.Start<StandaloneMicroservice>();
		}
		
	}
}
