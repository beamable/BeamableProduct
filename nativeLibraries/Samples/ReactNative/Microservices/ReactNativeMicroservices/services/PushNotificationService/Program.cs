using Beamable.Server;
using System.Threading.Tasks;

namespace Beamable.PushNotificationService
{
	public class Program
	{
		/// <summary>
		/// The entry point for the <see cref="PushNotificationService"/> service.
		/// </summary>
		public static async Task Main()
		{
			await BeamServer
				.Create()
				.IncludeRoutes<PushNotificationService>(routePrefix: "")
				.RunForever();
		}
	}
}
