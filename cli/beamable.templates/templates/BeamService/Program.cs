using Beamable.Server;
using System.Threading.Tasks;

namespace Beamable.BeamService
{
	public class Program
	{
		/// <summary>
		/// The entry point for the <see cref="BeamService"/> service.
		/// </summary>
		public static async Task Main()
		{
			// inject data from the CLI.
			await MicroserviceBootstrapper.Prepare<BeamService>();
			
			// run the Microservice code
			await MicroserviceBootstrapper.Start<BeamService>();
		}
	}
}
