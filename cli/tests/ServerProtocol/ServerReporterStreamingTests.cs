using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using cli.Services.HttpServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace tests.ServerProtocol;

/// <summary>
/// The /execute reporter buffers events and serializes them with a shared serializer. Unity parses the bytes, so
/// they must stay exactly what JsonConvert.SerializeObject produced, in order, and still arrive promptly.
/// </summary>
[NonParallelizable]
public class ServerReporterStreamingTests
{
	public enum Mood { Calm, Excited }

	public class Awkward
	{
		public string text = "";
		public double ratio;
		public float small;
		public long big;
		public int? missing;
		public DateTime when;
		public Mood mood;
		public List<string> items = new();
		public Dictionary<string, int> counts = new();
		public Awkward? child;
	}

	public class Indexed
	{
		public int index;
	}

	private static readonly Awkward[] AwkwardPayloads =
	{
		new()
		{
			text = "quotes \" backslash \\ newline \n tab \t control \u0001 </script> zero-width ​ emoji 🎮 ąęłżź 日本",
			ratio = 0.1, small = 1e-7f, big = long.MaxValue, when = new DateTime(2026, 10, 27, 12, 30, 45, 123, DateTimeKind.Utc),
			mood = Mood.Excited, items = { "a", "", "c" }, counts = { ["x"] = 1, ["y"] = -2 },
			child = new Awkward { text = "nested", ratio = double.NaN }
		},
		new() { text = "", ratio = double.PositiveInfinity, small = float.MinValue, big = long.MinValue },
		new() { text = new string('x', 40_000) }, // larger than the flush threshold on its own
	};

	[Test]
	public async Task Events_are_byte_identical_to_JsonConvert()
	{
		await using var server = new ServerServiceTests.TestServer(() => new ServerServiceTests.ControlledApp((_, reporter) =>
		{
			foreach (var payload in AwkwardPayloads)
			{
				reporter.Report("stream", payload);
			}
			reporter.Report("other", new Indexed { index = 7 });
			return Task.FromResult(0);
		}));
		var body = await server.Execute("awkward");

		var lines = body.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
		Assert.That(lines, Has.Length.EqualTo(AwkwardPayloads.Length + 1), "Every event is one line.");
		var expected = new StringBuilder();
		for (var i = 0; i < lines.Length; i++)
		{
			var ts = (long)JObject.Parse(lines[i]["data: ".Length..])["ts"]!;
			var json = i < AwkwardPayloads.Length
				? JsonConvert.SerializeObject(new ReportDataPoint<Awkward> { ts = ts, type = "stream", data = AwkwardPayloads[i] }, UnitySerializationSettings.Instance)
				: JsonConvert.SerializeObject(new ReportDataPoint<Indexed> { ts = ts, type = "other", data = new Indexed { index = 7 } }, UnitySerializationSettings.Instance);
			expected.Append("data: ").Append(json).Append(Environment.NewLine);
		}
		Assert.That(body, Is.EqualTo(expected.ToString()));
		server.AssertNoErrors();
	}

	[Test]
	public async Task Ten_thousand_events_arrive_in_order_and_framed()
	{
		const int count = 10_000;
		await using var server = new ServerServiceTests.TestServer(() => new ServerServiceTests.ControlledApp((_, reporter) =>
		{
			for (var i = 0; i < count; i++)
			{
				reporter.Report("stream", new Indexed { index = i });
			}
			return Task.FromResult(0);
		}));
		var events = ExecuteGoldenTests.ParseEvents(await server.Execute("many"));
		Assert.That(events, Has.Count.EqualTo(count));
		Assert.That(events.Select(e => (int)e["data"]!["index"]!), Is.EqualTo(Enumerable.Range(0, count)));
		server.AssertNoErrors();
	}

	[Test]
	public async Task A_buffered_event_is_sent_while_the_command_is_still_running()
	{
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		await using var server = new ServerServiceTests.TestServer(() => new ServerServiceTests.ControlledApp(async (_, reporter) =>
		{
			reporter.Report("stream", new Indexed { index = 1 });
			await release.Task; // the command keeps running; nothing else is reported
			return 0;
		}));
		try
		{
			using var request = new HttpRequestMessage(HttpMethod.Post, "execute")
			{
				Content = new StringContent(JsonConvert.SerializeObject(new ServerRequest { commandLine = "slow" }), Encoding.UTF8, "application/json")
			};
			using var response = await server.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
			using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
			var line = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(2));
			Assert.That(line, Does.StartWith("data: ").And.Contain("\"index\":1"),
				"A lone event must not wait for more events or for the command to end.");
		}
		finally
		{
			release.TrySetResult();
		}
		server.AssertNoErrors();
	}

	[Test]
	public async Task Heartbeat_follows_buffered_events_and_a_hang_up_cancels_the_command()
	{
		var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		ServerServiceTests.ControlledApp? app = null;
		await using var server = new ServerServiceTests.TestServer(() => app = new ServerServiceTests.ControlledApp(async (_, reporter) =>
		{
			reporter.Report("stream", new Indexed { index = 1 });
			try
			{
				await Task.Delay(TimeSpan.FromSeconds(30), app!.Lifecycle.CancellationToken);
			}
			catch (OperationCanceledException)
			{
				cancelled.TrySetResult();
			}
			return 0;
		}) { Heartbeat = true });

		using var request = new HttpRequestMessage(HttpMethod.Post, "execute")
		{
			Content = new StringContent(JsonConvert.SerializeObject(new ServerRequest { commandLine = "long" }), Encoding.UTF8, "application/json")
		};
		var response = await server.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		var stream = await response.Content.ReadAsStreamAsync();
		var received = new MemoryStream();
		var buffer = new byte[256];
		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
		while (DateTime.UtcNow < deadline && !Encoding.UTF8.GetString(received.ToArray()).Contains('\u200b'))
		{
			var read = await stream.ReadAsync(buffer).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
			received.Write(buffer, 0, read);
		}
		var text = Encoding.UTF8.GetString(received.ToArray());
		Assert.That(text, Does.StartWith("data: ").And.Contain("\"index\":1"));
		Assert.That(text.IndexOf('\u200b'), Is.GreaterThan(text.IndexOf("data: ", StringComparison.Ordinal)),
			"The heartbeat must come after the event that was reported before it.");

		// The client hangs up; the next heartbeat fails and cancels the command.
		response.Dispose();
		await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
	}
}
