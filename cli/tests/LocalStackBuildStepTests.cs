using System;
using System.IO;
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

	/// <summary>
	/// Writes a throwaway Scala repo whose root pom registers <paramref name="rootModules"/> and whose
	/// tools aggregator registers <paramref name="toolModules"/>, so the reactor-module readers have real
	/// poms to parse. Only the two aggregator poms matter — nothing is built.
	/// </summary>
	private static string WriteFakeScalaRepo(string[] rootModules, string[] toolModules)
	{
		var dir = Path.Combine(Path.GetTempPath(), "beam-scala-repo-" + Guid.NewGuid().ToString("N")[..8]);
		Directory.CreateDirectory(Path.Combine(dir, "tools"));

		string Pom(string[] modules) =>
			"<project><modules>"
			+ string.Join("", modules.Select(m => $"\n        <module>{m}</module>"))
			+ "\n    </modules></project>\n";

		if (rootModules != null)
		{
			File.WriteAllText(Path.Combine(dir, "pom.xml"), Pom(rootModules));
		}

		File.WriteAllText(Path.Combine(dir, "tools", "pom.xml"), Pom(toolModules));
		return dir;
	}

	private static LocalStackConfig CreateWithScalaRepo(string scalaDir) =>
		LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			apiDir = @"C:\repos\BeamableAPI",
			scalaDir = scalaDir,
			portalDir = @"C:\repos\portal",
			scalaTools = new System.Collections.Generic.List<LocalStackTemplate.ScalaToolInfo>
			{
				new() { name = "gateway", mainClass = "com.beamable.gateway.App" },
				new() { name = "auth", mainClass = "com.beamable.auth.App" },
			},
		});

	/// <summary>
	/// `core` must be built explicitly rather than left to `-am`: every Scala launch script prepends
	/// `core/target/classes` to the classpath (it holds rendered config resources the ~/.m2 jar lacks), but
	/// `-am` only pulls core into the reactor when a selected tool declares a direct dependency on it.
	/// </summary>
	[Test]
	public void Scala_build_includes_core_when_the_root_pom_registers_it()
	{
		var repo = WriteFakeScalaRepo(new[] { "core", "tools", "rest" }, new[] { "gateway", "auth" });
		try
		{
			var step = Step(CreateWithScalaRepo(repo), "build: scala");

			Assert.That(step.arguments, Does.Contain("-pl core,"));
			Assert.That(step.arguments, Does.Contain("tools/gateway"));
			Assert.That(step.arguments, Does.Contain("tools/auth"));
			Assert.That(step.arguments, Does.Contain("-am"));
			Assert.That(step.arguments, Does.Contain("clean"));
			// One `core` entry only — a repeated -pl module is at best noise and at worst a broken selector.
			Assert.That(step.arguments.Split(',').Count(m => m.Trim().EndsWith("core")), Is.EqualTo(1));
			// A cold clean build of core + the tools can outrun the old 900s.
			Assert.That(step.readyTimeoutSeconds, Is.EqualTo(1800));
		}
		finally
		{
			Directory.Delete(repo, recursive: true);
		}
	}

	/// <summary>
	/// The safe fallback: an unregistered `-pl` entry fails the ENTIRE reactor build and rolls the stack back,
	/// so a root pom we cannot read must leave `core` out rather than guess it in.
	/// </summary>
	[Test]
	public void Scala_build_omits_core_when_the_root_pom_does_not_register_it()
	{
		var repo = WriteFakeScalaRepo(rootModules: null, toolModules: new[] { "gateway", "auth" });
		try
		{
			var step = Step(CreateWithScalaRepo(repo), "build: scala");

			Assert.That(step.arguments, Does.Not.Contain("-pl core"));
			Assert.That(step.arguments, Does.Contain("tools/gateway"));
		}
		finally
		{
			Directory.Delete(repo, recursive: true);
		}
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
			// Still flagged `build` in the manifest; `up` opts them in by default (see
			// LocalStackWebRegistryStepTests), and --no-web-registry opts back out.
			Assert.That(step.build, Is.True, $"{name} must be a build step");
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

	/// <summary>The three .NET hosts, as (build step, run step, project) — the set that must stay in lockstep.</summary>
	private static readonly (string build, string run, string project)[] DotnetHosts =
	{
		("build: c# gateway", "c# gateway", "BeamableGateway"),
		("build: c# message rail runtime", "c# message rail runtime", "BeamableMessageRailRuntime"),
		("build: c# campaign runtime", "c# campaign runtime", "BeamableCampaignRuntime"),
	};

	/// <summary>
	/// Each .NET host's build step must declare the binary it produces, because that is what lets `beam local
	/// up` build it when it is missing instead of launching a nonexistent executable, retrying it, and then
	/// reporting the stack as up minus that service.
	/// </summary>
	[Test]
	public void Dotnet_host_build_steps_declare_the_binary_they_produce()
	{
		var config = CreateWithRepos();

		foreach (var (buildName, _, project) in DotnetHosts)
		{
			var build = Step(config, buildName);
			Assert.That(build.requiredOutput, Is.Not.Null.And.Not.Empty, $"{buildName} must declare its output");
			Assert.That(build.requiredOutput, Does.Contain(Path.Combine("bin", "Debug", "net10.0")),
				$"{buildName} output must be the Debug build output folder");
			Assert.That(Path.GetFileName(build.requiredOutput),
				Is.EqualTo(OperatingSystem.IsWindows() ? project + ".exe" : project),
				$"{buildName} output must be the apphost `dotnet build` actually produces on this OS");
		}
	}

	/// <summary>
	/// The anti-drift guard: the declared output and the binary the run step launches are the same file. They
	/// come from one helper for exactly this reason — a build step that "succeeds" while pointing somewhere
	/// else would let `up` skip the build and then fail to launch.
	/// </summary>
	[Test]
	public void Declared_build_output_is_the_binary_the_run_step_launches()
	{
		var config = CreateWithRepos();

		foreach (var (buildName, runName, _) in DotnetHosts)
		{
			var run = Step(config, runName);
			var launched = Path.Combine(run.workingDirectory, run.command.TrimStart('.', '/', '\\'));
			Assert.That(Step(config, buildName).requiredOutput, Is.EqualTo(launched),
				$"{buildName} must declare exactly the binary '{runName}' launches");
		}
	}

	/// <summary>
	/// Steps whose build is slow and whose absence fails loudly on its own declare no output, so the
	/// self-healing "its output is missing, build it anyway" path never reaches them — a surprise multi-minute
	/// `mvn clean package` on a plain `up` is worse than an error. The two web steps also declare no output,
	/// which is why `up` needs its own default to run them (see LocalStackWebRegistryStepTests).
	/// </summary>
	[Test]
	public void Slow_build_steps_declare_no_output_so_they_stay_build_only()
	{
		var config = CreateWithWebRegistry();

		foreach (var name in new[]
			{
				"build: scala", "build: portal deps",
				LocalStackTemplate.WebPublishStepName, LocalStackTemplate.WebRefreshStepName
			})
		{
			Assert.That(Step(config, name).requiredOutput, Is.Null.Or.Empty,
				$"{name} must not opt into building without --build");
		}
	}

	private static LocalStackConfig EmptyConfig() => new LocalStackConfig();

	[Test]
	public void Build_output_missing_is_true_only_for_a_build_step_with_an_absent_declared_output()
	{
		var dir = TestContext.CurrentContext.TestDirectory;
		var present = Path.Combine(dir, "beam-required-output-probe.txt");
		File.WriteAllText(present, "built");

		var absent = new LocalStackStep { build = true, requiredOutput = Path.Combine(dir, "does-not-exist.bin") };
		Assert.That(LocalStackConfigIO.BuildOutputMissing(absent, EmptyConfig()), Is.True);

		var built = new LocalStackStep { build = true, requiredOutput = present };
		Assert.That(LocalStackConfigIO.BuildOutputMissing(built, EmptyConfig()), Is.False);

		// A directory counts as produced too (e.g. node_modules), not just a file.
		var builtDir = new LocalStackStep { build = true, requiredOutput = dir };
		Assert.That(LocalStackConfigIO.BuildOutputMissing(builtDir, EmptyConfig()), Is.False);

		// No declaration = today's behavior: --build only.
		Assert.That(LocalStackConfigIO.BuildOutputMissing(new LocalStackStep { build = true }, EmptyConfig()), Is.False);

		// Never applies to a run step, which is not gated on --build in the first place.
		var runStep = new LocalStackStep { build = false, requiredOutput = Path.Combine(dir, "does-not-exist.bin") };
		Assert.That(LocalStackConfigIO.BuildOutputMissing(runStep, EmptyConfig()), Is.False);
	}

	[Test]
	public void Relative_build_output_resolves_against_the_working_directory()
	{
		var dir = TestContext.CurrentContext.TestDirectory;
		var name = "beam-relative-output-probe.txt";
		File.WriteAllText(Path.Combine(dir, name), "built");

		var step = new LocalStackStep { build = true, workingDirectory = dir, requiredOutput = name };
		Assert.That(LocalStackConfigIO.ResolveRequiredOutput(step, EmptyConfig()),
			Is.EqualTo(Path.Combine(dir, name)));
		Assert.That(LocalStackConfigIO.BuildOutputMissing(step, EmptyConfig()), Is.False);
	}

	/// <summary>
	/// An unedited `&lt;EDIT: absolute path to ...&gt;` placeholder must not be read as "the output is missing,
	/// go build it" — the manifest is simply not filled in yet.
	/// </summary>
	[Test]
	public void Placeholder_paths_do_not_trigger_a_build()
	{
		var config = LocalStackTemplate.Create(new LocalStackTemplate.Options());

		foreach (var (buildName, _, _) in DotnetHosts)
		{
			var step = Step(config, buildName);
			Assert.That(step.requiredOutput, Does.Contain("<EDIT:"),
				"an unset api dir must still produce the placeholder");
			Assert.That(LocalStackConfigIO.BuildOutputMissing(step, config), Is.False,
				$"{buildName} must not auto-build off a placeholder path");
		}
	}
}
