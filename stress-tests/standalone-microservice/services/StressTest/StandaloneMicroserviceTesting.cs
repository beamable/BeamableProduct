using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http;
using NBomber.Http.CSharp;

namespace StressTest;

public class StandaloneMicroserviceTesting
{
	public void Run()
	{
		using var httpClient = new HttpClient();

		var scenario = Scenario.Create("simple_add_scenario", async context =>
			{
				var request =
					Http.CreateRequest("GET", "https://dev.api.beamable.com/basic/1323424830305280.DE_1394663369397248.micro_standalone-microservice/Add")
						.WithHeader("X-DE-SCOPE", "1323424830305280.DE_1394663369397248") 
						.WithBody(new StringContent("{ \"a\":4,\"b\":2 }"));

				// HttpCompletionOption: https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpcompletionoption?view=net-7.0
            
				var clientArgs = new HttpClientArgs(
					httpCompletion: HttpCompletionOption.ResponseHeadersRead, // or ResponseHeadersRead
					cancellationToken: CancellationToken.None
				);

				var response = await Http.Send(httpClient, clientArgs, request);

				return response;
			})
			.WithoutWarmUp()
			.WithMaxFailCount(1)
			
			.WithLoadSimulations(Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)))
			;

		var res = NBomberRunner
			.RegisterScenarios(scenario)
			.WithReportFileName("report")                
			.WithReportFolder("reports")
			.WithReportFormats(ReportFormat.Txt, ReportFormat.Csv, ReportFormat.Html, ReportFormat.Md)
			.Run();
		
	}
}
