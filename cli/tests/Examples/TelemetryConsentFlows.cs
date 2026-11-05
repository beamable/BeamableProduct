using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Spectre.Console;
using Spectre.Console.Testing;
using Otel = Beamable.Common.Constants.Features.Otel;

namespace tests.Examples;

/// <summary>
/// The first command run in a new workspace asks for telemetry consent. Without a TTY (CI, a script, an agent)
/// that prompt used to throw "Failed to read input in non-interactive mode" and crash the command, e.g.
/// `beam org realms` right after `beam init`. Regression test for the non-interactive branch in App.cs.
/// </summary>
public class TelemetryConsentFlows : CLITest
{
	private string? _autoSetup, _disabled, _docker;

	[SetUp]
	public void ClearTelemetryEnvironment()
	{
		// the consent branch is only reached when none of these are set
		_autoSetup = Environment.GetEnvironmentVariable(Otel.ENV_CLI_AUTO_SETUP_TELEMETRY);
		_disabled = Environment.GetEnvironmentVariable(Otel.ENV_CLI_DISABLE_TELEMETRY);
		_docker = Environment.GetEnvironmentVariable(Otel.ENV_CLI_RUNNING_ON_DOCKER);
		Environment.SetEnvironmentVariable(Otel.ENV_CLI_AUTO_SETUP_TELEMETRY, null);
		Environment.SetEnvironmentVariable(Otel.ENV_CLI_DISABLE_TELEMETRY, null);
		Environment.SetEnvironmentVariable(Otel.ENV_CLI_RUNNING_ON_DOCKER, null);
	}

	[TearDown]
	public void RestoreTelemetryEnvironment()
	{
		Environment.SetEnvironmentVariable(Otel.ENV_CLI_AUTO_SETUP_TELEMETRY, _autoSetup);
		Environment.SetEnvironmentVariable(Otel.ENV_CLI_DISABLE_TELEMETRY, _disabled);
		Environment.SetEnvironmentVariable(Otel.ENV_CLI_RUNNING_ON_DOCKER, _docker);
	}

	[Test]
	public void FirstCommandInFreshWorkspace_WithoutTty_DoesNotPrompt()
	{
		// a fresh workspace: a .beamable folder with no telemetry decision recorded yet
		Directory.CreateDirectory(".beamable");
		File.WriteAllText(Path.Combine(".beamable", "config.beam.json"), "{ \"cid\": \"123\", \"pid\": \"DE_1\", \"host\": \"https://api.beamable.com\" }");

		// no TTY: a console that can't read input (no .Interactive())
		AnsiConsole.Console = new TestConsole().Colors(ColorSystem.NoColors);

		// no -q, so only the non-interactive check stands between us and the prompt
		var exitCode = RunFull(new[] { "config" });

		Assert.AreEqual(0, exitCode, "the command must not fail on the consent prompt");
		var configText = string.Join("\n", Directory.GetFiles(".beamable", "*.json", SearchOption.AllDirectories).Select(File.ReadAllText));
		Assert.That(configText, Does.Match("\"BeamCliAllowTelemetry\"\\s*:\\s*false"),
			"without an answer, telemetry must default to off");
	}
}
