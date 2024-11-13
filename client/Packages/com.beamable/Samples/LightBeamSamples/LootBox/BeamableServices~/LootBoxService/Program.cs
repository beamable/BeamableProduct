using Beamable.Server;
using System.Threading.Tasks;

namespace Beamable.LootBoxService
{
	public class Program
	{
		/// <summary>
		/// The entry point for the <see cref="LootBoxService"/> service.
		/// </summary>
		public static async Task Main()
		{
			// inject data from the CLI.
			await MicroserviceBootstrapper.Prepare<LootBoxService>();
			
			// run the Microservice code
			await MicroserviceBootstrapper.Start<LootBoxService>();
		}
	}
}
