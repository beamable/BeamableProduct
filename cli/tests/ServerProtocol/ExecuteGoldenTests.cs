using cli.Services.HttpServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace tests.ServerProtocol;

/// <summary>
/// Replays recorded /execute exchanges (see `beam server serve --record-to`) through the real server and a real
/// CLI app, and checks the client sees the same event sequence. Anything Unity parses (channel, payload fields,
/// order, count) must match; only timestamps, stack traces and the workspace path may differ.
/// </summary>
[NonParallelizable]
public class ExecuteGoldenTests
{
	private static readonly string FixtureFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "ServerProtocol", "Fixtures");

	private string _originalDirectory = null!;
	private string _workspace = null!;

	public static IEnumerable<TestCaseData> Fixtures() => Directory
		.GetFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "ServerProtocol", "Fixtures"), "*.json")
		.OrderBy(f => f)
		.Select(f => new TestCaseData(Path.GetFileNameWithoutExtension(f)).SetName($"Replay_{Path.GetFileNameWithoutExtension(f)}"));

	public static ExecuteFixture Load(string name) =>
		JsonConvert.DeserializeObject<ExecuteFixture>(File.ReadAllText(Path.Combine(FixtureFolder, name + ".json")))!;

	[SetUp]
	public void SetUp()
	{
		// The fixtures were recorded in a workspace whose backend is unreachable, so nothing leaves the machine.
		_originalDirectory = Directory.GetCurrentDirectory();
		_workspace = Path.Combine(Path.GetTempPath(), "beam-golden", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path.Combine(_workspace, ".beamable"));
		File.WriteAllText(Path.Combine(_workspace, ".beamable", "config.beam.json"), JsonConvert.SerializeObject(new
		{
			cid = "1234567890",
			pid = "DE_1234567890",
			host = "http://127.0.0.1:9",
			otelConfig = new { BeamCliAllowTelemetry = false }
		}));
		Directory.SetCurrentDirectory(_workspace);
	}

	[TearDown]
	public void TearDown()
	{
		Directory.SetCurrentDirectory(_originalDirectory);
		try
		{
			Directory.Delete(_workspace, true);
		}
		catch (IOException)
		{
			// A log file may still be open; the temp folder is cleaned eventually.
		}
	}

	[TestCaseSource(nameof(Fixtures))]
	public async Task Replay(string name)
	{
		var fixture = Load(name);
		await using var server = new ServerServiceTests.TestServer(null);
		using var response = await server.Send(HttpMethod.Post, "execute", fixture.body);
		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

		var actual = ParseEvents(await response.Content.ReadAsStringAsync());
		AssertSameSequence(fixture.events, actual, _workspace);
		server.AssertNoErrors();
	}

	/// <summary>Fixtures whose request is a command (not a malformed body), which MCP's beam_exec can run too.</summary>
	public static IEnumerable<TestCaseData> CommandFixtures() => Fixtures()
		.Where(c => CommandOf(Load((string)c.Arguments[0]!)) != null)
		.Select(c => new TestCaseData(c.Arguments[0]).SetName($"McpEquivalence_{c.Arguments[0]}"));

	private static string? CommandOf(ExecuteFixture fixture)
	{
		try
		{
			var command = JsonConvert.DeserializeObject<ServerRequest>(fixture.body)?.commandLine;
			return string.IsNullOrWhiteSpace(command) ? null : command;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	[TestCaseSource(nameof(CommandFixtures))]
	public async Task McpEquivalence(string name)
	{
		// An agent must see what Unity sees: beam_exec's structured events match the /execute stream.
		var fixture = Load(name);
		var command = CommandOf(fixture)!;
		var result = await new cli.Commands.Mcp.McpToolExecutor().ExecuteCommandAsync(command);

		var structured = JObject.Parse(result.StructuredContent!.Value.GetRawText());
		var events = ((JArray)structured["events"]!).Cast<JObject>().ToList();
		// The parse diagram in an error's invocation echoes the flags each transport adds, so it legitimately differs.
		AssertSameSequence(fixture.events, events, _workspace, ignoreInvocation: true);
		var failed = fixture.events.Any(e => ((string?)e["type"] ?? "").StartsWith("error"));
		Assert.That(result.IsError == true, Is.EqualTo(failed));
	}

	[Test]
	public void Fixtures_exist()
	{
		Assert.That(Fixtures().Count(), Is.GreaterThanOrEqualTo(4), $"Expected recordings in {FixtureFolder}.");
	}

	/// <summary>The events in an /execute response body, without the heartbeat characters.</summary>
	public static List<JObject> ParseEvents(string body) => body
		.Replace("​", "")
		.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
		.Select(line =>
		{
			Assert.That(line, Does.StartWith("data: "), "Every line must be an SSE data line.");
			return JObject.Parse(line["data: ".Length..]);
		})
		.ToList();

	public static void AssertSameSequence(IReadOnlyList<JObject> expected, IReadOnlyList<JObject> actual, string workspace,
		bool ignoreInvocation = false)
	{
		var want = expected.Select(e => Normalize(e, null, ignoreInvocation)).ToList();
		var got = actual.Select(e => Normalize(e, workspace, ignoreInvocation)).ToList();
		Assert.That(got.Count, Is.EqualTo(want.Count),
			$"Event count differs.\nexpected: {string.Join("\n", want)}\nactual: {string.Join("\n", got)}");
		for (var i = 0; i < want.Count; i++)
		{
			Assert.That(got[i], Is.EqualTo(want[i]), $"Event {i} differs.");
		}
	}

	/// <summary>Drops what legitimately varies between runs, so the comparison is about the protocol.</summary>
	public static string Normalize(JObject evt, string? workspace, bool ignoreInvocation = false)
	{
		var copy = (JObject)evt.DeepClone();
		Assert.That(copy["ts"]?.Type, Is.EqualTo(JTokenType.Integer), "Every event carries a unix-ms timestamp.");
		copy.Remove("ts");
		if (copy["data"] is JObject data)
		{
			if (data["stackTrace"] is JValue { Type: JTokenType.String })
			{
				data["stackTrace"] = "<stack>";
			}

			// A parse error's invocation diagram starts with the root command, which is the host's assembly name.
			if (data["invocation"] is JValue { Type: JTokenType.String } invocation)
			{
				data["invocation"] = ignoreInvocation
					? "<invocation>"
					: Regex.Replace((string)invocation!, @"^!\[ \S+ ", "![ <root> ");
			}

			if (workspace != null && data["configPath"] is JValue { Type: JTokenType.String } configPath)
			{
				var path = (string)configPath!;
				// macOS resolves the temp folder through /private.
				foreach (var prefix in new[] { workspace, "/private" + workspace })
				{
					if (path.StartsWith(prefix))
					{
						data["configPath"] = "<workspace>" + path[prefix.Length..];
						break;
					}
				}
			}
		}
		return copy.ToString(Formatting.None);
	}
}
