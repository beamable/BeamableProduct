using System;
using System.Linq;
using cli.Services.LocalStack;
using NUnit.Framework;

namespace tests;

/// <summary>
/// Verifies that `beam local init` (via <see cref="LocalStackTemplate.Create"/>) emits the opt-in build
/// steps for the components `beam local up` does not otherwise build — the C# gateway, the Scala services,
/// and the portal node deps — each immediately before its run step and marked build+waitForExit so
/// `up --build` runs them and plain `up` skips them.
/// </summary>
public class LocalStackBuildStepTests
{
	private static LocalStackConfig CreateWithRepos() => LocalStackTemplate.Create(new LocalStackTemplate.Options
	{
		apiDir = @"C:\repos\BeamableAPI",
		scalaDir = @"C:\repos\BeamableBackend",
		portalDir = @"C:\repos\portal",
		scalaTools = new System.Collections.Generic.List<LocalStackTemplate.ScalaToolInfo>
		{
			new() { name = "gateway", mainClass = "com.beamable.gateway.App" },
			new() { name = "auth", mainClass = "com.beamable.auth.App" },
		},
	});

	private static LocalStackStep Step(LocalStackConfig c, string name) =>
		c.steps.FirstOrDefault(s => s.name == name);

	private static int IndexOf(LocalStackConfig c, string name) =>
		c.steps.FindIndex(s => s.name == name);

	[Test]
	public void Emits_three_build_steps_marked_build_and_waitForExit()
	{
		var config = CreateWithRepos();

		foreach (var name in new[] { "build: c# gateway", "build: scala", "build: portal deps" })
		{
			var step = Step(config, name);
			Assert.That(step, Is.Not.Null, $"missing {name}");
			Assert.That(step.build, Is.True, $"{name} must be a build step");
			Assert.That(step.waitForExit, Is.True, $"{name} must run to completion");
		}
	}

	[Test]
	public void Each_build_step_precedes_its_run_step()
	{
		var config = CreateWithRepos();

		Assert.That(IndexOf(config, "build: c# gateway"), Is.LessThan(IndexOf(config, "c# gateway")));
		Assert.That(IndexOf(config, "build: portal deps"), Is.LessThan(IndexOf(config, "portal frontend")));
		Assert.That(IndexOf(config, "build: scala"), Is.LessThan(IndexOf(config, "scala: gateway")));
	}

	[Test]
	public void Gateway_build_runs_dotnet_build_in_the_api_repo()
	{
		var step = Step(CreateWithRepos(), "build: c# gateway");

		Assert.That(step.command, Is.EqualTo("dotnet"));
		Assert.That(step.arguments, Does.Contain("build BeamableGateway"));
		Assert.That(step.workingDirectory, Is.EqualTo(@"C:\repos\BeamableAPI"));
	}

	[Test]
	public void Scala_build_packages_selected_modules_with_java_home()
	{
		var step = Step(CreateWithRepos(), "build: scala");

		Assert.That(step.command, Is.EqualTo(OperatingSystem.IsWindows() ? "mvn.cmd" : "mvn"));
		Assert.That(step.arguments, Does.Contain("package"));
		Assert.That(step.arguments, Does.Contain("tools/gateway"));
		Assert.That(step.arguments, Does.Contain("tools/auth"));
		Assert.That(step.arguments, Does.Contain("-am"));
		Assert.That(step.workingDirectory, Is.EqualTo(@"C:\repos\BeamableBackend"));
		Assert.That(step.environment.TryGetValue("JAVA_HOME", out var jh) && jh == "${java}", Is.True,
			"scala build must run under the Java 8 home substituted by `up`");
	}

	[Test]
	public void Portal_build_runs_npm_install_in_the_portal_repo()
	{
		var step = Step(CreateWithRepos(), "build: portal deps");

		Assert.That(step.command, Is.EqualTo(OperatingSystem.IsWindows() ? "npm.cmd" : "npm"));
		Assert.That(step.arguments, Is.EqualTo("install"));
		Assert.That(step.workingDirectory, Is.EqualTo(@"C:\repos\portal"));
	}

	[Test]
	public void Run_steps_are_not_marked_build()
	{
		var config = CreateWithRepos();

		foreach (var name in new[] { "c# gateway", "portal frontend", "scala: gateway" })
			Assert.That(Step(config, name).build, Is.False, $"{name} must not be a build step");
	}
}
