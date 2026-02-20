using System.CommandLine;
using System.Text;
using Beamable.Server;
using cli.Services.HttpServer;
using Newtonsoft.Json;

namespace cli.CliServerCommand;


public class RequestCliCommandArgs : CommandArgs
{
	public int port;
	public string commandLine;
}

public class RequestCliCommandOutput
{
	
}

public class RequestCliCommand 
	: StreamCommand<RequestCliCommandArgs, RequestCliCommandOutput>
	, IStandaloneCommand
{
	public override bool IsForInternalUse => true;

	public RequestCliCommand() : base("req", "Request a CLI invocation from a running CLI server")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<int>("--port", () => ServerService.DEFAULT_PORT, "The port where the CLI server is running"),
			(args, i) => args.port = i);
		AddOption(new Option<string>("--cli", "The CLI command to execute"),
			(args, s) => args.commandLine = s);
	}

	public override async Task Handle(RequestCliCommandArgs args)
	{
		var client = new HttpClient();
		
		var req = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:{args.port}/execute");

		var json = JsonConvert.SerializeObject(new ServerRequest() { commandLine = args.commandLine });
		
		req.Content = new StringContent(json, Encoding.UTF8, "application/json");

		
		try
		{
			Log.Information("sending request " + json);
			using HttpResponseMessage response =
				await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

			using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
			using StreamReader reader = new StreamReader(streamToReadFrom);

			while (!reader.EndOfStream)
			{
				Log.Debug("waiting for next chunk of data...");
				var line = await reader.ReadLineAsync();
				
				if (string.IsNullOrEmpty(line)) continue; // TODO: what if the message contains a \n character?
				line = line.Replace("\u200b", ""); // remove life-cycle zero-width character 

				if (!line.StartsWith("data: ")) continue;

				var lineJson = line.Substring("data: ".Length); // remove the Server-Side-Event notation
					
				Log.Information("received, " + lineJson);

			}
		}
		catch (Exception ex)
		{
			Log.Error("failed : " + ex.Message);
		}

	}
}
