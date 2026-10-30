using Beamable.Server;
using cli.Services.HttpServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace cli.Mcp;

public class McpStatusCommandArgs : CommandArgs { }

[Serializable]
public class McpStatusResult
{
	/// <summary>True when an MCP endpoint for this workspace answered just now.</summary>
	public bool available;
	public string url;
	public string version;
	public int pid;
	public List<string> protocolVersions = new List<string>();
	/// <summary>Why the endpoint is not available, and what to do about it.</summary>
	public string reason;
}

/// <summary>
/// Tells an agent (or a person, or CI) whether this workspace's MCP endpoint is up, so it can use the beam tools
/// or ask for the server to be started instead of failing on a dead URL.
/// </summary>
public class McpStatusCommand
	: AtomicCommand<McpStatusCommandArgs, McpStatusResult>
	, IStandaloneCommand
	, ISkipManifest
{
	/// <summary>The exit code when no endpoint is available.</summary>
	public const int UNAVAILABLE_EXIT_CODE = 3;

	public McpStatusCommand() : base("status", "Check whether an MCP endpoint is running for this workspace and how to reach it")
	{
	}

	public override void Configure()
	{
	}

	public override async Task<McpStatusResult> GetResult(McpStatusCommandArgs args)
	{
		var result = args.ConfigService?.DirectoryExists == true
			? await Check(args.ConfigService.ConfigTempDirectoryPath)
			: new McpStatusResult { reason = "Not in a Beamable workspace." };
		if (!result.available)
		{
			throw new CliException(result.reason, UNAVAILABLE_EXIT_CODE, true);
		}
		return result;
	}

	/// <summary>Finds the workspace's server and confirms it answers as the process that wrote the locator.</summary>
	public static async Task<McpStatusResult> Check(string configTempDirectory, HttpClient client = null)
	{
		var locator = McpLocalServer.Find(configTempDirectory);
		if (locator == null)
		{
			return new McpStatusResult { reason = McpLocalServer.NOT_RUNNING };
		}

		var result = new McpStatusResult { url = locator.mcpUrl, pid = locator.pid, version = locator.version };
		var ownsClient = client == null;
		client ??= new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
		try
		{
			// The port could have been taken over by another process since the locator was written.
			var info = JsonConvert.DeserializeObject<ServerInfoResponse>(await client.GetStringAsync(locator.uri.TrimEnd('/') + "/info"));
			if (info?.pid != locator.pid || info.features?.Contains(ServerService.FEATURE_MCP) != true)
			{
				result.reason = $"Port {locator.port} no longer belongs to the server that wrote the locator. " + McpLocalServer.NOT_RUNNING;
				return result;
			}

			using var request = new HttpRequestMessage(HttpMethod.Get, locator.mcpUrl.TrimEnd('/') + "/server-card");
			request.Headers.Accept.ParseAdd(ServerCardEndpoint.CARD_MEDIA_TYPE);
			using var response = await client.SendAsync(request);
			response.EnsureSuccessStatusCode();
			var card = JObject.Parse(await response.Content.ReadAsStringAsync());
			result.version = (string)card["version"] ?? result.version;
			result.protocolVersions = card["remotes"]?.FirstOrDefault()?["supportedProtocolVersions"]?.ToObject<List<string>>()
			                          ?? new List<string>();
			result.available = true;
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
		{
			result.reason = $"The server at {locator.uri} did not answer ({ex.Message}). " + McpLocalServer.NOT_RUNNING;
		}
		finally
		{
			if (ownsClient)
			{
				client.Dispose();
			}
		}
		return result;
	}
}
