using System;
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
/// <see cref="LocalStackBuildStepTests.Slow_build_steps_declare_no_output_so_they_stay_build_only"/>), so the
/// build gate alone would never run them — and a stack that brings the registry up without them leaves the
/// portal's extensions pinned at a published toolkit, which makes the portal resolve the sdk from unpkg instead
/// of the local build and any local-only API fail at runtime as "not a function". The stack name decides: the
/// <c>:web</c> variant (or a stack that includes the <c>web-registry</c> tag) runs all three, and nothing else
/// runs any of them.
/// </summary>
public class LocalStackWebRegistryStepTests
{
	/// <summary>A catalogue as <c>beam local setup</c> writes one: the three web steps are always in it.</summary>
	private static LocalStackConfig Catalogue(string portalDir = @"C:\repos\portal") =>
		LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			apiDir = @"C:\repos\BeamableAPI",
			scalaDir = @"C:\repos\BeamableBackend",
			portalDir = portalDir,
			includeWebRegistry = true,
			webRegistryDir = @"C:\repos\BeamableProduct\portal-localdev",
			extensions = new List<string> { "my-ext" },
		});

	private static readonly string[] WebSteps =
	{
		LocalStackTemplate.WebRegistryStepName,
		LocalStackTemplate.WebBuildStepName,
		LocalStackTemplate.WebPublishStepName,
		LocalStackTemplate.WebInstallStepName,
		LocalStackTemplate.WebWatchStepName,
	};

	/// <summary>Mirrors what <c>beam local run &lt;name&gt;</c> does: resolve the name, then apply the build gate.</summary>
	private static List<string> Run(LocalStackConfig config, string name)
	{
		var resolved = LocalStackStacks.BuiltIn().Resolve(name, config);
		var autoBuild = resolved.forceBuild
			? new HashSet<LocalStackStep>()
			: resolved.steps.Where(s => s.enabled && LocalStackConfigIO.BuildOutputMissing(s, config)).ToHashSet();

		return LocalStackRunCommand.SelectSteps(resolved, autoBuild).Select(s => s.name).ToList();
	}

	[Test]
	public void A_stack_without_web_runs_no_web_step()
	{
		var names = Run(Catalogue(), "full");

		foreach (var name in WebSteps)
		{
			Assert.That(names, Does.Not.Contain(name), $"{name} must only run when the name asks for it");
		}

		Assert.That(names, Does.Contain("portal frontend"));
		Assert.That(names, Does.Contain("c# gateway"));
	}

	/// <summary>
	/// The regression this all exists for: a run that brought the registry up skipped the two build steps while
	/// reporting success, leaving the portal on a published toolkit pin. `:web` runs all three, and first.
	/// </summary>
	[Test]
	public void The_web_variant_runs_every_web_step_first_without_build()
	{
		var names = Run(Catalogue(), "full:web");

		Assert.That(names.Take(4), Is.EqualTo(WebSteps.Take(4)), "`:web` adds them before the stack's own steps");
		Assert.That(names, Does.Contain("portal frontend"));
	}

	/// <summary>Running the web steps must not drag in the slow Scala/gateway builds — that is what `:build` is for.</summary>
	[Test]
	public void The_web_variant_does_not_run_unrelated_build_steps()
	{
		var config = Catalogue();
		var names = Run(config, "full:web");

		Assert.That(names, Does.Not.Contain("build: scala"), "`:web` must not trigger the Scala reactor build");

		// `build: portal deps` IS expected here: this config points at a portal directory with no node_modules,
		// and installing them is what lets the Vite step start at all.
		Assert.That(names, Does.Contain("build: portal deps"),
			"a portal with no node_modules must install them, or the frontend cannot start");

		// ...while `:build` runs everything.
		Assert.That(Run(config, "full:web:build"), Does.Contain("build: scala"));
	}

	[Test]
	public void The_build_variant_runs_every_build_step_but_no_web_step()
	{
		var names = Run(Catalogue(), "full:build");

		Assert.That(names, Does.Contain("build: scala").And.Contain("build: c# gateway"));
		foreach (var name in WebSteps)
		{
			Assert.That(names, Does.Not.Contain(name), $"`:build` must not bring {name} in");
		}
	}

	[Test]
	public void No_build_skips_builds_even_when_an_output_is_missing()
	{
		var names = Run(Catalogue(), "full:no-build");

		Assert.That(names, Does.Not.Contain("build: portal deps"));
		Assert.That(names, Does.Contain("portal frontend"));
	}

	/// <summary>`:no-build` removes the stack's own builds; the web steps `:web` adds are asked for outright.</summary>
	[Test]
	public void No_build_keeps_what_web_adds()
	{
		var names = Run(Catalogue(), "full:web:no-build");

		Assert.That(names.Take(4), Is.EqualTo(WebSteps.Take(4)));
		Assert.That(names, Does.Contain(LocalStackTemplate.WebWatchStepName));
		Assert.That(names, Does.Not.Contain("build: portal deps"));
	}

	[Test]
	public void Web_dev_runs_the_web_steps_through_its_own_selection()
	{
		var names = Run(Catalogue(), "web-dev");

		foreach (var name in WebSteps)
		{
			Assert.That(names, Does.Contain(name));
		}

		Assert.That(names, Does.Not.Contain("c# gateway"));
	}

	[Test]
	public void Web_dev_non_publish_builds_but_never_publishes_or_installs()
	{
		var names = Run(Catalogue(), "web-dev:non-publish");

		Assert.That(names, Does.Contain(LocalStackTemplate.WebRegistryStepName).And.Contain(LocalStackTemplate.WebBuildStepName));
		Assert.That(names, Does.Not.Contain(LocalStackTemplate.WebPublishStepName));
		Assert.That(names, Does.Not.Contain(LocalStackTemplate.WebInstallStepName));
		Assert.That(names, Does.Not.Contain(LocalStackTemplate.WebWatchStepName));
	}

	/// <summary>:web builds, publishes and points the extensions at the build before the stack, and watches after it.</summary>
	[Test]
	public void The_web_variant_runs_the_package_loop_around_the_stack()
	{
		var names = Run(Catalogue(), "full:web");
		var web = new[] { LocalStackTemplate.WebRegistryStepName, LocalStackTemplate.WebBuildStepName, LocalStackTemplate.WebPublishStepName, LocalStackTemplate.WebInstallStepName };

		Assert.That(names.Take(4), Is.EqualTo(web));
		Assert.That(names.IndexOf(LocalStackTemplate.WebWatchStepName), Is.GreaterThan(names.IndexOf("portal extension: my-ext")));
	}

	/// <summary>Disabled steps stay out no matter how they are asked for.</summary>
	[Test]
	public void Disabled_web_steps_are_never_selected()
	{
		var config = Catalogue();
		foreach (var step in config.steps.Where(s => LocalStackTemplate.IsWebStep(s.name)))
		{
			step.enabled = false;
		}

		var names = Run(config, "full:web");
		foreach (var name in WebSteps)
		{
			Assert.That(names, Does.Not.Contain(name));
		}
	}

	[Test]
	public void IsWebStep_matches_the_web_steps()
	{
		foreach (var name in WebSteps)
			Assert.That(LocalStackTemplate.IsWebStep(name), Is.True, name);

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

	/// <summary>
	/// A local build is the next minor after the highest version on npmjs, then -0. and the time: newer than every
	/// release so far, older than the next one's prereleases, and never the same twice.
	/// </summary>
	[Test]
	public void Local_builds_get_a_version_of_their_own()
	{
		Assert.That(WebPackagesService.NextBase(new[] { "0.4.0", "0.5.0-rc.8", "0.4.2", "not-a-version" }), Is.EqualTo("0.6.0"));
		Assert.That(WebPackagesService.NextBase(new[] { "1.3.0", "1.2.9" }), Is.EqualTo("1.4.0"));
		Assert.That(WebPackagesService.NextBase(new string[0]), Is.Null);

		var version = WebPackagesService.LocalVersion("0.6.0", new DateTime(2026, 10, 23, 9, 5, 7, DateTimeKind.Utc));
		Assert.That(version, Is.EqualTo("0.6.0-0.20261023090507"));
		Assert.That(cli.Utils.NpmSemver.TrySatisfies(version, ">0.5.0-rc.8 <0.6.0-dev.0", out var between) && between, Is.True,
			"above the last release candidate, below the next minor's dev builds");
	}

	/// <summary>The overlay goes in with --no-save, from the local registry for the @beamable scope only.</summary>
	[Test]
	public void The_overlay_installs_without_saving_from_the_local_registry()
	{
		var overlay = new WebPackagesOverlay
		{
			registry = WebLocalRegistryService.DefaultRegistry,
			packages = { [WebLocalRegistryService.ToolkitPackage] = "0.6.0-0.20261023090507", [WebLocalRegistryService.SdkPackage] = "1.4.0-0.20261023090507" }
		};
		var args = WebPackagesService.OverlayInstallArgs(overlay);

		Assert.That(args, Does.StartWith("install --no-save "));
		Assert.That(args, Does.Contain("@beamable/portal-toolkit@0.6.0-0.20261023090507").And.Contain("@beamable/sdk@1.4.0-0.20261023090507"));
		Assert.That(args, Does.Contain("--@beamable:registry=http://localhost:4873").And.Not.Contain(" --registry"));
	}

	/// <summary>A toolkit build with the portal's components unchanged reuses the saved manifest: generating it writes
	/// files the portal's dev server watches, which reloads every open portal tab.</summary>
	[Test]
	public void An_unchanged_component_manifest_is_not_generated_again()
	{
		var workspace = Path.Combine(Path.GetTempPath(), "web-manifest-tests", Path.GetRandomFileName());
		var saved = Path.Combine(workspace, ".beamable", "local", "custom-elements.json");
		Directory.CreateDirectory(Path.GetDirectoryName(saved)!);
		File.WriteAllText(saved, "{\"saved\":true}");
		try
		{
			// No portal here: generating would run `npm run generate:cem` and fail, so only the saved one can come back.
			var manifest = WebPackagesService.ComponentManifestFor(workspace, new WebPackagesState { components = "print" }, "print", force: false);
			Assert.That(System.Text.Encoding.UTF8.GetString(manifest), Is.EqualTo("{\"saved\":true}"));

			Assert.Throws<CliException>(() => WebPackagesService.ComponentManifestFor(workspace, new WebPackagesState { components = "print" }, "changed", force: false),
				"changed components generate again");
		}
		finally
		{
			Directory.Delete(workspace, true);
		}
	}

	/// <summary>A version the registry has already is published, not a failure, even when the check before missed it.</summary>
	[Test]
	public void A_conflict_on_publish_means_already_published()
	{
		Assert.That(WebPackagesService.AlreadyPublished("npm error code E409\r\nnpm error 409 Conflict - PUT http://localhost:4873/@beamable%2fsdk - this package is already present"), Is.True);
		Assert.That(WebPackagesService.AlreadyPublished("npm error code E401\r\nnpm error 401 Unauthorized"), Is.False);
	}
}
