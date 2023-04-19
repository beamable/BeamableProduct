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
	public int rps;
	public int duration;
	public string authHeader;
	public bool includeAuth;
}

public class RunNBomberCommand : AppCommand<RunNBomberCommandArgs>
{
	public RunNBomberCommand() : base("run-nbomber", "Runs an n-bomber stress test for a given microservice method")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("service", "The name of the microservice to stress test"), (args, i) => args.service = i);
		AddArgument(new Argument<string>("method", "The method name in the service to stress test"), (args, i) => args.method = i);
		AddArgument(new Argument<string>("body", "The json body for each request"), (args, i) => args.jsonBody = i);
		AddOption(new Option<bool>("--include-prefix", () => true, "If true, the generated .env file will include the local machine name as prefix"), (args, i) => args.includePrefix = i);
		AddOption(new Option<bool>("--include-auth", () => true, "If true, the requests will be given the CLI's auth token"), (args, i) => args.includePrefix = i);
		AddOption(new Option<int>("--rps", () => 50, "The requested requests per second for the test"), (args, i) => args.rps = i);
		AddOption(new Option<int>("--duration", () => 30, "How long to run the test for"), (args, i) => args.duration = i);
		AddOption(new Option<string>("--auth", "Include an auth header. This will override the --include-auth flag"), (args, i) => args.authHeader = i);
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

				if (args.includeAuth)
				{
					request = request.WithHeader("Authorization", $"Bearer {args.AppContext.Token.Token}");
				}
				if (!string.IsNullOrEmpty(args.authHeader))
				{
					request = request.WithHeader("Authorization", "Bearer 5d938ebe-9cbd-41d6-b077-d8dd89b99819");
				}
				
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

			.WithLoadSimulations(Simulation.Inject(rate: args.rps, interval: TimeSpan.FromSeconds(1),
				during: TimeSpan.FromSeconds(args.duration)))
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
