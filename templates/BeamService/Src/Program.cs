using Beamable.Server;
using System.Threading.Tasks;

namespace Beamable.BeamService
{
	public class Program
	{
		/// <summary>
		/// The entry point for the microservice.
		/// </summary>
		public static async Task Main()
		{
			// run the Microservice code
			await MicroserviceBootstrapper.Start<BeamService>();
		}
	}
}
