using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Server;
using Newtonsoft.Json;
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
			await MicroserviceBootstrapper.Prepare<StandaloneMicroservice>(" . --auto-deploy --include-prefix=false --instance-count=10");
			
			// run the Microservice code
			await MicroserviceBootstrapper.Start<StandaloneMicroservice>();
		}
	}
}
