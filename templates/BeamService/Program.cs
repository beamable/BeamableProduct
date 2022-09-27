using Beamable.Server;
using System.Reflection;

namespace Beamable.BeamService
{
	public class Program
	{
		public static void Main(string[] _)
		{
			Console.Out.WriteLine("Hello world: " + Assembly.GetEntryAssembly().GetName().Version);
		}
		// public static async Task Main(string[] _)
		// {
		// 	await MicroserviceBootstrapper.Start<BeamService>();
		// }
	}
}
