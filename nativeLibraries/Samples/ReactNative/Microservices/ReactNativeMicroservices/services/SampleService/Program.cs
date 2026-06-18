using Beamable.Server;
using System.Threading.Tasks;

namespace Beamable.SampleService
{
	public class Program
	{
		/// <summary>
		/// The entry point for the <see cref="SampleService"/> service.
		/// </summary>
		public static async Task Main()
		{
			await BeamServer
				.Create()
				.IncludeRoutes<SampleService>(routePrefix: "")
				.RunForever();
		}
	}
}
