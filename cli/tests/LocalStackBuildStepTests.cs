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

	private static LocalStackConfig CreateWithWebRegistry() => LocalStackTemplate.Create(new LocalStackTemplate.Options
	{
		apiDir = @"C:\repos\BeamableAPI",
		scalaDir = @"C:\repos\BeamableBackend",
		portalDir = @"C:\repos\portal",
		includeWebRegistry = true,
		webRegistryDir = @"C:\repos\BeamableProduct\portal-localdev",
		extensions = new System.Collections.Generic.List<string> { "my-ext" },
		scalaTools = new System.Collections.Generic.List<LocalStackTemplate.ScalaToolInfo>
		{
			new() { name = "gateway", mainClass = "com.beamable.gateway.App" },
			new() { name = "auth", mainClass = "com.beamable.auth.App" },
		},
	});

	[Test]
	public void Web_steps_are_omitted_unless_the_web_registry_is_included()
	{
		var config = CreateWithRepos();

		foreach (var name in new[]
			{
				LocalStackTemplate.WebRegistryStepName,
				LocalStackTemplate.WebPublishStepName,
				LocalStackTemplate.WebRefreshStepName
			})
		{
			Assert.That(Step(config, name), Is.Null, $"{name} must not be emitted by default");
		}
	}

	[Test]
	public void Web_package_steps_are_build_steps_that_run_to_completion()
	{
		var config = CreateWithWebRegistry();

		foreach (var name in new[] { LocalStackTemplate.WebPublishStepName, LocalStackTemplate.WebRefreshStepName })
		{
			var step = Step(config, name);
			Assert.That(step, Is.Not.Null, $"missing {name}");
			Assert.That(step.build, Is.True, $"{name} must only run under --build");
			Assert.That(step.waitForExit, Is.True, $"{name} must run to completion");
			Assert.That(step.beam, Is.True, $"{name} must invoke the beam CLI");
			// Paths must travel via workingDirectory: the runner splits arguments on whitespace.
			Assert.That(step.arguments, Does.Not.Contain(@"C:\"), $"{name} must keep paths out of its arguments");
		}
	}

	[Test]
	public void Web_package_steps_run_in_the_right_repos()
	{
		var config = CreateWithWebRegistry();

		// Derived from the portal-localdev path rather than a separate option.
		Assert.That(Step(config, LocalStackTemplate.WebPublishStepName).workingDirectory,
			Is.EqualTo(@"C:\repos\BeamableProduct"));
		Assert.That(Step(config, LocalStackTemplate.WebRefreshStepName).workingDirectory,
			Is.EqualTo(@"C:\repos\portal"));
	}

	[Test]
	public void Web_steps_are_ordered_after_scala_and_before_the_extensions_that_consume_them()
	{
		var config = CreateWithWebRegistry();

		// After the Scala services: `beam local up` logs in before its first beam step, and that login
		// authenticates through the Scala auth service.
		Assert.That(IndexOf(config, "scala: auth"),
			Is.LessThan(IndexOf(config, LocalStackTemplate.WebPublishStepName)));

		// Publish, then repoint, then run the extension that was repointed.
		Assert.That(IndexOf(config, LocalStackTemplate.WebPublishStepName),
			Is.LessThan(IndexOf(config, LocalStackTemplate.WebRefreshStepName)));
		Assert.That(IndexOf(config, LocalStackTemplate.WebRefreshStepName),
			Is.LessThan(IndexOf(config, "portal extension: my-ext")));

		// The registry itself comes up first of all.
		Assert.That(IndexOf(config, LocalStackTemplate.WebRegistryStepName), Is.EqualTo(0));
	}

	[Test]
	public void Run_steps_are_not_marked_build()
	{
		var config = CreateWithRepos();

		foreach (var name in new[] { "c# gateway", "portal frontend", "scala: gateway" })
			Assert.That(Step(config, name).build, Is.False, $"{name} must not be a build step");
	}

	/// <summary>
	/// The two backend workers are as load-bearing as the gateway, and their absence is silent: without the
	/// message-rail runtime a send stages and never delivers; without the campaign runtime a campaign
	/// publishes and then never enrolls, advances or sends. The campaign runtime was in fact missing from
	/// this template until it was noticed only by a campaign sitting in Launching forever — hence the guard.
	/// </summary>
	[Test]
	public void Emits_the_backend_worker_runtimes_with_their_own_ports()
	{
		var config = CreateWithRepos();

		foreach (var (buildName, runName, binary) in new[]
		{
			("build: c# message rail runtime", "c# message rail runtime", "BeamableMessageRailRuntime"),
			("build: c# campaign runtime", "c# campaign runtime", "BeamableCampaignRuntime"),
		})
		{
			var build = Step(config, buildName);
			Assert.That(build, Is.Not.Null, $"missing {buildName}");
			Assert.That(build.build, Is.True, $"{buildName} must be a build step");
			Assert.That(build.arguments, Does.Contain(binary));

			var run = Step(config, runName);
			Assert.That(run, Is.Not.Null, $"missing {runName}");
			Assert.That(run.build, Is.False, $"{runName} must not be a build step");
			Assert.That(run.command, Does.Contain(binary));
			Assert.That(run.environment["ASPNETCORE_ENVIRONMENT"], Is.EqualTo("Local"));
			Assert.That(run.readyWhenHttp200, Does.EndWith("/health"));
			Assert.That(IndexOf(config, buildName), Is.LessThan(IndexOf(config, runName)));
		}

		// Three .NET hosts share the machine, so each must bind a port of its own. The gateway takes the
		// ASPNETCORE_URLS default, so it has no entry — stand in a sentinel to keep the comparison honest.
		var ports = new[] { "c# gateway", "c# message rail runtime", "c# campaign runtime" }
			.Select(n => Step(config, n))
			.Select(s => s.environment.TryGetValue("ASPNETCORE_URLS", out var url) ? url : "gateway-default")
			.ToArray();
		Assert.That(ports, Is.Unique);

		// Both workers join the actor cluster, so the docker step bringing up Mongo + ActiveMQ runs first.
		Assert.That(IndexOf(config, "docker: api deps + caddy"),
			Is.LessThan(IndexOf(config, "c# campaign runtime")));
	}
}
