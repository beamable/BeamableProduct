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
/// <c>beam local run &lt;stack&gt;:remote-ms</c> (see <see cref="RemoteMicroservices"/>): the variant, the developer's
/// name, the changes to this run's steps, and the tunnel step's script.
/// </summary>
public class LocalStackRemoteMicroservicesTests
{
	/// <summary>The settings <c>beam local setup</c> writes, with the tunnel step <c>run</c> adds to them.</summary>
	private static LocalStackConfig TemplateConfig(string apiDir = "/repos/BeamableAPI")
	{
		var config = LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			apiDir = apiDir,
			scalaDir = "/repos/BeamableBackend",
			portalDir = "/repos/portal",
		});
		RemoteMicroservices.AddTunnelStep(config.steps);
		return config;
	}

	private static LocalStackStep Step(LocalStackConfig config, string name) => config.steps.Single(s => s.name == name);

	private const string RoleArn = "arn:aws:iam::123456789012:role/example-tunnel-user";
	private const string ParameterArn = "arn:aws:ssm:us-west-2:123456789012:parameter/example/settings";

	/// <summary>The shape the infrastructure's settings parameter has, with placeholder values.</summary>
	private const string ParameterJson = @"{
		""region"": ""us-west-2"",
		""domain"": ""ms.example.com"",
		""tunnel"": { ""instanceName"": ""example-tunnel"", ""sshDocument"": ""example-ssh"", ""user"": ""tunnel"" },
		""beamo"": {
			""Subnets"": [""subnet-a"", ""subnet-b""],
			""ShouldAssignPublicIp"": false,
			""TaskRoleArn"": ""arn:aws:iam::123456789012:role/example-task"",
			""DockerRegistrySecretArn"": ""arn:aws:secretsmanager:us-west-2:123456789012:secret:example/registry-AbCdEf"",
			""WebsocketUri"": ""wss://not-this/socket""
		}
	}";

	private static RemoteMsSettings Settings(string name, string json = ParameterJson)
	{
		var (settings, problem) = RemoteMsSettings.Parse(json, RoleArn, name);
		Assert.That(problem, Is.Null);
		return settings;
	}

	[TestCase("arn:aws:sts::123456789012:assumed-role/AWSReservedSSO_Developer_0123/jane@example.com", "jane")]
	[TestCase("arn:aws:iam::123456789012:user/jane-cli", "jane-cli")]
	[TestCase("arn:aws:sts::1:assumed-role/AWSReservedSSO_Developer_0123/Jane.Doe@example.com", "jane-doe")]
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
		// <dev>-clickhouse is the longest of the developer's names.
		Assert.That(Settings(name).ClickHouseHost.Split('.')[0].Length, Is.EqualTo(63));
	}

	[TestCase("justin", true)]
	[TestCase("jane-cli", true)]
	[TestCase("", false)]
	[TestCase("-justin", false)]
	[TestCase("justin-", false)]
	[TestCase("Justin", false)]
	[TestCase("jus_tin", false)]
	[TestCase("bob-registry", false)] // would collide with bob's registry name
	[TestCase("bob-clickhouse", false)] // would collide with bob's ClickHouse name
	public void ValidateName(string name, bool valid)
	{
		Assert.That(RemoteMicroservices.ValidateName(name) == null, Is.EqualTo(valid));
	}

	[Test]
	public void RemoteMsVariant_AddsTheTunnelAfterTheStacksOwnSteps()
	{
		var config = TemplateConfig();

		var steps = LocalStackStacks.BuiltIn().Resolve("full:remote-ms", config).steps;

		Assert.That(steps.Last().name, Is.EqualTo(RemoteMicroservices.TunnelStepName));
		Assert.That(RemoteMicroservices.Runs(steps), Is.True);
		Assert.That(RemoteMicroservices.Runs(LocalStackStacks.BuiltIn().Resolve("full", config).steps), Is.False,
			"only the variant turns it on");
	}

	[Test]
	public void AddTunnelStep_OnlyWhereTheBackendsDockerStepIs()
	{
		var steps = new List<LocalStackStep> { new LocalStackStep { name = "portal" } };

		RemoteMicroservices.AddTunnelStep(steps);

		Assert.That(RemoteMicroservices.Runs(steps), Is.False);
	}

	[Test]
	public void AddTunnelStep_Twice_AddsOne()
	{
		var config = TemplateConfig();

		RemoteMicroservices.AddTunnelStep(config.steps);

		Assert.That(config.steps.Count(s => s.name == RemoteMicroservices.TunnelStepName), Is.EqualTo(1));
		var tunnel = Step(config, RemoteMicroservices.TunnelStepName);
		Assert.That(tunnel.shell, Is.True);
		Assert.That(tunnel.waitForExit, Is.False);
		Assert.That(tunnel.readyWhenLogContains, Is.EqualTo(RemoteMicroservices.TunnelReadyMarker));
		Assert.That(LocalStackStacks.BuiltIn().TagsOf(tunnel), Is.Empty, "no stack picks it up by a tag");
	}

	[Test]
	public void Apply_LayersTheRegistryOntoTheDockerStepEverywhereItRuns()
	{
		var config = TemplateConfig();

		RemoteMicroservices.Apply(config.steps, Settings("justin"));

		var docker = Step(config, RemoteMicroservices.DockerStepName);
		const string files = "compose -f docker-compose.yml -f docker-compose.remote-ms.yml ";
		Assert.That(docker.arguments, Is.EqualTo(files + "up -d --wait"));
		Assert.That(docker.stopArguments, Is.EqualTo(files + "stop"));
		Assert.That(docker.purgeStopArguments, Is.EqualTo(files + "down -v"));
		Assert.That(docker.environment["MS_REGISTRY_HOST"], Is.EqualTo("justin-registry.ms.example.com"));
		Assert.That(docker.environment["MS_CLICKHOUSE_HOST"], Is.EqualTo("justin-clickhouse.ms.example.com"));
		Assert.That(docker.environment, Does.ContainKey(LocalStackTemplate.LogRetentionVariable), "the template's own settings stay");
	}

	[Test]
	public void Apply_PointsTheGatewayAtTheDevelopersHostNames()
	{
		var config = TemplateConfig();

		RemoteMicroservices.Apply(config.steps, Settings("justin"));

		var env = Step(config, RemoteMicroservices.GatewayStepName).environment;
		Assert.That(env["BEAMABLE_Beamo__WebsocketUri"], Is.EqualTo("wss://justin.ms.example.com/socket"));
		Assert.That(env["BEAMABLE_Beamo__DockerRegistryHostAndPort"], Is.EqualTo("justin-registry.ms.example.com:443"));
		Assert.That(env["BEAMABLE_Beamo__DockerRegistryProtocol"], Is.EqualTo("https"));
		// Deployed services' collectors are handed this endpoint, so their telemetry reaches the local ClickHouse.
		Assert.That(env["BEAMABLE_Beamo__Otel__ClickHouse__Endpoint__Host"], Is.EqualTo("justin-clickhouse.ms.example.com"));
		Assert.That(env["BEAMABLE_Beamo__Otel__ClickHouse__Endpoint__Port"], Is.EqualTo("443"));
		Assert.That(env["BEAMABLE_Beamo__Otel__ClickHouse__Endpoint__Protocol"], Is.EqualTo("https"));
		Assert.That(env["ASPNETCORE_ENVIRONMENT"], Is.EqualTo("Local"), "the template's own settings stay");
	}

	[Test]
	public void Apply_GivesTheTunnelStepItsScript()
	{
		var config = TemplateConfig();

		RemoteMicroservices.Apply(config.steps, Settings("justin"));

		Assert.That(Step(config, RemoteMicroservices.TunnelStepName).arguments, Is.EqualTo(RemoteMicroservices.TunnelScript(Settings("justin"))));
	}

	[Test]
	public void Apply_Twice_ChangesNothingMore()
	{
		var once = TemplateConfig();
		RemoteMicroservices.Apply(once.steps, Settings("justin"));
		var twice = TemplateConfig();
		RemoteMicroservices.Apply(twice.steps, Settings("justin"));
		RemoteMicroservices.Apply(twice.steps, Settings("justin"));

		Assert.That(twice.steps.Select(s => s.name), Is.EqualTo(once.steps.Select(s => s.name)));
		foreach (var name in new[] { RemoteMicroservices.DockerStepName, RemoteMicroservices.GatewayStepName, RemoteMicroservices.TunnelStepName })
		{
			Assert.That(Step(twice, name).arguments, Is.EqualTo(Step(once, name).arguments), name);
			Assert.That(Step(twice, name).stopArguments, Is.EqualTo(Step(once, name).stopArguments), name);
			Assert.That(Step(twice, name).environment, Is.EquivalentTo(Step(once, name).environment), name);
		}
	}

	[Test]
	public void Apply_WithoutTheBackendsSteps_NamesTheMissingOne()
	{
		var steps = new List<LocalStackStep> { new LocalStackStep { name = RemoteMicroservices.TunnelStepName } };

		var e = Assert.Throws<CliException>(() => RemoteMicroservices.Apply(steps, Settings("justin")));
		Assert.That(e.Message, Does.Contain(RemoteMicroservices.DockerStepName));
	}

	[Test]
	public void StackProblems_NeedTheBackendHere()
	{
		var config = TemplateConfig();
		var backend = LocalStackStacks.BuiltIn().Resolve("backend:remote-ms", config).steps;
		var portalOnly = config.steps.Where(s => s.name.StartsWith("portal")).ToList();

		Assert.That(RemoteMicroservices.StackProblems(backend, runsLocal: true), Is.Empty);
		Assert.That(RemoteMicroservices.StackProblems(backend, runsLocal: false).Single(), Does.Contain("remote one"));
		var missing = RemoteMicroservices.StackProblems(portalOnly, runsLocal: true).Single();
		Assert.That(missing, Does.Contain(RemoteMicroservices.DockerStepName).And.Contain(RemoteMicroservices.GatewayStepName));
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
		RemoteMicroservices.Apply(config.steps, Settings("justin"));

		var docker = LocalStackRunCommand.ReversalEnvironment(Step(config, RemoteMicroservices.DockerStepName), config);
		var gateway = LocalStackRunCommand.ReversalEnvironment(Step(config, RemoteMicroservices.GatewayStepName), config);

		Assert.That(docker["MS_REGISTRY_HOST"], Is.EqualTo("justin-registry.ms.example.com"),
			"stop runs the override too, which won't parse without it");
		Assert.That(docker["MS_CLICKHOUSE_HOST"], Is.EqualTo("justin-clickhouse.ms.example.com"));
		Assert.That(docker[LocalStackTemplate.LogRetentionVariable], Does.Not.Contain("${"), "tokens are substituted");
		Assert.That(gateway, Is.Null, "the gateway has no reversal, so nothing is recorded");
	}

	[Test]
	public void TunnelScript_ForwardsAllThreeSocketsAndSurvivesTokenSubstitution()
	{
		var script = RemoteMicroservices.TunnelScript(Settings("justin"));

		Assert.That(script, Does.Contain("dev='justin'"));
		Assert.That(script, Does.Contain("-R \"/run/tunnels/$dev.sock:127.0.0.1:8080\""));
		Assert.That(script, Does.Contain("-R \"/run/tunnels/$dev-registry.sock:127.0.0.1:5002\""));
		Assert.That(script, Does.Contain("-R \"/run/tunnels/$dev-clickhouse.sock:127.0.0.1:8123\""));
		Assert.That(script, Does.Contain("LocalCommand=echo " + RemoteMicroservices.TunnelReadyMarker));
		// The tunnel role may only start sessions with this document, which fixes the port at 22.
		Assert.That(script, Does.Contain("ssh_document='example-ssh'"));
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
			Assert.Ignore("POSIX sh only; :remote-ms refuses to run on Windows.");
		}

		var path = Path.GetTempFileName();
		try
		{
			File.WriteAllText(path, RemoteMicroservices.TunnelScript(Settings("justin")));
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

	[Test]
	public void Parse_TurnsBeamoIntoTheGatewaysVariables()
	{
		var env = Settings("justin").GatewayEnvironment;

		Assert.That(env["BEAMABLE_Beamo__Subnets__0"], Is.EqualTo("subnet-a"));
		Assert.That(env["BEAMABLE_Beamo__Subnets__1"], Is.EqualTo("subnet-b"));
		Assert.That(env["BEAMABLE_Beamo__ShouldAssignPublicIp"], Is.EqualTo("false"));
		Assert.That(env["BEAMABLE_Beamo__TaskRoleArn"], Is.EqualTo("arn:aws:iam::123456789012:role/example-task"));
		Assert.That(env["BEAMABLE_Beamo__DockerRegistrySecretArn"], Does.EndWith("example/registry-AbCdEf"));
	}

	[Test]
	public void Parse_ReadsTheTunnelAndDomain()
	{
		var settings = Settings("justin");

		Assert.That(settings.Region, Is.EqualTo("us-west-2"));
		Assert.That(settings.TunnelInstanceName, Is.EqualTo("example-tunnel"));
		Assert.That(settings.SshDocument, Is.EqualTo("example-ssh"));
		Assert.That(settings.TunnelUser, Is.EqualTo("tunnel"));
		Assert.That(settings.RoleArn, Is.EqualTo(RoleArn));
		Assert.That(settings.GatewayHost, Is.EqualTo("justin.ms.example.com"));
	}

	// The tunnel script puts these between single quotes and through token substitution.
	[TestCase("\"instanceName\": \"example-tunnel\"", "\"instanceName\": \"x'; rm -rf ~; '\"", "tunnel.instanceName")]
	[TestCase("\"sshDocument\": \"example-ssh\"", "\"sshDocument\": \"${HOME}\"", "tunnel.sshDocument")]
	[TestCase("\"region\": \"us-west-2\"", "\"region\": 5", "region")]
	[TestCase("\"user\": \"tunnel\"", "\"user\": \"\"", "tunnel.user")]
	[TestCase("\"domain\": \"ms.example.com\"", "\"domain\": \"ms example\"", "domain")]
	[TestCase("\"beamo\": {", "\"notBeamo\": {", "beamo")]
	public void Parse_RejectsSettingsTheScriptCantCarry(string from, string to, string named)
	{
		var (settings, problem) = RemoteMsSettings.Parse(ParameterJson.Replace(from, to), RoleArn, "justin");

		Assert.That(settings, Is.Null);
		Assert.That(problem, Does.Contain(named));
	}

	[Test]
	public void Parse_RejectsWhatIsntJson()
	{
		Assert.That(RemoteMsSettings.Parse("ParameterNotFound", RoleArn, "justin").problem, Does.Contain("JSON"));
	}

	[Test]
	public void Apply_GivesTheGatewayTheSharedSettings_ButItsOwnHostNamesWin()
	{
		var config = TemplateConfig();

		RemoteMicroservices.Apply(config.steps, Settings("justin"));

		var env = Step(config, RemoteMicroservices.GatewayStepName).environment;
		Assert.That(env["BEAMABLE_Beamo__Subnets__1"], Is.EqualTo("subnet-b"));
		Assert.That(env["BEAMABLE_Beamo__DockerRegistrySecretArn"], Does.EndWith("example/registry-AbCdEf"));
		Assert.That(env["BEAMABLE_Beamo__WebsocketUri"], Is.EqualTo("wss://justin.ms.example.com/socket"));
	}

	[Test]
	public void Locate_ReadsTheFileInTheBackendsCheckout()
	{
		var apiDir = Directory.CreateTempSubdirectory().FullName;
		try
		{
			var config = TemplateConfig(apiDir);
			File.WriteAllText(Path.Combine(apiDir, RemoteMicroservices.SettingsFile),
				$"{{ \"roleArn\": \"{RoleArn}\", \"settingsParameter\": \"{ParameterArn}\" }}");

			var (found, problem) = RemoteMicroservices.Locate(config, config.steps);

			Assert.That(problem, Is.Null);
			Assert.That(found, Is.EqualTo((RoleArn, ParameterArn)));
		}
		finally
		{
			Directory.Delete(apiDir, true);
		}
	}

	[Test]
	public void Locate_SettingsVariablesWinOverTheFile()
	{
		var apiDir = Directory.CreateTempSubdirectory().FullName;
		try
		{
			var config = TemplateConfig(apiDir);
			File.WriteAllText(Path.Combine(apiDir, RemoteMicroservices.SettingsFile),
				$"{{ \"roleArn\": \"{RoleArn}\", \"settingsParameter\": \"{ParameterArn}\" }}");
			config.variables ??= new Dictionary<string, string>();
			config.variables[RemoteMicroservices.RoleArnVariable] = "arn:aws:iam::210987654321:role/other";

			var (found, _) = RemoteMicroservices.Locate(config, config.steps);

			Assert.That(found, Is.EqualTo(("arn:aws:iam::210987654321:role/other", ParameterArn)));
		}
		finally
		{
			Directory.Delete(apiDir, true);
		}
	}

	[Test]
	public void Locate_WithoutTheFile_SaysWhereItLooked()
	{
		var apiDir = Directory.CreateTempSubdirectory().FullName;
		try
		{
			var config = TemplateConfig(apiDir);

			var (_, problem) = RemoteMicroservices.Locate(config, config.steps);

			Assert.That(problem, Does.Contain(Path.Combine(apiDir, RemoteMicroservices.SettingsFile)).And.Contain("missing"));
		}
		finally
		{
			Directory.Delete(apiDir, true);
		}
	}

	[TestCase("arn:aws:iam::123456789012:role/x' ; true", "arn:aws:ssm:us-west-2:123456789012:parameter/p", "IAM role")]
	[TestCase("arn:aws:iam::123456789012:role/x", "/p", "SSM parameter")]
	public void Locate_RejectsWhatIsntAnArn(string roleArn, string parameterArn, string named)
	{
		var config = TemplateConfig();
		config.variables ??= new Dictionary<string, string>();
		config.variables[RemoteMicroservices.RoleArnVariable] = roleArn;
		config.variables[RemoteMicroservices.SettingsParameterVariable] = parameterArn;

		Assert.That(RemoteMicroservices.Locate(config, config.steps).problem, Does.Contain(named));
	}

	[Test]
	public void GetParameterArgs_TakeTheRegionAndNameFromTheArn()
	{
		Assert.That(RemoteMicroservices.GetParameterArgs(ParameterArn),
			Is.EqualTo("ssm get-parameter --region us-west-2 --name /example/settings --query Parameter.Value --output text"));
	}

	[Test]
	public void HostsEntry_SendsTheNamesHereAndIsMarked()
	{
		var settings = Settings("justin");

		Assert.That(RemoteMicroservices.HostsEntry(new[] { settings.RegistryHost, settings.ClickHouseHost }),
			Is.EqualTo("127.0.0.1 justin-registry.ms.example.com justin-clickhouse.ms.example.com " + RemoteMicroservices.HostsMarker));
	}

	[Test]
	public void Unresolved_IsOnlyTheNamesThatDontResolveHere()
	{
		var settings = Settings("justin");

		Assert.That(RemoteMicroservices.Unresolved(settings, host => host == settings.RegistryHost),
			Is.EqualTo(new[] { settings.ClickHouseHost }));
		Assert.That(RemoteMicroservices.Unresolved(settings, _ => true), Is.Empty);
		Assert.That(RemoteMicroservices.Unresolved(settings, _ => false),
			Is.EqualTo(new[] { settings.RegistryHost, settings.ClickHouseHost }), "the gateway name is never looked up here");
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
