using cli.Commands.Project;
using Newtonsoft.Json;
using Serilog;

namespace cli.Services;

public class ProjectLogsService
{
	public static async Task Handle(TailLogsCommandArgs args, Action<TailLogMessage> handleLog, CancellationToken token = default)
	{
		void LogHandler(string logMessage)
		{
			var parsed = JsonConvert.DeserializeObject<TailLogMessage>(logMessage);
			parsed.raw = logMessage;
			handleLog(parsed);
		}

		
		while (args.reconnect && !token.IsCancellationRequested)
		{
			var discovery = args.DependencyProvider.GetService<DiscoveryService>();

			Task tailTask = null;
			// TODO: ignore remote, and also care about id filtering
			await foreach (var evt in discovery.StartDiscovery(args, default, token))
			{
				if (evt.Type != ServiceEventType.Running)
					continue;
				
				switch (evt)
				{
					case DockerServiceEvent dockerEvt:
						tailTask = TailDockerContainer(dockerEvt.descriptor, args.BeamoLocalSystem, LogHandler);
						break;
					case HostServiceEvent hostEvt:
						tailTask = TailProcess(hostEvt.descriptor, LogHandler);
						break;
				}

				if (tailTask != null)
				{
					break;
				}
			}

			if (tailTask == null)
			{
				Log.Verbose("no start event found");
			}
			else
			{
				await tailTask;
				Log.Verbose($"{args.service} has stopped.");
			}

			await discovery.Stop();

		}
	}

	async static Task TailDockerContainer(DockerServiceDescriptor container, BeamoLocalSystem beamo, Action<string> handleLog)
	{
		await foreach (var line in beamo.TailLogs(container.containerId))
		{
			if (!string.IsNullOrEmpty(line))
			{
				handleLog(line);
			}
		}
	}

	static async Task TailProcess(HostServiceDescriptor host, Action<string> handleLog)
	{
		using (var client = new HttpClient())
		{
			// Set up the HTTP GET request
			var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{host.healthPort}/logs");

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

						var substrLength = "data: ".Length;
						if (line?.Length > substrLength)
						{
							handleLog(line[substrLength..]);
						}
					}
				}
				else
				{
					Log.Error("Error: {0} - {1}", (int)response.StatusCode, response.ReasonPhrase);
				}
			}
			catch (HttpRequestException ex) when (ex.Message.StartsWith("Connection refused"))
			{
				Log.Verbose("Service is not ready to accept connections yet... Retrying soon...");
			}
		}
	}
}
