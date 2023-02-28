using System.Diagnostics;

namespace StressTest;

public class Program
{
	public static void Main()
	{
		// Console.WriteLine("Hello");
		(new StandaloneMicroserviceTesting()).Run();
		// sudo dotnet-counters collect --refresh-interval 3 --format csv -- standalone-microservice.dll

	}
}
