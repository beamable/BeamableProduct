using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using cli;
using cli.Commands.LocalStack;
using cli.Services.LocalStack;
using cli.Services.Web;
using NUnit.Framework;

namespace tests;

/// <summary>
/// Covers the two "it looked like nothing was there" failures:
/// portal extensions that <c>init</c> could not see, and Scala services that <c>up</c> launched unbuilt.
/// </summary>
public class LocalStackDiscoveryTests
{
	// ----------------------------------------------------------------------------------
	// Portal extension discovery
	// ----------------------------------------------------------------------------------

	private static void WritePackage(string dir, string name, bool isExtension, params string[] groups)
	{
		Directory.CreateDirectory(dir);
		var serviceGroups = groups.Length == 0
			? string.Empty
			: ", \"serviceGroups\": [" + string.Join(",", groups.Select(g => $"\"{g}\"")) + "]";

		var beamable = isExtension
			? $"\"beamable\": {{ \"portalExtension\": true{serviceGroups} }},"
			: "\"beamable\": { \"portalExtension\": false },";

		File.WriteAllText(Path.Combine(dir, "package.json"), $"{{ {beamable} \"name\": \"{name}\" }}");
	}

	[Test]
	public void FindsExtensionsByTheirBeamableMarker()
	{
		// The beamo manifest only covers the workspace a command RUNS in, so running `init` anywhere but the
		// portal repo discovered nothing and the picker came up empty. Scanning the checkout that --portal-dir
		// names is what makes the extensions visible from any directory.
		var root = Directory.CreateTempSubdirectory("beam-ext-discovery");
		try
		{
			WritePackage(Path.Combine(root.FullName, "bundles", "extensions", "a"), "ext-a", true, "player-engagement");
			WritePackage(Path.Combine(root.FullName, "bundles", "extensions", "b"), "ext-b", true, "player-engagement", "vip");
			WritePackage(Path.Combine(root.FullName, "bundles", "libs", "shared"), "portal-ui", false);
			// A dependency copy must never be mistaken for a real extension.
			WritePackage(Path.Combine(root.FullName, "node_modules", "evil"), "ext-from-node-modules", true);

			var (extensions, groups) = LocalStackTemplate.DiscoverPortalExtensions(root.FullName);

			Assert.That(extensions, Is.EqualTo(new List<string> { "ext-a", "ext-b" }));
			Assert.That(extensions, Does.Not.Contain("portal-ui"), "a non-extension package must be ignored");
			Assert.That(extensions, Does.Not.Contain("ext-from-node-modules"), "node_modules must be skipped");

			Assert.That(groups["player-engagement"], Is.EquivalentTo(new[] { "ext-a", "ext-b" }));
			Assert.That(groups["vip"], Is.EquivalentTo(new[] { "ext-b" }));
		}
		finally
		{
			root.Delete(recursive: true);
		}
	}

	[Test]
	public void DiscoveryIsEmptyForAnUnusablePortalPath()
	{
		// A missing or unedited path is "nothing to report", never an error — init still has to write a manifest.
		Assert.That(LocalStackTemplate.DiscoverPortalExtensions(null).extensions, Is.Empty);
		Assert.That(LocalStackTemplate.DiscoverPortalExtensions(
			$"{LocalStackConfigIO.EditPlaceholder} absolute path to portal>").extensions, Is.Empty);
		Assert.That(LocalStackTemplate.DiscoverPortalExtensions(
			Path.Combine(Path.GetTempPath(), "no-such-portal-checkout")).extensions, Is.Empty);
	}

	[Test]
	public void AMalformedPackageJsonDoesNotAbortTheScan()
	{
		var root = Directory.CreateTempSubdirectory("beam-ext-malformed");
		try
		{
			Directory.CreateDirectory(Path.Combine(root.FullName, "broken"));
			File.WriteAllText(Path.Combine(root.FullName, "broken", "package.json"), "{ not json");
			WritePackage(Path.Combine(root.FullName, "good"), "ext-good", true);

			Assert.That(LocalStackTemplate.DiscoverPortalExtensions(root.FullName).extensions,
				Is.EqualTo(new List<string> { "ext-good" }));
		}
		finally
		{
			root.Delete(recursive: true);
		}
	}

	// ----------------------------------------------------------------------------------
	// "Has this Scala module ever been built?"
	// ----------------------------------------------------------------------------------

	private static LocalStackStep ScalaBuildStep(string scalaDir, params string[] modules) => new LocalStackStep
	{
		name = "build: scala",
		build = true,
		workingDirectory = scalaDir,
		scalaModules = modules.ToList()
	};

	[Test]
	public void AModuleWithNoTargetCountsAsUnbuilt()
	{
		var root = Directory.CreateTempSubdirectory("beam-scala-built");
		try
		{
			Directory.CreateDirectory(Path.Combine(root.FullName, "tools", "auth"));
			var step = ScalaBuildStep(root.FullName, "tools/auth");

			Assert.That(LocalStackConfigIO.FirstUnbuiltScalaModule(step, new LocalStackConfig()), Is.EqualTo("tools/auth"));
			// This is what makes a plain `beam local up` build itself instead of launching an empty service.
			Assert.That(LocalStackConfigIO.BuildOutputMissing(step, new LocalStackConfig()), Is.True);
		}
		finally
		{
			root.Delete(recursive: true);
		}
	}

	[Test]
	public void AnEmptyTargetClassesDirectoryIsNotBuilt()
	{
		// Maven creates target/classes even when it produced nothing, so testing for the directory would report a
		// failed build as a successful one — which is exactly how an unbuilt service got launched.
		var root = Directory.CreateTempSubdirectory("beam-scala-empty");
		try
		{
			Directory.CreateDirectory(Path.Combine(root.FullName, "tools", "auth", "target", "classes"));
			var step = ScalaBuildStep(root.FullName, "tools/auth");

			Assert.That(LocalStackConfigIO.FirstUnbuiltScalaModule(step, new LocalStackConfig()), Is.EqualTo("tools/auth"));
		}
		finally
		{
			root.Delete(recursive: true);
		}
	}

	[Test]
	public void CompiledClassesOrAJarCountAsBuilt()
	{
		var root = Directory.CreateTempSubdirectory("beam-scala-ok");
		try
		{
			var classes = Path.Combine(root.FullName, "tools", "auth", "target", "classes", "com");
			Directory.CreateDirectory(classes);
			File.WriteAllText(Path.Combine(classes, "App.class"), "x");

			var jarModule = Path.Combine(root.FullName, "tools", "stats", "target");
			Directory.CreateDirectory(jarModule);
			File.WriteAllText(Path.Combine(jarModule, "stats-1.0-SNAPSHOT.jar"), "x");

			var step = ScalaBuildStep(root.FullName, "tools/auth", "tools/stats");

			Assert.That(LocalStackConfigIO.FirstUnbuiltScalaModule(step, new LocalStackConfig()), Is.Null);
			Assert.That(LocalStackConfigIO.BuildOutputMissing(step, new LocalStackConfig()), Is.False,
				"a stack that is already built must not rebuild on every `up`");
		}
		finally
		{
			root.Delete(recursive: true);
		}
	}

	[Test]
	public void OneUnbuiltModuleIsEnoughToTriggerTheReactorBuild()
	{
		// Building a single module alone is unsafe (`-pl x -am` rebuilds core and skews every other module), so
		// the whole reactor runs when any one module is missing.
		var root = Directory.CreateTempSubdirectory("beam-scala-mixed");
		try
		{
			var classes = Path.Combine(root.FullName, "core", "target", "classes");
			Directory.CreateDirectory(classes);
			File.WriteAllText(Path.Combine(classes, "Core.class"), "x");
			Directory.CreateDirectory(Path.Combine(root.FullName, "tools", "mail"));

			var step = ScalaBuildStep(root.FullName, "core", "tools/mail");

			Assert.That(LocalStackConfigIO.FirstUnbuiltScalaModule(step, new LocalStackConfig()), Is.EqualTo("tools/mail"));
		}
		finally
		{
			root.Delete(recursive: true);
		}
	}

	[Test]
	public void AnUnresolvedRepoPathIsNeverReportedAsUnbuilt()
	{
		// "Not built" must be a fact, not a guess: an <EDIT: ...> placeholder means we cannot know.
		var step = ScalaBuildStep($"{LocalStackConfigIO.EditPlaceholder} absolute path to BeamableBackend>", "core");

		Assert.That(LocalStackConfigIO.FirstUnbuiltScalaModule(step, new LocalStackConfig()), Is.Null);
		Assert.That(LocalStackConfigIO.BuildOutputMissing(step, new LocalStackConfig()), Is.False);
	}

	[Test]
	public void ManifestsWithoutScalaModulesFallBackToTheMavenModuleList()
	{
		// An existing manifest predates `scalaModules`, but still names its modules in the mvn `-pl` list — so the
		// never-built check works without regenerating the manifest.
		var root = Directory.CreateTempSubdirectory("beam-scala-legacy");
		try
		{
			Directory.CreateDirectory(Path.Combine(root.FullName, "tools", "auth"));
			var step = new LocalStackStep
			{
				name = "build: scala",
				build = true,
				workingDirectory = root.FullName,
				arguments = "-q -pl core,tools/auth -am clean package -DskipTests"
			};

			Assert.That(LocalStackConfigIO.FirstUnbuiltScalaModule(step, new LocalStackConfig()), Is.EqualTo("core"));
			Assert.That(LocalStackConfigIO.BuildOutputMissing(step, new LocalStackConfig()), Is.True);
		}
		finally
		{
			root.Delete(recursive: true);
		}
	}

	[Test]
	public void AStepWithNoModulesAndNoMavenListIsNeverUnbuilt()
	{
		var step = new LocalStackStep { name = "build: portal deps", build = true, workingDirectory = Path.GetTempPath() };

		Assert.That(LocalStackConfigIO.FirstUnbuiltScalaModule(step, new LocalStackConfig()), Is.Null);
		Assert.That(LocalStackConfigIO.BuildOutputMissing(step, new LocalStackConfig()), Is.False);
	}

	// ----------------------------------------------------------------------------------
	// "Have the portal's node deps ever been installed?"
	// ----------------------------------------------------------------------------------

	[Test]
	public void PortalDepsRunWithoutBuildWhenNodeModulesIsMissing()
	{
		// The Windows failure: `npm install` is a --build-only step, so a fresh clone launched Vite against a
		// portal with no dependencies and died with `Cannot find package 'vite'`.
		var portal = Directory.CreateTempSubdirectory("beam-portal-deps");
		try
		{
			var config = LocalStackTemplate.Create(new LocalStackTemplate.Options { portalDir = portal.FullName });
			var step = config.steps.First(s => s.name == "build: portal deps");

			Assert.That(step.requiredOutput, Is.EqualTo(Path.Combine(portal.FullName, "node_modules")));
			Assert.That(LocalStackConfigIO.BuildOutputMissing(step, config), Is.True);

			Directory.CreateDirectory(Path.Combine(portal.FullName, "node_modules"));
			Assert.That(LocalStackConfigIO.BuildOutputMissing(step, config), Is.False,
				"an installed portal must not reinstall on every up");
		}
		finally
		{
			portal.Delete(recursive: true);
		}
	}

	[Test]
	public void AnOlderManifestInfersNodeModulesFromTheNpmInstallStep()
	{
		// Manifests written before requiredOutput was set on this step must self-heal too, without regenerating.
		var portal = Directory.CreateTempSubdirectory("beam-portal-legacy");
		try
		{
			var step = new LocalStackStep
			{
				name = "build: portal deps",
				build = true,
				command = LocalStackTemplate.NpmToken,
				arguments = "install",
				workingDirectory = portal.FullName
			};

			Assert.That(LocalStackConfigIO.BuildOutputMissing(step, new LocalStackConfig()), Is.True);

			Directory.CreateDirectory(Path.Combine(portal.FullName, "node_modules"));
			Assert.That(LocalStackConfigIO.BuildOutputMissing(step, new LocalStackConfig()), Is.False);
		}
		finally
		{
			portal.Delete(recursive: true);
		}
	}

	[Test]
	public void NpmRunStepsAreNotTreatedAsInstalls()
	{
		// `npm run dev` is the long-running portal step - it produces nothing and must never be mistaken for a
		// build whose output is missing, or `up` would try to "build" the dev server on every start.
		var step = new LocalStackStep
		{
			name = "portal frontend",
			build = false,
			command = LocalStackTemplate.NpmToken,
			arguments = "run dev",
			workingDirectory = Path.GetTempPath()
		};

		Assert.That(LocalStackConfigIO.BuildOutputMissing(step, new LocalStackConfig()), Is.False);
	}

	// ----------------------------------------------------------------------------------
	// Pinning the .NET SDK for build steps
	// ----------------------------------------------------------------------------------

	[Test]
	public void ToolchainDotnetRemovesTheSdkRedirects()
	{
		// The Windows failure: the toolchain's 10.0.100 dotnet.exe ran MSBuild against a newer SDK's targets
		// (C:\Program Files\dotnet\sdk\10.0.400) and died with MSB4062. Two mechanisms cause that, and this
		// covers both — the scrubbable env redirects, and the resolver's install-location scan which has no env
		// var to scrub and must instead be CONFINED to the toolchain.
		var dir = Directory.CreateTempSubdirectory("beam-dotnet-env");
		try
		{
			var config = new LocalStackConfig
			{
				toolchain = new LocalStackToolchain { dotnet = dir.FullName }
			};

			var psi = new System.Diagnostics.ProcessStartInfo();
			psi.Environment["MSBuildSDKsPath"] = "/some/other/sdk/Sdks";
			psi.Environment["MSBuildToolsPath"] = "/some/other/sdk";
			psi.Environment["MSBUILD_EXE_PATH"] = "/some/other/msbuild.exe";
			psi.Environment["NuGetRestoreTargets"] = "/some/other/sdk/NuGet.targets";
			psi.Environment["DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR"] = "/some/other/sdk/Sdks";
			psi.Environment["DOTNET_ROOT_X64"] = "/some/other/dotnet";
			// A stale pre-existing pin would keep aiming the resolver at a foreign SDK, so seed one to prove the
			// code OVERWRITES it rather than skipping when it is already set.
			psi.Environment["DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR"] = "/some/other/dotnet";

			LocalStackUpCommand.ApplyToolchainDotnetEnvironment(psi, config);

			// Removed outright, not set to empty — an empty value is still a path to the resolver.
			Assert.That(psi.Environment.ContainsKey("MSBuildSDKsPath"), Is.False);
			Assert.That(psi.Environment.ContainsKey("MSBuildToolsPath"), Is.False);
			Assert.That(psi.Environment.ContainsKey("MSBUILD_EXE_PATH"), Is.False);
			Assert.That(psi.Environment.ContainsKey("NuGetRestoreTargets"), Is.False,
				"NuGetRestoreTargets is the narrow redirect that hits only restore");
			Assert.That(psi.Environment.ContainsKey("DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR"), Is.False);
			Assert.That(psi.Environment.ContainsKey("DOTNET_ROOT_X64"), Is.False,
				"an architecture-specific root takes precedence over DOTNET_ROOT");

			Assert.That(psi.Environment["DOTNET_ROOT"], Is.EqualTo(dir.FullName));
			// PINNED, not removed. Unset, the resolver walks the machine's registered install locations and — from
			// a pre-release toolchain SDK — promotes the project's SDK reference to the newest stable one it finds.
			Assert.That(psi.Environment["DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR"], Is.EqualTo(dir.FullName),
				"the resolver must be confined to the toolchain, not left free to walk install locations");
			// A freshly extracted SDK otherwise installs an HTTPS dev certificate on its first run.
			Assert.That(psi.Environment["DOTNET_GENERATE_ASPNET_CERTIFICATE"], Is.EqualTo("false"));
			Assert.That(psi.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"], Is.EqualTo("1"));
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void WithoutAToolchainDotnetTheEnvironmentIsLeftAlone()
	{
		// A stack that never ran `beam local setup` must behave exactly as before — including keeping whatever
		// SDK redirects the machine has, because its dotnet IS the system one.
		var psi = new System.Diagnostics.ProcessStartInfo();
		// psi.Environment is seeded from THIS process, and a dotnet host newer than the toolchain prepopulates
		// DOTNET_ROOT for its children. Remove it so the assertion tests what the method does, not what the test
		// harness happened to inherit — otherwise this fails on exactly the machines the fix is aimed at.
		psi.Environment.Remove("DOTNET_ROOT");
		psi.Environment["MSBuildSDKsPath"] = "/keep/me";

		LocalStackUpCommand.ApplyToolchainDotnetEnvironment(psi, new LocalStackConfig());

		Assert.That(psi.Environment["MSBuildSDKsPath"], Is.EqualTo("/keep/me"));
		Assert.That(psi.Environment.ContainsKey("DOTNET_ROOT"), Is.False);
	}

	[Test]
	public void AToolchainDotnetPathThatNoLongerExistsIsIgnored()
	{
		var psi = new System.Diagnostics.ProcessStartInfo();
		// psi.Environment is seeded from THIS process, and a dotnet host newer than the toolchain prepopulates
		// DOTNET_ROOT for its children. Remove it so the assertion tests what the method does, not what the test
		// harness happened to inherit — otherwise this fails on exactly the machines the fix is aimed at.
		psi.Environment.Remove("DOTNET_ROOT");
		psi.Environment["MSBuildSDKsPath"] = "/keep/me";

		LocalStackUpCommand.ApplyToolchainDotnetEnvironment(psi, new LocalStackConfig
		{
			toolchain = new LocalStackToolchain { dotnet = Path.Combine(Path.GetTempPath(), "no-such-dotnet") }
		});

		Assert.That(psi.Environment["MSBuildSDKsPath"], Is.EqualTo("/keep/me"),
			"a stale toolchain path must not silently strip the machine's own configuration");
	}

	// ----------------------------------------------------------------------------------
	// Product-dir intactness preflight
	// ----------------------------------------------------------------------------------

	private static string MakeProductLikeDir(bool includeWeb = true, bool includeToolkit = true)
	{
		var dir = Directory.CreateTempSubdirectory("beam-product-check").FullName;
		if (includeWeb)
		{
			Directory.CreateDirectory(Path.Combine(dir, "web"));
			File.WriteAllText(Path.Combine(dir, "web", "package.json"), "{}");
		}
		if (includeToolkit)
		{
			Directory.CreateDirectory(Path.Combine(dir, "beam-portal-toolkit"));
			File.WriteAllText(Path.Combine(dir, "beam-portal-toolkit", "package.json"), "{}");
		}
		return dir;
	}

	[Test]
	public void MissingProductDirMarkers_IsEmpty_WhenBothPresent()
	{
		var dir = MakeProductLikeDir();
		try
		{
			Assert.That(WebLocalRegistryService.MissingProductDirMarkers(dir), Is.Empty);
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Test]
	public void MissingProductDirMarkers_NamesTheAbsentDirectory()
	{
		// The bug this preflight was written for: `web/` disappears while `beam-portal-toolkit/` stays. Names
		// the missing one so the error message downstream can point at what to restore, without listing
		// every marker as absent (which reads as "wrong directory" instead of "partial checkout").
		var dir = MakeProductLikeDir(includeWeb: false, includeToolkit: true);
		try
		{
			Assert.That(WebLocalRegistryService.MissingProductDirMarkers(dir), Is.EqualTo(new[] { "web" }));
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Test]
	public void EnsureProductDirIntact_IsSilent_OnIntactCheckout()
	{
		// The healthy-path cost has to be low — this runs on every `beam local up` that includes the web steps.
		var dir = MakeProductLikeDir();
		try
		{
			Assert.DoesNotThrow(() => WebLocalRegistryService.EnsureProductDirIntact(dir));
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Test]
	public void EnsureProductDirIntact_IsSilent_OnUnknownProductDir()
	{
		// Manifests written before the web-registry steps existed leave productDir null, and `local up` still
		// has to work there — the guard must not throw when there is nothing to check.
		Assert.DoesNotThrow(() => WebLocalRegistryService.EnsureProductDirIntact(null));
		Assert.DoesNotThrow(() => WebLocalRegistryService.EnsureProductDirIntact(string.Empty));
		Assert.DoesNotThrow(() => WebLocalRegistryService.EnsureProductDirIntact(Path.Combine(Path.GetTempPath(), "no-such-dir-" + Guid.NewGuid())));
	}

	[Test]
	public void EnsureProductDirIntact_ThrowsWithRestoreCommand_WhenPartialAndNotAGitRepo()
	{
		// A downloaded ZIP has no .git — the preflight can name what to do (re-clone) but cannot repair.
		var dir = MakeProductLikeDir(includeWeb: false, includeToolkit: true);
		try
		{
			var ex = Assert.Throws<CliException>(() => WebLocalRegistryService.EnsureProductDirIntact(dir));
			Assert.That(ex.Message, Does.Contain("web/"));
			Assert.That(ex.Message, Does.Contain(dir),
				"the message must name the directory so the user can restore or re-clone");
			Assert.That(ex.Message, Does.Contain("not a git checkout"),
				"there is no restore possible here — say why, so the message doesn't read like a bug in the preflight");
		}
		finally { Directory.Delete(dir, recursive: true); }
	}

	[Test]
	public void EnsureProductDirIntact_RestoresFromGit_WhenTrackedButDeleted()
	{
		// The actual bug that motivated the preflight: `web/` is tracked in git, has been wiped from the working
		// tree, and `beam local up` should not have to unwind a 20-service launch to name the fix. Make a real
		// git repo so the preflight's git-restore path exercises end-to-end.
		if (!TryFindGit(out var _))
		{
			Assert.Ignore("git is not on PATH in this test environment");
		}

		var dir = MakeProductLikeDir();
		try
		{
			RunGit(dir, "init", "-q");
			RunGit(dir, "-c", "user.email=t@t", "-c", "user.name=t", "add", ".");
			RunGit(dir, "-c", "user.email=t@t", "-c", "user.name=t", "commit", "-q", "-m", "seed");

			Directory.Delete(Path.Combine(dir, "web"), recursive: true);
			Assert.That(File.Exists(Path.Combine(dir, "web", "package.json")), Is.False);

			Assert.DoesNotThrow(() => WebLocalRegistryService.EnsureProductDirIntact(dir));

			Assert.That(File.Exists(Path.Combine(dir, "web", "package.json")), Is.True,
				"git restore should have re-materialised the working-tree copy from HEAD");
		}
		finally { ForceDeleteDir(dir); }
	}

	/// <summary>
	/// Recursive delete that first clears the read-only bit. Git leaves pack files marked read-only on
	/// Windows, and a plain <c>Directory.Delete(recursive: true)</c> then fails with UnauthorizedAccessException.
	/// </summary>
	private static void ForceDeleteDir(string dir)
	{
		if (!Directory.Exists(dir)) return;
		foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
		{
			try { File.SetAttributes(file, FileAttributes.Normal); } catch { /* best effort */ }
		}
		Directory.Delete(dir, recursive: true);
	}

	private static bool TryFindGit(out string _)
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = "git",
				ArgumentList = { "--version" },
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
			var proc = Process.Start(psi);
			if (proc == null) { _ = null; return false; }
			proc.WaitForExit(5_000);
			_ = null;
			return proc.ExitCode == 0;
		}
		catch { _ = null; return false; }
	}

	private static void RunGit(string workingDir, params string[] args)
	{
		var psi = new ProcessStartInfo
		{
			FileName = "git",
			WorkingDirectory = workingDir,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};
		foreach (var a in args) psi.ArgumentList.Add(a);
		var proc = Process.Start(psi);
		Assert.That(proc, Is.Not.Null);
		var stderr = proc.StandardError.ReadToEnd();
		proc.WaitForExit(20_000);
		Assert.That(proc.ExitCode, Is.EqualTo(0), $"git {string.Join(" ", args)} failed: {stderr}");
	}

	[Test]
	public void TheTemplateRecordsTheModulesItBuilds()
	{
		var scalaDir = Path.Combine(Path.GetPathRoot(Path.GetTempPath()) ?? "/", "repos", "BeamableBackend");
		var config = LocalStackTemplate.Create(new LocalStackTemplate.Options { scalaDir = scalaDir });
		var step = config.steps.FirstOrDefault(s => s.name == "build: scala");

		Assert.That(step, Is.Not.Null, "the scala build step should exist");
		Assert.That(step.scalaModules, Is.Not.Null.And.Not.Empty);
		// Every -pl module the build passes to Maven must be checkable, or `up` could miss an unbuilt one.
		foreach (var module in step.scalaModules)
			Assert.That(step.arguments, Does.Contain(module), $"'{module}' should be in the mvn -pl list");
	}
}
