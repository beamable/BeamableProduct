using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using cli;
using cli.Commands.LocalStack;
using cli.Services.LocalStack;
using NUnit.Framework;

namespace tests;

/// <summary>
/// <c>beam local up --remote-microservices</c> (see <see cref="RemoteMicroservices"/>): the developer's name, the
/// changes to this run's manifest, and the tunnel step's script.
/// </summary>
public class LocalStackRemoteMicroservicesTests
{
	private static LocalStackConfig TemplateConfig() =>
		LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			apiDir = "/repos/BeamableAPI",
			scalaDir = "/repos/BeamableBackend",
			portalDir = "/repos/portal",
		});

	private static LocalStackStep Step(LocalStackConfig config, string name) => config.steps.Single(s => s.name == name);

	[TestCase("arn:aws:sts::677138418699:assumed-role/AWSReservedSSO_Developer_0123/justin@beamable.com", "justin")]
	[TestCase("arn:aws:iam::677138418699:user/jlefebvre-cli", "jlefebvre-cli")]
	[TestCase("arn:aws:sts::1:assumed-role/AWSReservedSSO_Developer_0123/Justin.LeFebvre@beamable.com", "justin-lefebvre")]
	[TestCase("arn:aws:iam::1:user/_svc_", "svc")]
	[TestCase(null, null)]
	[TestCase("arn:aws:iam::1:user/___", null)]
	public void DeriveName_TurnsTheCallerArnIntoADnsLabel(string arn, string expected)
	{
		Assert.That(RemoteMicroservices.DeriveName(arn), Is.EqualTo(expected));
	}

	[Test]
	public void DeriveName_TruncatesSoTheRegistryNameStillFitsOneLabel()
	{
		var name = RemoteMicroservices.DeriveName("arn:aws:iam::1:user/" + new string('a', 80));

		Assert.That(RemoteMicroservices.ValidateName(name), Is.Null);
		Assert.That(RemoteMicroservices.RegistryHost(name).Split('.')[0].Length, Is.LessThanOrEqualTo(63));
	}

	[TestCase("justin", true)]
	[TestCase("jlefebvre-cli", true)]
	[TestCase("", false)]
	[TestCase("-justin", false)]
	[TestCase("justin-", false)]
	[TestCase("Justin", false)]
	[TestCase("jus_tin", false)]
	[TestCase("bob-registry", false)] // would collide with bob's registry name
	public void ValidateName(string name, bool valid)
	{
		Assert.That(RemoteMicroservices.ValidateName(name) == null, Is.EqualTo(valid));
	}

	[Test]
	public void Apply_LayersTheRegistryOntoTheDockerStepEverywhereItRuns()
	{
		var config = TemplateConfig();

		RemoteMicroservices.Apply(config, "justin");

		var docker = Step(config, RemoteMicroservices.DockerStepName);
		const string files = "compose -f docker-compose.yml -f docker-compose.remote-ms.yml ";
		Assert.That(docker.arguments, Is.EqualTo(files + "up -d --wait"));
		Assert.That(docker.stopArguments, Is.EqualTo(files + "stop"));
		Assert.That(docker.purgeStopArguments, Is.EqualTo(files + "down -v"));
		Assert.That(docker.environment["MS_REGISTRY_HOST"], Is.EqualTo("justin-registry.ms-local.beamable.com"));
	}

	[Test]
	public void Apply_PointsTheGatewayAtTheDevelopersHostNames()
	{
		var config = TemplateConfig();

		RemoteMicroservices.Apply(config, "justin");

		var env = Step(config, RemoteMicroservices.GatewayStepName).environment;
		Assert.That(env["BEAMABLE_Beamo__WebsocketUri"], Is.EqualTo("wss://justin.ms-local.beamable.com/socket"));
		Assert.That(env["BEAMABLE_Beamo__DockerRegistryHostAndPort"], Is.EqualTo("justin-registry.ms-local.beamable.com:443"));
		Assert.That(env["BEAMABLE_Beamo__DockerRegistryProtocol"], Is.EqualTo("https"));
		Assert.That(env["ASPNETCORE_ENVIRONMENT"], Is.EqualTo("Local"), "the template's own settings stay");
	}

	[Test]
	public void Apply_AddsTheTunnelStepRightAfterTheGateway()
	{
		var config = TemplateConfig();

		RemoteMicroservices.Apply(config, "justin");

		var gatewayIndex = config.steps.FindIndex(s => s.name == RemoteMicroservices.GatewayStepName);
		var tunnel = config.steps[gatewayIndex + 1];
		Assert.That(tunnel.name, Is.EqualTo(RemoteMicroservices.TunnelStepName));
		Assert.That(tunnel.shell, Is.True);
		Assert.That(tunnel.waitForExit, Is.False);
		Assert.That(tunnel.readyWhenLogContains, Is.EqualTo(RemoteMicroservices.TunnelReadyMarker));
	}

	[Test]
	public void Apply_Twice_ChangesNothingMore()
	{
		var once = TemplateConfig();
		RemoteMicroservices.Apply(once, "justin");
		var twice = TemplateConfig();
		RemoteMicroservices.Apply(twice, "justin");
		RemoteMicroservices.Apply(twice, "justin");

		Assert.That(twice.steps.Select(s => s.name), Is.EqualTo(once.steps.Select(s => s.name)));
		Assert.That(Step(twice, RemoteMicroservices.DockerStepName).arguments,
			Is.EqualTo(Step(once, RemoteMicroservices.DockerStepName).arguments));
	}

	[Test]
	public void Apply_WithoutTheTemplateSteps_SaysToRegenerateTheManifest()
	{
		var config = new LocalStackConfig { steps = new List<LocalStackStep>() };

		var e = Assert.Throws<CliException>(() => RemoteMicroservices.Apply(config, "justin"));
		Assert.That(e.Message, Does.Contain("beam local init"));
	}

	[Test]
	public void WithOverride_RejectsSomethingThatIsNotACompose()
	{
		Assert.Throws<CliException>(() => RemoteMicroservices.WithOverride("run --rm registry"));
	}

	[Test]
	public void ReversalEnvironment_IsRecordedOnlyForStepsStopCanReverse()
	{
		var config = TemplateConfig();
		RemoteMicroservices.Apply(config, "justin");

		var docker = LocalStackUpCommand.ReversalEnvironment(Step(config, RemoteMicroservices.DockerStepName), config);
		var gateway = LocalStackUpCommand.ReversalEnvironment(Step(config, RemoteMicroservices.GatewayStepName), config);

		Assert.That(docker["MS_REGISTRY_HOST"], Is.EqualTo("justin-registry.ms-local.beamable.com"),
			"stop runs the override too, which won't parse without it");
		Assert.That(gateway, Is.Null, "the gateway has no reversal, so nothing is recorded");
	}

	[Test]
	public void TunnelScript_ForwardsBothSocketsAndSurvivesTokenSubstitution()
	{
		var script = RemoteMicroservices.TunnelScript("justin");

		Assert.That(script, Does.Contain("dev='justin'"));
		Assert.That(script, Does.Contain("-R \"/run/tunnels/$dev.sock:127.0.0.1:8080\""));
		Assert.That(script, Does.Contain("-R \"/run/tunnels/$dev-registry.sock:127.0.0.1:5002\""));
		Assert.That(script, Does.Contain("LocalCommand=echo " + RemoteMicroservices.TunnelReadyMarker));
		// The tunnel role may only start sessions with this document, which fixes the port at 22.
		Assert.That(script, Does.Contain($"ssh_document='{RemoteMicroservices.SshDocumentName}'"));
		Assert.That(script, Does.Contain("--document-name $ssh_document\""));
		Assert.That(script, Does.Not.Contain("portNumber"));
		// Step arguments go through LocalStackConfigIO.Substitute, which rewrites ${...} tokens.
		Assert.That(script, Does.Not.Contain("${"));
	}

	[Test]
	public void TunnelScript_IsValidSh()
	{
		if (System.OperatingSystem.IsWindows())
		{
			Assert.Ignore("POSIX sh only; --remote-microservices refuses to run on Windows.");
		}

		var path = Path.GetTempFileName();
		try
		{
			File.WriteAllText(path, RemoteMicroservices.TunnelScript("justin"));
			using var sh = Process.Start(new ProcessStartInfo("sh", $"-n \"{path}\"") { RedirectStandardError = true });
			var errors = sh!.StandardError.ReadToEnd();
			sh.WaitForExit();

			Assert.That(sh.ExitCode, Is.EqualTo(0), errors);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[TestCase("Error when retrieving token from sso: Token has expired and refresh failed", true)]
	[TestCase("The SSO session associated with this profile has expired or is otherwise invalid.", true)]
	[TestCase("An error occurred (ExpiredToken) when calling the GetCallerIdentity operation", false)]
	[TestCase("Unable to locate credentials.", false)]
	[TestCase(null, false)]
	public void LooksLikeExpiredSso(string output, bool expected)
	{
		Assert.That(RemoteMicroservices.LooksLikeExpiredSso(output), Is.EqualTo(expected));
	}
}
