using System.Collections.Generic;
using System.IO;
using System.Linq;
using cli;
using cli.Commands.LocalStack;
using cli.Services.LocalStack;
using cli.Services.Web;
using NUnit.Framework;

namespace tests;

/// <summary>
/// The web-registry steps are build steps that deliberately declare no <c>requiredOutput</c> (see
/// <see cref="LocalStackBuildStepTests.Slow_build_steps_declare_no_output_so_they_stay_build_only"/>), so a
/// plain <c>beam local up</c> skipped them while still reporting success — leaving the portal's extensions
/// pinned at a published toolkit, which makes the portal resolve the sdk from unpkg instead of the local
/// build and any local-only API fail at runtime as "not a function". These cover the two ways to opt them
/// in (<c>--with-web-registry</c>, or naming them in <c>--only</c>).
/// </summary>
public class LocalStackWebRegistryStepTests
{
	private static LocalStackConfig ConfigWithWebRegistry(string portalDir = @"C:\repos\portal") =>
		LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			apiDir = @"C:\repos\BeamableAPI",
			scalaDir = @"C:\repos\BeamableBackend",
			portalDir = portalDir,
			includeWebRegistry = true,
			webRegistryDir = @"C:\repos\BeamableProduct\portal-localdev",
			extensions = new List<string> { "my-ext" },
		});

	private static readonly string[] WebBuildSteps =
	{
		LocalStackTemplate.WebPublishStepName,
		LocalStackTemplate.WebRefreshStepName,
	};

	private static List<LocalStackStep> Select(
		LocalStackConfig config, bool build = false, bool noWebRegistry = false,
		string only = null, string skip = null)
	{
		var autoBuild = build
			? new HashSet<LocalStackStep>()
			: config.steps.Where(s => s.enabled && LocalStackConfigIO.BuildOutputMissing(s, config)).ToHashSet();

		return LocalStackUpCommand.SelectSteps(
			config, build, autoBuild,
			LocalStackUpCommand.ForcedWebSteps(config, noWebRegistry),
			LocalStackUpCommand.NameSet(only), LocalStackUpCommand.NameSet(skip), noWebRegistry);
	}

	private static List<string> Names(List<LocalStackStep> steps) => steps.Select(s => s.name).ToList();

	/// <summary>
	/// The regression this all exists for: a plain `up` used to silently skip them, leaving the portal on a
	/// published toolkit pin while reporting success. They now run by default.
	/// </summary>
	[Test]
	public void Plain_up_runs_every_web_step()
	{
		var names = Names(Select(ConfigWithWebRegistry()));

		foreach (var name in WebBuildSteps.Append(LocalStackTemplate.WebRegistryStepName))
		{
			Assert.That(names, Does.Contain(name), $"{name} must run on a plain up");
		}
	}

	/// <summary>
	/// The opt-out has to cover all three. `docker: web registry` is not a build step, so a gate that only
	/// un-forced the build steps would leave the container coming up and honour the flag by halves.
	/// </summary>
	[Test]
	public void No_web_registry_skips_every_web_step_including_the_container()
	{
		var names = Names(Select(ConfigWithWebRegistry(), noWebRegistry: true));

		foreach (var name in WebBuildSteps.Append(LocalStackTemplate.WebRegistryStepName))
		{
			Assert.That(names, Does.Not.Contain(name), $"--no-web-registry must skip {name}");
		}

		// It is an opt-out for the web steps only — the rest of the stack is untouched.
		Assert.That(names, Does.Contain("portal frontend"));
		Assert.That(names, Does.Contain("c# gateway"));
	}

	/// <summary>--no-web-registry must win even over an explicit --build.</summary>
	[Test]
	public void No_web_registry_beats_build()
	{
		var names = Names(Select(ConfigWithWebRegistry(), build: true, noWebRegistry: true));

		foreach (var name in WebBuildSteps.Append(LocalStackTemplate.WebRegistryStepName))
		{
			Assert.That(names, Does.Not.Contain(name), $"--no-web-registry must skip {name} even under --build");
		}

		Assert.That(names, Does.Contain("build: scala"), "--build must still run the non-web build steps");
	}

	/// <summary>
	/// Running the web steps by default must not drag in the slow Scala/gateway builds — that is what --build
	/// is for.
	/// </summary>
	[Test]
	public void Default_web_steps_do_not_run_unrelated_build_steps()
	{
		var config = ConfigWithWebRegistry();
		var names = Names(Select(config));

		foreach (var name in new[] { "build: scala", "build: portal deps" })
		{
			Assert.That(names, Does.Not.Contain(name), $"a plain up must not trigger {name}");
		}

		// ...while --build still runs everything.
		Assert.That(Names(Select(config, build: true)), Does.Contain("build: scala"));
	}

	[Test]
	public void Web_steps_respect_skip()
	{
		var names = Names(Select(ConfigWithWebRegistry(), skip: LocalStackTemplate.WebRefreshStepName));

		Assert.That(names, Does.Contain(LocalStackTemplate.WebPublishStepName));
		Assert.That(names, Does.Not.Contain(LocalStackTemplate.WebRefreshStepName));
	}

	/// <summary>
	/// Naming a build step in --only used to select nothing at all and report "No steps to run", because the
	/// --build gate still excluded it. Naming a step IS the explicit request.
	/// </summary>
	[Test]
	public void Only_can_name_a_build_step_without_build()
	{
		var steps = Select(ConfigWithWebRegistry(), only: LocalStackTemplate.WebRefreshStepName);

		Assert.That(Names(steps), Is.EqualTo(new[] { LocalStackTemplate.WebRefreshStepName }));
	}

	[Test]
	public void Only_still_excludes_steps_it_does_not_name()
	{
		var steps = Select(ConfigWithWebRegistry(), only: "c# gateway");

		Assert.That(Names(steps), Is.EqualTo(new[] { "c# gateway" }));
	}

	/// <summary>Disabled steps stay out no matter how they are opted in.</summary>
	[Test]
	public void Disabled_web_steps_are_never_selected()
	{
		var config = ConfigWithWebRegistry();
		foreach (var step in config.steps.Where(s => LocalStackTemplate.IsWebStep(s.name)))
		{
			step.enabled = false;
		}

		var names = Names(Select(config, only: LocalStackTemplate.WebRefreshStepName));
		Assert.That(names, Is.Empty);
	}

	[Test]
	public void IsWebStep_matches_exactly_the_three_web_steps()
	{
		Assert.That(LocalStackTemplate.IsWebStep(LocalStackTemplate.WebRegistryStepName), Is.True);
		Assert.That(LocalStackTemplate.IsWebStep(LocalStackTemplate.WebPublishStepName), Is.True);
		Assert.That(LocalStackTemplate.IsWebStep(LocalStackTemplate.WebRefreshStepName), Is.True);

		Assert.That(LocalStackTemplate.IsWebStep("build: scala"), Is.False);
		Assert.That(LocalStackTemplate.IsWebStep("portal frontend"), Is.False);
		Assert.That(LocalStackTemplate.IsWebStep(null), Is.False);
	}

	/// <summary>
	/// The scaffolding templates carry the same portalExtension markers as a real extension, so a scan rooted
	/// at the product repo used to repin them to the local dev version — and every extension created from them
	/// afterwards would inherit that pin.
	/// </summary>
	[Test]
	public void Extension_scan_skips_the_dotnet_new_templates()
	{
		var root = Path.Combine(Path.GetTempPath(), "beam-tmpl-scan-" + TestContext.CurrentContext.Test.ID);
		var real = Path.Combine(root, "bundles", "my-ext");
		var template = Path.Combine(root, "cli", "beamable.templates", "templates", "PortalExtensionReactApp");
		Directory.CreateDirectory(real);
		Directory.CreateDirectory(template);

		const string manifest = """
			{
			  "name": "%NAME%",
			  "beamable": { "portalExtension": true },
			  "devDependencies": { "@beamable/portal-toolkit": "0.4.0" }
			}
			""";
		try
		{
			File.WriteAllText(Path.Combine(real, "package.json"), manifest.Replace("%NAME%", "my-ext"));
			File.WriteAllText(Path.Combine(template, "package.json"), manifest.Replace("%NAME%", "the-template"));

			var found = WebLocalRegistryService.FindExtensionProjects(root);

			Assert.That(found.Select(f => f.name), Is.EqualTo(new[] { "my-ext" }),
				"the scaffolding template must never be treated as an extension to repin");
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	/// <summary>A published pin is stale; only the local dev version (bare or ranged) counts as the local build.</summary>
	[Test]
	public void Published_pins_are_recognised_as_not_the_local_build()
	{
		Assert.That(WebLocalRegistryService.IsLocalDevVersion("0.4.0"), Is.False);
		Assert.That(WebLocalRegistryService.IsLocalDevVersion("1.3.0-rc.1"), Is.False);
		Assert.That(WebLocalRegistryService.IsLocalDevVersion(null), Is.False);

		Assert.That(WebLocalRegistryService.IsLocalDevVersion(WebLocalRegistryService.LocalDevVersion), Is.True);
		Assert.That(WebLocalRegistryService.IsLocalDevVersion("^" + WebLocalRegistryService.LocalDevVersion), Is.True);
	}
}
