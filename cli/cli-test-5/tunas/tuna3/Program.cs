using Beamable.Server;
using System.Threading.Tasks;

namespace Beamable.tuna3
{
	public class Program
	{
		/// <summary>
		/// The entry point for the microservice.
		/// </summary>
		public static async Task Main()
		{
			// load up the environment from .env
			DotEnv.Load();
			
			// run the Microservice code
			await MicroserviceBootstrapper.Start<tuna3>();
		}
	}
}
