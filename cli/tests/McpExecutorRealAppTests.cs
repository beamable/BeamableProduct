using cli.Commands.Mcp;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace tests;

/// <summary>
/// MCP tool calls through the real CLI app (not a scripted one), where services such as the command's
/// <see cref="cli.Services.AppLifecycle"/> live in the invocation's own scope.
/// </summary>
[NonParallelizable]
public class McpExecutorRealAppTests
{
	private string _originalDirectory = null!;
	private string _workspace = null!;

	[SetUp]
	public void SetUp()
	{
		// A workspace whose backend is unreachable, so commands never wait on the network.
		_originalDirectory = Directory.GetCurrentDirectory();
		_workspace = Path.Combine(Path.GetTempPath(), "beam-mcp-real", Guid.NewGuid().ToString("N"));
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

	private static string Text(CallToolResult result) => string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));

	[Test]
	public async Task Cancelling_reaches_the_commands_own_lifecycle()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();
		var result = await new McpToolExecutor().ExecuteCommandAsync("config", cancellationToken: cts.Token);

		Assert.That(Text(result), Does.Not.Contain("Service not found"),
			"The command's lifecycle lives in the invocation scope, not the app's root.");
		Assert.That(result.IsError, Is.True);
		Assert.That(Text(result), Does.Contain("cancelled"));
	}

	[Test]
	public async Task Help_calls_do_not_leak()
	{
		var executor = new McpToolExecutor();
		await executor.ExecuteHelpAsync("");
		var first = await HelpAndForget(executor);
		for (var i = 0; i < 3; i++)
		{
			await executor.ExecuteHelpAsync("config");
		}
		for (var i = 0; i < 3; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		Assert.That(first.IsAlive, Is.False, "A finished help call's app is still reachable.");
	}

	// Kept out of line so no local keeps the app reachable.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<WeakReference> HelpAndForget(McpToolExecutor executor)
	{
		cli.App? created = null;
		var factory = executor.AppFactory;
		executor.AppFactory = () => created = factory();
		var text = await executor.ExecuteHelpAsync("config");
		executor.AppFactory = factory;
		Assert.That(text, Does.Contain("config"));
		return new WeakReference(created);
	}
}
