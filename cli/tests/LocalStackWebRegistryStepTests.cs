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
	/// <summary>
	/// A manifest as <c>beam local init</c> writes one: the three web steps are always emitted, and
	/// <paramref name="webRegistry"/> is the standing choice of whether <c>up</c> runs them.
	/// </summary>
	private static LocalStackConfig ConfigWithWebRegistry(
		string portalDir = @"C:\repos\portal", bool webRegistry = true) =>
		LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			apiDir = @"C:\repos\BeamableAPI",
			scalaDir = @"C:\repos\BeamableBackend",
			portalDir = portalDir,
			includeWebRegistry = true,
			webRegistry = webRegistry,
			webRegistryDir = @"C:\repos\BeamableProduct\portal-localdev",
			extensions = new List<string> { "my-ext" },
		});

	private static readonly string[] WebBuildSteps =
	{
		LocalStackTemplate.WebPublishStepName,
		LocalStackTemplate.WebRefreshStepName,
	};

	/// <summary>
	/// Mirrors what <c>beam local up</c> does: resolve the flag against the manifest's standing choice, then
	/// select. <paramref name="webRegistry"/> is the FLAG value, not the resolved one — null means
	/// <c>--with-web-registry</c> was not passed, i.e. a plain <c>up</c>.
	/// </summary>
	private static List<LocalStackStep> Select(
		LocalStackConfig config, bool build = false, bool? webRegistry = null,
		string only = null, string skip = null)
	{
		var autoBuild = build
			? new HashSet<LocalStackStep>()
			: config.steps.Where(s => s.enabled && LocalStackConfigIO.BuildOutputMissing(s, config)).ToHashSet();

		var resolved = LocalStackUpCommand.ResolveNoWebRegistry(config, webRegistry);

		return LocalStackUpCommand.SelectSteps(
			config, build, autoBuild,
			LocalStackUpCommand.ForcedWebSteps(config, resolved),
			LocalStackUpCommand.NameSet(only), LocalStackUpCommand.NameSet(skip), resolved);
	}

	private static void AssertWebStepsRun(List<string> names, string because)
	{
		foreach (var name in WebBuildSteps.Append(LocalStackTemplate.WebRegistryStepName))
		{
			Assert.That(names, Does.Contain(name), $"{name} must run: {because}");
		}
	}

	private static void AssertWebStepsSkipped(List<string> names, string because)
	{
		foreach (var name in WebBuildSteps.Append(LocalStackTemplate.WebRegistryStepName))
		{
			Assert.That(names, Does.Not.Contain(name), $"{name} must be skipped: {because}");
		}
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
	public void Opting_out_skips_every_web_step_including_the_container()
	{
		var names = Names(Select(ConfigWithWebRegistry(), webRegistry: false));

		foreach (var name in WebBuildSteps.Append(LocalStackTemplate.WebRegistryStepName))
		{
			Assert.That(names, Does.Not.Contain(name), $"--with-web-registry=false must skip {name}");
		}

		// It is an opt-out for the web steps only — the rest of the stack is untouched.
		Assert.That(names, Does.Contain("portal frontend"));
		Assert.That(names, Does.Contain("c# gateway"));
	}

	/// <summary>Opting out must win even over an explicit --build.</summary>
	[Test]
	public void Opting_out_beats_build()
	{
		var names = Names(Select(ConfigWithWebRegistry(), build: true, webRegistry: false));

		foreach (var name in WebBuildSteps.Append(LocalStackTemplate.WebRegistryStepName))
		{
			Assert.That(names, Does.Not.Contain(name), $"--with-web-registry=false must skip {name} even under --build");
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

		Assert.That(names, Does.Not.Contain("build: scala"), "a plain up must not trigger the Scala reactor build");

		// `build: portal deps` IS expected here: this config points at a portal directory with no node_modules,
		// and installing them is what lets the Vite step start at all.
		Assert.That(names, Does.Contain("build: portal deps"),
			"a portal with no node_modules must install them, or the frontend cannot start");

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

	// ---------------------------------------------------------------------------------------------------
	// The standing choice `beam local init` records in the manifest, and the two flags that override it for a
	// single run. The point of the choice is that a workspace with the registry off does not need a flag on
	// every `up` — so "manifest says off, no flags" skipping is the behaviour these exist to protect.
	// ---------------------------------------------------------------------------------------------------

	[Test]
	public void Init_choice_off_skips_every_web_step_without_any_flag()
	{
		var names = Names(Select(ConfigWithWebRegistry(webRegistry: false)));

		AssertWebStepsSkipped(names, "the manifest records webRegistry: false");

		// The steps are still IN the manifest — that is what makes the choice reversible without re-running
		// init — so this must come from the choice, not from their absence.
		var config = ConfigWithWebRegistry(webRegistry: false);
		Assert.That(config.steps.Select(x => x.name), Does.Contain(LocalStackTemplate.WebRegistryStepName),
			"init always writes the web steps, so the choice is what decides whether they run");

		// Opting out is web-only; the rest of the stack is untouched.
		Assert.That(names, Does.Contain("portal frontend"));
		Assert.That(names, Does.Contain("c# gateway"));
	}

	[Test]
	public void With_web_registry_overrides_an_off_init_choice_for_one_run()
	{
		var names = Names(Select(ConfigWithWebRegistry(webRegistry: false), webRegistry: true));

		AssertWebStepsRun(names, "--with-web-registry overrides the manifest choice");
	}

	[Test]
	public void The_flag_overrides_an_on_init_choice_for_one_run()
	{
		var names = Names(Select(ConfigWithWebRegistry(webRegistry: true), webRegistry: false));

		AssertWebStepsSkipped(names, "--with-web-registry=false overrides the manifest choice");
	}

	/// <summary>
	/// A manifest written before the choice existed has no <c>webRegistry</c> field. Those must keep running
	/// the web steps: upgrading the CLI must never silently turn the registry off under an existing workspace.
	/// </summary>
	[Test]
	public void A_manifest_with_no_recorded_choice_still_runs_the_web_steps()
	{
		var config = ConfigWithWebRegistry();
		config.webRegistry = null;

		Assert.That(LocalStackUpCommand.ResolveNoWebRegistry(config, null), Is.False);
		AssertWebStepsRun(Names(Select(config)), "an unrecorded choice means on, as it always did");
	}

	/// <summary>Round-trips through the JSON, since the field only helps if it actually persists.</summary>
	[Test]
	public void The_choice_survives_a_save_and_load()
	{
		var path = Path.Combine(Path.GetTempPath(), "beam-web-choice-" + TestContext.CurrentContext.Test.ID + ".json");
		try
		{
			LocalStackConfigIO.Save(path, ConfigWithWebRegistry(webRegistry: false));

			// DefaultValueHandling.Include must keep the `false` in the file — dropped, it would read back as
			// "unrecorded", which means ON, and the choice would silently invert on the next `up`.
			Assert.That(File.ReadAllText(path), Does.Contain("\"webRegistry\""));
			Assert.That(LocalStackConfigIO.Load(path).webRegistry, Is.False);

			LocalStackConfigIO.Save(path, ConfigWithWebRegistry(webRegistry: true));
			Assert.That(LocalStackConfigIO.Load(path).webRegistry, Is.True);
		}
		finally
		{
			File.Delete(path);
		}
	}

	/// <summary>The three states the single override field has to represent, at the resolver.</summary>
	[Test]
	public void The_run_override_has_three_states()
	{
		var off = ConfigWithWebRegistry(webRegistry: false);
		var on = ConfigWithWebRegistry();

		// null defers to the manifest — the state a plain bool could not represent.
		Assert.That(LocalStackUpCommand.ResolveNoWebRegistry(off, null), Is.True);
		Assert.That(LocalStackUpCommand.ResolveNoWebRegistry(on, null), Is.False);

		// ...and an explicit flag overrides it in either direction.
		Assert.That(LocalStackUpCommand.ResolveNoWebRegistry(off, true), Is.False);
		Assert.That(LocalStackUpCommand.ResolveNoWebRegistry(on, false), Is.True);
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
