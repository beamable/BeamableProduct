using Beamable.Common;
using cli.Utils;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http;
using NBomber.Http.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.CommandLine;

namespace cli;

public class RunNBomberCommandArgs : CommandArgs
{
	public string service;
	public string method;
	public string jsonFilePath;

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
		AddOption(new Option<string>("--body", "The file path containing the json body for each request"), (args, i) => args.jsonFilePath = i);
		AddOption(new Option<bool>("--include-prefix", () => true, "If true, the generated .env file will include the local machine name as prefix"), (args, i) => args.includePrefix = i);
		AddOption(new Option<bool>("--include-auth", () => true, "If true, the requests will be given the CLI's auth token"), (args, i) => args.includeAuth = i);
		AddOption(new Option<int>("--rps", () => 50, "The requested requests per second for the test"), (args, i) => args.rps = i);
		AddOption(new Option<int>("--duration", () => 30, "How long to run the test for"), (args, i) => args.duration = i);
		AddOption(new Option<string>("--auth", "Include an auth header. This will override the --include-auth flag"), (args, i) => args.authHeader = i);
	}

	public override Task Handle(RunNBomberCommandArgs args)
	{
		var cid = args.AppContext.Cid;
		var pid = args.AppContext.Pid;
		var prefix = args.includePrefix ? MachineHelper.GetUniqueDeviceId() : "";
		var host = args.AppContext.Host;

		var url = $"{host}/basic/{cid}.{pid}.{prefix}micro_{args.service}/{args.method}";
		var scope = $"{cid}.{pid}";

		if (string.IsNullOrWhiteSpace(args.jsonFilePath))
			return Task.Run(() =>
				BeamableLogger.LogError(
					"No file specified as body for the request, try add `--body <your_file.json>` to the command"));

		if (!TryGetJsonContent(args.jsonFilePath, out string jsonContent))
			return Task.Run(() => BeamableLogger.LogError("File specified is not a valid json file"));

		if (!IsValidJson(ref jsonContent)) return Task.Run(() => BeamableLogger.LogError("File content is not a valid json"));

		var scenario = Scenario.Create("simple_scenario", async context =>
			{
				var request =
					Http.CreateRequest("POST", url)
						.WithHeader("X-DE-SCOPE", scope)
						.WithBody(new StringContent(jsonContent));
				if (args.includeAuth)
				{
					request = request.WithHeader("Authorization", $"Bearer {args.AppContext.Token.Token}");
				}
				if (!string.IsNullOrEmpty(args.authHeader))
				{
					request = request.WithHeader("Authorization", $"Bearer {args.authHeader}");
				}
				// HttpCompletionOption: https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpcompletionoption?view=net-7.0

				var clientArgs = new HttpClientArgs(
					httpCompletion: HttpCompletionOption.ResponseHeadersRead, // or ResponseHeadersRead
					cancellationToken: CancellationToken.None
				);

				using var httpClient = new HttpClient();

				var response = await Http.Send(httpClient, clientArgs, request);

				return response;
			})
			.WithoutWarmUp()
			.WithMaxFailCount(1)
			.WithLoadSimulations(Simulation.Inject(rate: args.rps, interval: TimeSpan.FromSeconds(1),
				during: TimeSpan.FromSeconds(args.duration)));

		NBomberRunner
			.RegisterScenarios(scenario)
			.WithReportFileName("report")
			.WithReportFolder("reports")
			.WithReportFormats(ReportFormat.Txt, ReportFormat.Csv, ReportFormat.Html, ReportFormat.Md)
			.Run();

		return Task.CompletedTask;
	}

	private static bool TryGetJsonContent(string filePath, out string jsonContent)
	{
		jsonContent = string.Empty;
		if (!IsJsonFile(filePath)) return false;
		try
		{
			jsonContent = ReadJsonFile(filePath);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static bool IsJsonFile(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath)) return false;
		string extension = Path.GetExtension(filePath);
		return extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
	}

	private static string ReadJsonFile(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath)) return string.Empty;
		try
		{
			string jsonContent = File.ReadAllText(filePath);
			return jsonContent;
		}
		catch (FileNotFoundException e)
		{
			BeamableLogger.LogError(e.Message);
			throw;
		}
	}

	private static bool IsValidJson(ref string jsonContent)
	{
		try
		{
			jsonContent = JToken.Parse(jsonContent).ToString();
			return true;
		}
		catch (JsonReaderException)
		{
			return false;
		}
	}
}
