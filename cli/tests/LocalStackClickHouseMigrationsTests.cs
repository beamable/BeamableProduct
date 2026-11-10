using System.Collections.Generic;
using System.Linq;
using cli.Commands.LocalStack;
using cli.Services.LocalStack;
using NUnit.Framework;

namespace tests;

/// <summary>
/// The <c>c# clickhouse migrations</c> step and its build (see <see cref="LocalStackTemplate.AddClickHouseMigrationsStep"/>): it
/// reaches settings written before it existed, and the stacks that run the C# hosts.
/// </summary>
public class LocalStackClickHouseMigrationsTests
{
	/// <summary>The settings <c>beam local setup</c> writes, read the way every catalogue reader reads them.</summary>
	private static LocalStackConfig Catalogue()
	{
		var config = LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			apiDir = "/repos/BeamableAPI",
			scalaDir = "/repos/BeamableBackend",
			portalDir = "/repos/portal",
		});
		new WorkspaceServices().AddTo(config.steps);
		return config;
	}

	private static string Build => "build: " + LocalStackTemplate.ClickHouseMigrationsStepName;

	[Test]
	public void AddTo_GivesSettingsTheMigrationsOnce()
	{
		var config = Catalogue();

		new WorkspaceServices().AddTo(config.steps);

		var run = config.steps.Single(s => s.name == LocalStackTemplate.ClickHouseMigrationsStepName);
		var build = config.steps.Single(s => s.name == Build);
		var docker = config.steps.Single(s => s.name == LocalStackTemplate.ApiDepsStepName);
		Assert.That(build.build, Is.True);
		Assert.That(build.workingDirectory, Is.EqualTo(docker.workingDirectory), "it builds in the BeamableAPI checkout");
		Assert.That(build.arguments, Is.EqualTo("build Beamable.Database.Management -c Debug"));
		Assert.That(run.workingDirectory, Does.StartWith(docker.workingDirectory).And.Contain("Beamable.Database.Management"),
			"it runs from its output directory, where its appsettings are");
		Assert.That(build.requiredOutput, Does.StartWith(run.workingDirectory), "the build produces what the run launches");
		Assert.That(run.arguments, Is.EqualTo("migrate"));
		Assert.That(run.environment["DOTNET_ENVIRONMENT"], Is.EqualTo("Local"));
		Assert.That(run.dependsOn, Is.EqualTo(new[] { LocalStackTemplate.ApiDepsStepName }));
		Assert.That(run.waitForExit, Is.True);
	}

	[Test]
	public void Stacks_RunTheMigrationsAfterTheDockerDepsAndBeforeTheGateway()
	{
		var config = Catalogue();
		var steps = LocalStackStacks.BuiltIn().Resolve("full", config).steps.Select(s => s.name).ToList();

		var docker = steps.IndexOf(LocalStackTemplate.ApiDepsStepName);
		var migrations = steps.IndexOf(LocalStackTemplate.ClickHouseMigrationsStepName);
		var gateway = steps.IndexOf(LocalStackDependencies.GatewayStep);
		Assert.That(docker, Is.GreaterThanOrEqualTo(0));
		Assert.That(migrations, Is.GreaterThan(docker), "ClickHouse has to be up");
		Assert.That(migrations, Is.LessThan(gateway), "the tables exist before anything writes telemetry");
	}

	[Test]
	public void Builds_ShareBeamableApisLaneWithTheGateway()
	{
		var config = Catalogue();
		var steps = LocalStackStacks.BuiltIn().Resolve("full", config).steps;

		var lane = LocalStackRunCommand.BuildLanes(steps, config).Single(l => l.Any(s => s.name == Build)).Select(s => s.name).ToList();

		Assert.That(lane, Does.Contain("build: " + LocalStackDependencies.GatewayStep), "one lane: never two builds of the shared projects at once");
		Assert.That(lane.IndexOf(Build), Is.LessThan(lane.IndexOf("build: " + LocalStackDependencies.GatewayStep)));
	}

	[Test]
	public void AddTo_LeavesSettingsWithoutTheBackendAlone()
	{
		var steps = new List<LocalStackStep> { new LocalStackStep { name = LocalStackTemplate.PortalFrontendStepName } };

		new WorkspaceServices().AddTo(steps);

		Assert.That(steps.Any(s => s.name == LocalStackTemplate.ClickHouseMigrationsStepName), Is.False);
	}

	[TestCase("full", true)]
	[TestCase("backend", true)]
	[TestCase("csharp-portal", true)]
	[TestCase("portal-extensions", false)] // against the dev backend: no local ClickHouse
	[TestCase("remote", false)]
	public void Stacks_ThatRunTheCSharpHostsRunIt(string stack, bool runs)
	{
		var steps = LocalStackStacks.BuiltIn().Resolve(stack, Catalogue()).steps;

		Assert.That(steps.Any(s => s.name == LocalStackTemplate.ClickHouseMigrationsStepName), Is.EqualTo(runs));
	}
}
