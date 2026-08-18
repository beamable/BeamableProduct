using cli.Commands.LocalStack;
using cli.Services.LocalStack;
using NUnit.Framework;

namespace tests;

/// <summary>
/// Covers the pure command-line token derivation `beam local stop` uses to find and kill an orphaned
/// runtime whose recorded pid is stale (the Windows wrapper-chain orphaning of the Scala JVMs, the portal
/// node process, and the beam microservice dotnet processes). The Win32 process matching itself is not
/// exercised here — only the platform-independent token logic. Tokens must be specific to a single step so
/// `stop <step>` stays precise, and specific to this stack so Rider/MSBuild/MCP are never matched.
/// </summary>
public class LocalStackKillTokenTests
{
	[TestCase("scala: gateway", "scala:", ExpectedResult = "gateway")]
	[TestCase("scala:account", "scala:", ExpectedResult = "account")]
	[TestCase("SCALA: Gateway", "scala:", ExpectedResult = "Gateway")]
	[TestCase("microservice: CampaignService", "microservice:", ExpectedResult = "CampaignService")]
	[TestCase("portal extension: MyExt", "portal extension:", ExpectedResult = "MyExt")]
	[TestCase("group: core", "group:", ExpectedResult = "core")]
	[TestCase("portal", "scala:", ExpectedResult = null)]
	[TestCase("", "scala:", ExpectedResult = null)]
	[TestCase(null, "scala:", ExpectedResult = null)]
	public string DeriveSuffix_extracts_after_prefix(string stepName, string prefix)
		=> LocalStackStopCommand.DeriveSuffix(stepName, prefix);

	[Test]
	public void Scala_step_tokens_are_mainClass_and_service_classpath()
	{
		var entry = new LocalStackRunEntry
		{
			name = "scala: gateway",
			kind = "shell",
			workingDirectory = @"C:\repos\BeamableBackend", // shared across all scala steps — must NOT be a token
			matchToken = "com.beamable.gateway.App"
		};

		var tokens = LocalStackStopCommand.BuildKillTokens(entry);

		Assert.That(tokens, Does.Contain("com.beamable.gateway.App"));
		Assert.That(tokens, Does.Contain("tools/gateway/"));
		Assert.That(tokens, Does.Contain(@"tools\gateway\"));
		Assert.That(tokens, Does.Not.Contain(@"C:\repos\BeamableBackend"),
			"the shared scala repo dir would kill every scala service, breaking single-step stop");
	}

	[Test]
	public void Scala_step_self_heals_without_matchToken()
	{
		// Simulates a run-state written by a pre-fix CLI: no matchToken, but the step name still identifies
		// the Scala service so `stop` can find the orphaned JVM by its classpath fragment.
		var entry = new LocalStackRunEntry { name = "scala: auth", kind = "shell" };

		var tokens = LocalStackStopCommand.BuildKillTokens(entry);

		Assert.That(tokens, Does.Contain("tools/auth/"));
		Assert.That(tokens, Does.Contain(@"tools\auth\"));
	}

	[Test]
	public void Process_step_token_is_its_working_directory()
	{
		// C# gateway / portal vite: the working dir is an absolute, per-step path on the runtime's cmdline.
		var entry = new LocalStackRunEntry
		{
			name = "portal frontend",
			kind = "process",
			workingDirectory = @"C:\repos\portal"
		};

		var tokens = LocalStackStopCommand.BuildKillTokens(entry);

		Assert.That(tokens, Is.EqualTo(new[] { @"C:\repos\portal" }));
	}

	[Test]
	public void Beam_step_token_is_the_service_id()
	{
		var entry = new LocalStackRunEntry { name = "microservice: CampaignService", kind = "beam" };

		var tokens = LocalStackStopCommand.BuildKillTokens(entry);

		Assert.That(tokens, Is.EqualTo(new[] { "CampaignService" }));
	}

	[Test]
	public void Unknown_or_docker_step_yields_no_tokens()
	{
		Assert.That(LocalStackStopCommand.BuildKillTokens(
			new LocalStackRunEntry { name = "docker: api deps + caddy", kind = "docker" }), Is.Empty);
		Assert.That(LocalStackStopCommand.BuildKillTokens(new LocalStackRunEntry { name = "portal" }), Is.Empty);
	}
}
