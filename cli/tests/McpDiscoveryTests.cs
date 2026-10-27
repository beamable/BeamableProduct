using Beamable.Common.BeamCli;
using cli.CliServerCommand;
using cli.Commands.Mcp;
using cli.Mcp;
using cli.Services.HttpServer;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace tests;

/// <summary>
/// How agents find the MCP endpoint: the standard Server Card and AI Catalog it serves, and the locator-based
/// `beam mcp status` / `beam mcp token` / `beam mcp setup --transport http` for this machine.
/// </summary>
[NonParallelizable]
public class McpDiscoveryTests
{
	private const string Token = "discovery-test-token";
	private McpHttpEndpoint _endpoint = null!;
	private ServeCliCommandArgs _args = null!;
	private string _tempDir = null!;

	[SetUp]
	public async Task SetUp()
	{
		_endpoint = await McpHttpEndpoint.Start("1.2.3", "test workspace");
		_args = new ServeCliCommandArgs { owner = "discovery-tests", token = Token, mcpEndpoint = _endpoint };
		_tempDir = Path.Combine(Path.GetTempPath(), "beam-discovery", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);
	}

	[TearDown]
	public async Task TearDown()
	{
		await _endpoint.DisposeAsync();
		Directory.Delete(_tempDir, true);
	}

	private static Task<HttpResponseMessage> Get(ServerServiceTests.TestServer server, string route, Action<HttpRequestMessage>? configure = null) =>
		server.Send(HttpMethod.Get, route, configure: configure);

	[Test]
	public async Task Server_card_follows_the_spec()
	{
		await using var server = new ServerServiceTests.TestServer(null, args: _args);
		using var response = await Get(server, ServerCardEndpoint.CARD_ROUTE,
			r => r.Headers.Accept.ParseAdd(ServerCardEndpoint.CARD_MEDIA_TYPE));
		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(ServerCardEndpoint.CARD_MEDIA_TYPE));
		Assert.That(response.Headers.GetValues("Access-Control-Allow-Origin").Single(), Is.EqualTo("*"));
		Assert.That(response.Headers.GetValues("Access-Control-Expose-Headers").Single(), Does.Contain("ETag"));
		Assert.That(response.Headers.CacheControl?.Public, Is.True);
		Assert.That(response.Headers.ETag, Is.Not.Null);

		var card = JObject.Parse(await response.Content.ReadAsStringAsync());
		AssertMatchesSchema(card);
		var remote = (JObject)card["remotes"]![0]!;
		Assert.That((string?)remote["url"], Is.EqualTo(new Uri(server.Client.BaseAddress!, "mcp").ToString()));
		Assert.That(remote["supportedProtocolVersions"]!.ToObject<string[]>(), Is.EqualTo(McpHttpEndpoint.SupportedProtocolVersions));
		Assert.That((bool?)remote["headers"]![0]!["isSecret"], Is.True);
		server.AssertNoErrors();
	}

	/// <summary>
	/// Checks the card against the vendored SEP-2127 schema (experimental-ext-server-card/schema.json): required
	/// properties, the $schema and name patterns, and each remote's required fields, type and url pattern.
	/// </summary>
	private static void AssertMatchesSchema(JObject card)
	{
		var schema = JObject.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,
			"ServerProtocol", "Fixtures", "ServerCard", "server-card.schema.json")));
		var cardSchema = (JObject)schema["$defs"]!["ServerCard"]!;
		foreach (var required in cardSchema["required"]!.Values<string>())
		{
			Assert.That(card.ContainsKey(required!), $"Server Card is missing required '{required}'.");
		}
		foreach (var property in card.Properties())
		{
			Assert.That(((JObject)cardSchema["properties"]!).ContainsKey(property.Name), $"'{property.Name}' is not a Server Card property.");
			var pattern = (string?)cardSchema["properties"]![property.Name]!["pattern"];
			if (pattern != null)
			{
				Assert.That((string?)property.Value, Does.Match(pattern), $"'{property.Name}' must match {pattern}.");
			}
		}

		var remoteSchema = (JObject)schema["$defs"]!["Remote"]!;
		foreach (var remote in card["remotes"]!.Cast<JObject>())
		{
			foreach (var required in remoteSchema["required"]!.Values<string>())
			{
				Assert.That(remote.ContainsKey(required!), $"Remote is missing required '{required}'.");
			}
			Assert.That(remoteSchema["properties"]!["type"]!["enum"]!.Values<string>(), Does.Contain((string?)remote["type"]));
			Assert.That((string?)remote["url"], Does.Match((string)remoteSchema["properties"]!["url"]!["pattern"]!));
		}
	}

	[Test]
	public async Task Server_card_carries_no_secrets_or_workspace_details()
	{
		// Cards are public (CORS *): no credentials, user- or session-specific data.
		await using var server = new ServerServiceTests.TestServer(null, args: _args);
		var text = await (await Get(server, ServerCardEndpoint.CARD_ROUTE)).Content.ReadAsStringAsync();
		Assert.That(text, Does.Not.Contain(Token));
		Assert.That(text, Does.Not.Contain("test workspace"));
		Assert.That(text, Does.Not.Contain(Directory.GetCurrentDirectory()));
		Assert.That(text, Does.Not.Contain("discovery-tests"), "The owner names the editor's project path.");
	}

	[Test]
	public async Task Server_card_revalidates_with_its_etag()
	{
		await using var server = new ServerServiceTests.TestServer(null, args: _args);
		using var first = await Get(server, ServerCardEndpoint.CARD_ROUTE);
		var etag = first.Headers.ETag!;
		using var second = await Get(server, ServerCardEndpoint.CARD_ROUTE, r => r.Headers.IfNoneMatch.Add(etag));
		Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
	}

	[Test]
	public async Task Discovery_is_readable_cross_origin_but_read_only()
	{
		await using var server = new ServerServiceTests.TestServer(null, args: _args);
		using var fromPage = await Get(server, ServerCardEndpoint.CARD_ROUTE, r => r.Headers.Add("Origin", "https://agent.example"));
		Assert.That(fromPage.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		using var preflight = await server.Send(HttpMethod.Options, ServerCardEndpoint.CARD_ROUTE,
			configure: r => r.Headers.Add("Origin", "https://agent.example"));
		Assert.That(preflight.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
		using var write = await server.Send(HttpMethod.Post, ServerCardEndpoint.CARD_ROUTE, "{}");
		Assert.That(write.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
		server.AssertNoErrors();
	}

	[Test]
	public async Task Ai_catalog_points_at_the_card()
	{
		await using var server = new ServerServiceTests.TestServer(null, args: _args);
		using var response = await Get(server, ServerCardEndpoint.CATALOG_ROUTE);
		Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(ServerCardEndpoint.CATALOG_MEDIA_TYPE));
		var catalog = JObject.Parse(await response.Content.ReadAsStringAsync());
		Assert.That((string?)catalog["specVersion"], Is.EqualTo("1.0"));
		var entry = catalog["entries"]!.Single();
		Assert.That((string?)entry["type"], Is.EqualTo(ServerCardEndpoint.CARD_MEDIA_TYPE));
		Assert.That((string?)entry["identifier"], Does.Match("^urn:air:[^:]+:[^:]+:[^:]+$"));

		using var card = await Get(server, new Uri((string)entry["url"]!).AbsolutePath.TrimStart('/'));
		Assert.That(card.StatusCode, Is.EqualTo(HttpStatusCode.OK), "The catalog's url must resolve to the card.");
	}

	[Test]
	public async Task Discovery_is_absent_without_mcp()
	{
		await using var server = new ServerServiceTests.TestServer(null, args: new ServeCliCommandArgs { owner = "x", token = Token });
		using var response = await Get(server, ServerCardEndpoint.CATALOG_ROUTE);
		Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.OK));
	}

	// --- `beam mcp status`

	private ServerLocator WriteLocator(ServerServiceTests.TestServer server, int pid)
	{
		var uri = server.Client.BaseAddress!.ToString();
		var locator = new ServerLocator
		{
			pid = pid, port = server.Client.BaseAddress!.Port, uri = uri, owner = "x", version = "1.2.3",
			token = Token, startedAt = 1, mcpUrl = uri.TrimEnd('/') + McpHttpEndpoint.PATH
		};
		ServerLocatorFile.Write(_tempDir, locator);
		return locator;
	}

	[Test]
	public async Task Status_without_a_server_says_how_to_start_one()
	{
		var status = await McpStatusCommand.Check(_tempDir);
		Assert.That(status.available, Is.False);
		Assert.That(status.reason, Does.Contain("--enable-mcp"));
	}

	[Test]
	public async Task Status_finds_the_live_server()
	{
		await using var server = new ServerServiceTests.TestServer(null, args: _args);
		var locator = WriteLocator(server, Environment.ProcessId);
		var status = await McpStatusCommand.Check(_tempDir);
		Assert.That(status.available, Is.True, status.reason);
		Assert.That(status.url, Is.EqualTo(locator.mcpUrl));
		Assert.That(status.protocolVersions, Is.EqualTo(McpHttpEndpoint.SupportedProtocolVersions));
	}

	[Test]
	public async Task Status_notices_a_port_taken_over_by_another_process()
	{
		// The locator's process is alive but is not the one answering on the port.
		using var other = Process.Start(new ProcessStartInfo("sleep", "30"))!;
		try
		{
			await using var server = new ServerServiceTests.TestServer(null, args: _args);
			WriteLocator(server, other.Id);
			var status = await McpStatusCommand.Check(_tempDir);
			Assert.That(status.available, Is.False);
			Assert.That(status.reason, Does.Contain("no longer belongs"));
		}
		finally
		{
			other.Kill();
		}
	}

	// --- `beam mcp token` / `beam mcp setup --transport http`

	[Test]
	public async Task Token_lookup_matches_the_configured_url()
	{
		await using var server = new ServerServiceTests.TestServer(null, args: _args);
		var locator = WriteLocator(server, Environment.ProcessId);
		Assert.That(McpLocalServer.Find(_tempDir, locator.mcpUrl)?.token, Is.EqualTo(Token));
		Assert.That(McpLocalServer.Find(_tempDir, "http://127.0.0.1:1/mcp"), Is.Null, "Never hand a token to a different server.");
		Assert.That(McpLocalServer.Find(_tempDir)?.token, Is.EqualTo(Token));
	}

	[Test]
	public void Setup_writes_an_http_entry_and_keeps_other_servers()
	{
		var root = JObject.Parse("{\"mcpServers\":{\"other\":{\"command\":\"x\"},\"beamable\":{\"command\":\"dotnet\"}}}");
		McpSetupCommand.SetBeamableServer(root, "http://127.0.0.1:8818/mcp");
		var beamable = (JObject)root["mcpServers"]!["beamable"]!;
		Assert.That((string?)beamable["type"], Is.EqualTo("http"));
		Assert.That((string?)beamable["url"], Is.EqualTo("http://127.0.0.1:8818/mcp"));
		Assert.That((string?)beamable["headersHelper"], Is.EqualTo("dotnet beam mcp token"));
		Assert.That(beamable.ContainsKey("command"), Is.False, "An http entry must not keep the stdio command.");
		Assert.That(root["mcpServers"]!["other"], Is.Not.Null);
	}

	// --- preferred port

	[Test]
	public void Preferred_port_is_stable_and_in_range()
	{
		var port = CliServerPorts.PreferredPort("/Users/dev/Projects/MyGame");
		Assert.That(CliServerPorts.PreferredPort("/Users/dev/Projects/MyGame"), Is.EqualTo(port));
		Assert.That(CliServerPorts.PreferredPort("/users/dev/projects/mygame/"), Is.EqualTo(port), "Case and trailing slash must not matter.");
		Assert.That(port, Is.InRange(8500, CliServerPorts.RANGE_END));
		Assert.That(CliServerPorts.PreferredPort("/Users/dev/Projects/OtherGame"), Is.Not.EqualTo(port));
	}
}
