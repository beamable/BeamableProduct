using Beamable.Server;
using cli.Commands.Project;
using Newtonsoft.Json;

namespace cli.Services;

public class ProjectLogsService
{
	public static async Task Handle(TailLogsCommandArgs args, Action<TailLogMessage> handleLog, CancellationTokenSource cts)
	{
		void LogHandler(string logMessage)
		{
			try
			{
				var parsed = JsonConvert.DeserializeObject<TailLogMessage>(logMessage);
				if (parsed.message == null)
				{
					var mongoParsed = JsonConvert.DeserializeObject<MongoLogMessage>(logMessage);
					parsed.message = mongoParsed.message;
					parsed.timeStamp = DateTimeOffset.Now.ToString("T");
					parsed.logLevel = "info";
				}

				parsed.raw = logMessage;
				handleLog(parsed);
			}
			catch (Exception ex)
			{
				// log an error, but continue parsing logs. 
				Log.Error($"Unable to parse log=[{logMessage}] message=[{ex.Message}]");
			}
		}

		/*
		 * the discovery never exits.
		 */

		while (!cts.IsCancellationRequested)
		{
			var discovery = args.DependencyProvider.GetService<DiscoveryService>();

			Task tailTask = null;
			// TODO: ignore remote, and also care about id filtering
			var discoveryCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
			await foreach (var evt in discovery.StartDiscovery(args, default, discoveryCts.Token))
			{
				if (evt.Type != ServiceEventType.Running)
					continue;

				if (evt.Service != args.service)
					continue;

				switch (evt)
				{
					case DockerServiceEvent dockerEvt:
						tailTask = TailDockerContainer(dockerEvt.descriptor, args.BeamoLocalSystem, LogHandler, cts);
						break;
					case HostServiceEvent hostEvt:
						tailTask = TailProcess(hostEvt.descriptor, LogHandler, cts);
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
				Log.Verbose($"service=[{args.service}] is waiting for log observation");

				await tailTask;

				await discoveryCts.CancelAsync();
				await discovery.Stop();

				Log.Verbose($"{args.service} has stopped.");
			}
		}
	}

	async static Task TailDockerContainer(DockerServiceDescriptor container, BeamoLocalSystem beamo, Action<string> handleLog, CancellationTokenSource cts)
	{
		await foreach (var line in beamo.TailLogs(container.containerId, cts))
		{
			if (!string.IsNullOrEmpty(line))
			{
				handleLog(line);
			}
		}
	}

	static async Task TailProcess(HostServiceDescriptor host, Action<string> handleLog, CancellationTokenSource cts)
	{
		using var client = new HttpClient();
		client.Timeout = Timeout.InfiniteTimeSpan;
		// Set up the HTTP GET request
		var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{host.healthPort}/logs");

		// Set the "text/event-stream" media type to indicate Server-Sent Events
		request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));


		// Send the request and get the response
		try
		{
			using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

			// Check if the response is successful
			if (response.IsSuccessStatusCode)
			{
				// Read the content as a stream
				using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
				using var reader = new StreamReader(stream);
				try
				{
					while (true)
					{
						if (cts.IsCancellationRequested) break;

						var line = await reader.ReadLineAsync(cts.Token);
						if (line == null)
						{
							Log.Verbose("invalid log, server is likely destroyed.");
							break;
						}
 
						var substrLength = "data: ".Length;
						if (line?.Length > substrLength)
						{
							handleLog(line[substrLength..]);
						}
					}
				}
				catch (IOException ex) when (ex.Message.Contains("forcibly closed"))
				{
					// TODO: We are explicitly ignoring this exception until a later date.
					// The problem is that if the request stream is closed by an external process that had invoked this command (such as CliServer OR the Stop server command),
					// an exception is thrown about it.
					// This is mostly inconsequential other than the fact that it makes this command not exit gracefully.
					// Behavior-wise, this exception happening changes nothing about this command's execution, so... we are ignoring it until a later date.
				}
				catch (TaskCanceledException)
				{
					Log.Verbose("log task cancelled");
				}
			}
			else
			{
				Log.Error("Error: {0} - {1}", (int)response.StatusCode, response.ReasonPhrase);
			}
		}
		catch (TaskCanceledException)
		{
			Log.Verbose("Task was cancelled, shutting down logs");
		}
		catch (HttpRequestException ex) when (ex.Message.StartsWith("Connection refused"))
		{
			Log.Verbose("Service is not ready to accept connections yet... Retrying soon...");
		}
		catch (HttpRequestException ex)
		{
			Log.Verbose($"Skipping this error --- usually this happens when some external application kills the process. If this is NOT the case, notify Beamable. TYPE={ex.GetType()}, MESSAGE={ex.Message}");
		}
		catch (Exception ex)
		{
			Log.Error($"Some other error happened; please report a bug. TYPE={ex.GetType()}, MESSAGE={ex.Message}");
			throw;
		}
	}
}
