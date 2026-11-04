using System.Collections.Generic;
using System.Linq;
using cli.Commands.LocalStack;
using cli.Services.LocalStack;
using NUnit.Framework;

namespace tests;

/// <summary>
/// The <c>c#: clickhouse migrations</c> step (see <see cref="LocalStackTemplate.AddClickHouseMigrationsStep"/>): it
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

	[Test]
	public void AddTo_GivesSettingsTheMigrationsOnce()
	{
		var config = Catalogue();

		new WorkspaceServices().AddTo(config.steps);

		var step = config.steps.Single(s => s.name == LocalStackTemplate.ClickHouseMigrationsStepName);
		var docker = config.steps.Single(s => s.name == LocalStackTemplate.ApiDepsStepName);
		Assert.That(step.workingDirectory, Is.EqualTo(docker.workingDirectory), "it runs from the BeamableAPI checkout");
		Assert.That(step.arguments, Is.EqualTo("run --project Beamable.Database.Management -- migrate"));
		Assert.That(step.environment["DOTNET_ENVIRONMENT"], Is.EqualTo("Local"));
		Assert.That(step.dependsOn, Is.EqualTo(new[] { LocalStackTemplate.ApiDepsStepName }));
		Assert.That(step.waitForExit, Is.True);
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
