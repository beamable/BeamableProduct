using Beamable.Common.BeamCli.Contracts;
using Beamable.Serialization.SmallerJSON;
using cli.CliServerCommand;
using cli.Services;
using cli.Services.CliInvocation;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tests.ServerProtocol;

/// <summary>
/// The Unity editor runs its CLI server with --log=verbose and shows those logs in its console, so the verbose
/// dumps of the app context and command arguments must never contain credentials.
/// </summary>
[NonParallelizable]
public class VerboseLogSecretsTests
{
	private const string AccessToken = "access-0b7e1d6a-secret";
	private const string RefreshToken = "refresh-5c2f9e44-secret";
	private const string ServerToken = "server-session-token-secret";

	private string _originalDirectory = null!;
	private string _workspace = null!;

	private sealed class LogCapture : IDataReporterService
	{
		public readonly ConcurrentQueue<string> Lines = new();
		public void Report(string rawMessage) => Lines.Enqueue(rawMessage);
		public void Report<T>(string type, T data) => Lines.Enqueue(data is CliLogMessage log ? log.message : JsonConvert.SerializeObject(data));
	}

	[SetUp]
	public void SetUp()
	{
		_originalDirectory = Directory.GetCurrentDirectory();
		_workspace = Path.Combine(Path.GetTempPath(), "beam-secrets", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path.Combine(_workspace, ".beamable", "temp"));
		File.WriteAllText(Path.Combine(_workspace, ".beamable", "config.beam.json"), JsonConvert.SerializeObject(new
		{
			cid = "1234567890",
			pid = "DE_1234567890",
			host = "http://127.0.0.1:9",
			otelConfig = new { BeamCliAllowTelemetry = false }
		}));
		var now = DateTimeOffset.Now;
		File.WriteAllText(Path.Combine(_workspace, ".beamable", "temp", "auth.beam.json"), JsonConvert.SerializeObject(new
		{
			access_token = AccessToken,
			refresh_token = RefreshToken,
			expires_at = now.AddDays(1),
			expires_in = 86_400_000,
			issued_at = now,
			cid = "1234567890",
			pid = "DE_1234567890"
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

	private static async Task<string[]> RunVerbose(string command)
	{
		var capture = new LogCapture();
		await CliInvocationCore.Execute(new CliInvocationRequest
		{
			ReadCommand = () => Task.FromResult((command, command)),
			UseCustomSplitter = true,
			CreateReporter = _ => capture,
			ReportSetupFailure = _ => Task.CompletedTask
		});
		return capture.Lines.ToArray();
	}

	[Test]
	public async Task App_context_dump_has_no_tokens()
	{
		var lines = await RunVerbose("config --log verbose --emit-log-streams");
		Assert.That(lines, Has.Some.Contains("app context="), "The dump itself is still wanted for diagnostics.");
		Assert.That(lines, Has.Some.Contains("1234567890"), "Non-secret context (the cid) stays in the dump.");
		Assert.That(lines, Has.None.Contains(AccessToken));
		Assert.That(lines, Has.None.Contains(RefreshToken));
	}

	[Test]
	public void Argument_dump_has_no_server_tokens()
	{
		// The same serializer the command handler uses for "running command=[...] with parsed arguments".
		var request = Json.Serialize(new RequestCliCommandArgs { port = 9114, commandLine = "config", token = ServerToken }, new StringBuilder());
		var serve = Json.Serialize(new ServeCliCommandArgs { port = 9114, owner = "unity", token = ServerToken }, new StringBuilder());
		Assert.That(request, Does.Contain("config").And.Not.Contain(ServerToken));
		Assert.That(serve, Does.Contain("unity").And.Not.Contain(ServerToken));
		Assert.That(serve, Does.Not.Contain("mcpEndpoint"), "The live endpoint object is not an argument.");
	}
}
