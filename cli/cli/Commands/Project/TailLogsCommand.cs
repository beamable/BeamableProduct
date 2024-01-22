using Beamable.Common.Semantics;
using cli.Services;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;

#pragma warning disable CS0649
// ReSharper disable InconsistentNaming

namespace cli.Commands.Project;

public class TailLogsCommandArgs : CommandArgs
{
	public bool reconnect;
	public ServiceName service;
}


public class TailLogMessage
{
	[JsonProperty("__t")]
	public string timeStamp;

	[JsonProperty("__m")]
	public string message;

	[JsonProperty("__l")]
	public string logLevel;

	[JsonProperty("__raw")]
	public string raw;
}

public class TailLogsCommand : StreamCommand<TailLogsCommandArgs, TailLogMessage>
{
	public TailLogsCommand() : base("logs", "Tail the logs of a microservice")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<ServiceName>("service", "The name of the service to view logs for"),
			(args, i) => args.service = i);
		AddOption(new Option<bool>("--reconnect", getDefaultValue: () => true, "If the service stops, and reconnect is enabled, then the logs command will wait for the service to restart and then reattach to logs"), (args, i) => args.reconnect = i);
	}

	public override async Task Handle(TailLogsCommandArgs args)
	{
		while (args.reconnect)
		{
			var discovery = args.DependencyProvider.GetService<DiscoveryService>();

			ServiceDiscoveryEvent startEvt = null;
			await foreach (var evt in discovery.StartDiscovery())
			{
				if (evt.service == args.service && evt.isRunning)
				{
					startEvt = evt;
					break;
				}
			}

			if (startEvt.isContainer)
			{
				await TailDockerContainer(startEvt.containerId, args.BeamoLocalSystem);
			}
			else
			{
				await TailProcess(startEvt, args);
			}

			Log.Debug($"{args.service} has stopped.");

			await discovery.Stop();

		}
	}

	void HandleLog(string logMessage)
	{
		var parsed = JsonConvert.DeserializeObject<TailLogMessage>(logMessage);
		Log.Information($"[{parsed.logLevel}] {parsed.message}");
		parsed.raw = logMessage;
		SendResults(parsed);
	}


	async Task TailDockerContainer(string containerId, BeamoLocalSystem beamo)
	{
		await foreach (var line in beamo.TailLogs(containerId))
		{
			HandleLog(line);
		}
	}

	async Task TailProcess(ServiceDiscoveryEvent evt, TailLogsCommandArgs args)
	{
		using (var client = new HttpClient())
		{
			// Set up the HTTP GET request
			var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{evt.healthPort}/logs");

			// Set the "text/event-stream" media type to indicate Server-Sent Events
			request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

			// Send the request and get the response
			try
			{
				var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

				// Check if the response is successful
				if (response.IsSuccessStatusCode)
				{
					// Read the content as a stream
					using var stream = await response.Content.ReadAsStreamAsync();
					using var reader = new StreamReader(stream);
					while (!reader.EndOfStream)
					{
						var line = await reader.ReadLineAsync();
						var _ = await reader.ReadLineAsync(); // skip new line.
						HandleLog(line?.Substring("data: ".Length));
					}
				}
				else
				{
					Log.Error("Error: {0} - {1}", (int)response.StatusCode, response.ReasonPhrase);
				}
			}
			catch (HttpRequestException ex) when (ex.Message.StartsWith("Connection refused"))
			{
				Log.Debug("Service is not ready to accept connections yet... Retrying soon...");
			}
		}
	}

}
