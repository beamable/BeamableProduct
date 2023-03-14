using cli.Utils;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http;
using NBomber.Http.CSharp;
using System.CommandLine;

namespace cli;

public class RunNBomberCommandArgs : CommandArgs
{
	public string service;
	public string method;
	public string jsonBody;

	public bool includePrefix;
}

public class RunNBomberCommand : AppCommand<RunNBomberCommandArgs>
{
	public RunNBomberCommand() : base("run-nbomber", "runs an n-bomber stress test for a given microservice method. ")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("service", "the name of the microservice to stress test"), (args, i) => args.service = i);
		AddArgument(new Argument<string>("method", "the method name in the service to stress test"), (args, i) => args.method = i);
		AddArgument(new Argument<string>("body", "the json body for each request"), (args, i) => args.jsonBody = i);
		AddOption(new Option<bool>("--include-prefix", () => true, "if true, the generated .env file will include the local machine name as prefix"), (args, i) => args.includePrefix = i);
		// AddOption(new Option<string>("--host", "if set, the stress test will use the given host instead of the default"), (args, i) => args.hostOverride = i);
		// AddOption(new Option<string>("--cid", "if set, the stress test will use the given cid instead of the default"), (args, i) => args.cidOverride = i);
		// AddOption(new Option<string>("--pid", "if set, the stress test will use the given cid instead of the default"), (args, i) => args.cidOverride = i);
	}

	public override Task Handle(RunNBomberCommandArgs args)
	{
		using var httpClient = new HttpClient();

		var cid = args.AppContext.Cid;
		var pid = args.AppContext.Pid;
		var prefix = args.includePrefix ? MachineHelper.GetUniqueDeviceId() : "";
		var host = args.AppContext.Host;
		
		var url = $"{host}/basic/{cid}.{pid}.{prefix}micro_{args.service}/{args.method}";
		var scope = $"{cid}.{pid}";
		var scenario = Scenario.Create("simple_scenario", async context =>
			{
				var request =
					Http.CreateRequest("POST", url)
						.WithHeader("X-DE-SCOPE", scope)
						.WithBody(new StringContent(args.jsonBody));

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

			.WithLoadSimulations(Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1),
				during: TimeSpan.FromSeconds(30)))
		;

		NBomberRunner
			.RegisterScenarios(scenario)
			.WithReportFileName("report")
			.WithReportFolder("reports")
			.WithReportFormats(ReportFormat.Txt, ReportFormat.Csv, ReportFormat.Html, ReportFormat.Md)
			.Run();

		return Task.CompletedTask;
	}
}
