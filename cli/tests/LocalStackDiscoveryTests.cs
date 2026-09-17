using Beamable.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using cli;
using cli.Commands.LocalStack;
using cli.Services.LocalStack;
using cli.Services.Web;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace tests;

/// <summary>
/// Covers the two "it looked like nothing was there" failures:
/// portal extensions that <c>init</c> could not see, and Scala services that <c>up</c> launched unbuilt.
/// </summary>
public class LocalStackDiscoveryTests
{
	[SetUp]
	public void SetUp()
	{
		// The git-restore path logs a warning; ensure a logger exists in this bare test context.
		BeamableZLoggerProvider.SetLogger(NullLogger.Instance);
	}

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

	// ----------------------------------------------------------------------------------
	// Maven negative-cache guardrails
	// ----------------------------------------------------------------------------------

	[Test]
	public void BuildScala_ForcesSnapshotUpdate()
	{
		// The reactor build has to run with -U so a machine that inherited a cached "com.kickstand:core was
		// not found in nexus" miss (from an older CLI that used `mvn package`) can re-resolve. Without -U,
		// the cached miss short-circuits every subsequent resolve and no amount of `beam local up --build`
		// helps — the specific failure the user hit on Pedro's box.
		var scalaDir = Path.Combine(Path.GetPathRoot(Path.GetTempPath()) ?? "/", "repos", "BeamableBackend");
		var config = LocalStackTemplate.Create(new LocalStackTemplate.Options { scalaDir = scalaDir });
		var step = config.steps.FirstOrDefault(s => s.name == "build: scala");
		Assert.That(step, Is.Not.Null, "the scala build step should exist");
		Assert.That(step.arguments, Does.Contain(" -U "),
			"-U forces re-check of remote for SNAPSHOTs and invalidates the cached miss");
		Assert.That(step.arguments, Does.Contain("install"),
			"install (not package) is what writes core to ~/.m2 — the -U is only load-bearing paired with install");
	}

	[Test]
	public void ScalaLauncher_UsesOfflineMode_OnPowerShell()
	{
		// The per-service launcher never needs to touch remote: every dep is in ~/.m2 (public artifacts from an
		// earlier resolve, plus `core` from `build: scala`'s mvn install). -o (offline) means Maven cannot
		// consult `_remote.repositories` for a cached miss — which is the mechanism the negative cache uses to
		// short-circuit resolution.
		var config = new LocalStackConfig();
		var arguments = InvokeScalaLaunchPowerShell("dbflake", "com.kickstand.tools.dbflake.App", string.Empty);
		var substituted = LocalStackConfigIO.Substitute(arguments, config);
		Assert.That(substituted, Does.Contain("dependency:build-classpath"),
			"the classpath cache must still be built");
		Assert.That(substituted, Does.Contain(" -o "),
			"the launcher must run offline — sidesteps any stale negative-cache entry for com.kickstand:core");
	}

	[Test]
	public void ScalaLauncher_UsesOfflineMode_OnShell()
	{
		var config = new LocalStackConfig();
		var arguments = InvokeScalaLaunchShell("dbflake", "com.kickstand.tools.dbflake.App", string.Empty);
		var substituted = LocalStackConfigIO.Substitute(arguments, config);
		Assert.That(substituted, Does.Contain("dependency:build-classpath"));
		Assert.That(substituted, Does.Contain(" -o "),
			"the launcher must run offline — sidesteps any stale negative-cache entry for com.kickstand:core");
	}

	private static string InvokeScalaLaunchPowerShell(string svc, string mainClass, string jvmArgs) =>
		InvokeScalaLauncherPrivate("ScalaLaunchPowerShell", svc, mainClass, jvmArgs);

	private static string InvokeScalaLaunchShell(string svc, string mainClass, string jvmArgs) =>
		InvokeScalaLauncherPrivate("ScalaLaunchShell", svc, mainClass, jvmArgs);

	private static string InvokeScalaLauncherPrivate(string methodName, string svc, string mainClass, string jvmArgs)
	{
		// The launcher builders are private helpers of LocalStackTemplate. Reflection avoids widening their
		// visibility just for a test — same trade-off as the existing tests that reach into private helpers
		// elsewhere in this file.
		var m = typeof(LocalStackTemplate).GetMethod(methodName,
			System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
		Assert.That(m, Is.Not.Null, methodName + " should be present on LocalStackTemplate");
		var result = m!.Invoke(null, new object[] { svc, mainClass, jvmArgs });
		Assert.That(result, Is.Not.Null);
		return (string)result!;
	}

	[Test]
	public void MissingCoreInM2_ForcesBuildScalaWhenScalaLaunchIsPresent()
	{
		// The exact state Pedro's box was in: build: scala exists in the manifest, target/classes is populated
		// (from an older CLI's `mvn package`), and ~/.m2 has no core. Without this preflight, autoBuild stays
		// empty on a plain `beam local up`, and the launcher hits the negative-cache trap.
		var config = new LocalStackConfig
		{
			steps = new List<LocalStackStep>
			{
				new LocalStackStep { name = "build: scala", enabled = true, build = true },
				new LocalStackStep { name = "scala: dbflake", enabled = true },
			}
		};
		var autoBuild = new HashSet<LocalStackStep>();

		var jarPath = Path.Combine(Path.GetTempPath(), "no-such-core-" + Guid.NewGuid() + ".jar");
		var result = LocalStackUpCommand.ShouldForceBuildScalaForMissingCore(config, autoBuild, jarPath);

		Assert.That(result, Is.Not.Null, "missing core with a scala launch step should force build: scala");
		Assert.That(result.name, Is.EqualTo("build: scala"));
	}

	[Test]
	public void MissingCoreInM2_LeftAlone_WhenBuildScalaIsAlreadyInAutoBuild()
	{
		// With --build (autoBuild starts empty) or when BuildOutputMissing already picked build: scala, this
		// preflight has no work to do — it must not duplicate.
		var buildStep = new LocalStackStep { name = "build: scala", enabled = true, build = true };
		var config = new LocalStackConfig
		{
			steps = new List<LocalStackStep>
			{
				buildStep,
				new LocalStackStep { name = "scala: dbflake", enabled = true },
			}
		};
		var autoBuild = new HashSet<LocalStackStep> { buildStep };

		var jarPath = Path.Combine(Path.GetTempPath(), "no-such-core-" + Guid.NewGuid() + ".jar");
		var result = LocalStackUpCommand.ShouldForceBuildScalaForMissingCore(config, autoBuild, jarPath);
		Assert.That(result, Is.Null);
	}

	[Test]
	public void MissingCoreInM2_LeftAlone_WhenNoScalaLaunchStep()
	{
		// A stack with no scala services (--skip "scala: *", or an init that never included them) shouldn't
		// have a Scala reactor forced on it just because ~/.m2 lacks an artifact that's never going to be used.
		var config = new LocalStackConfig
		{
			steps = new List<LocalStackStep>
			{
				new LocalStackStep { name = "build: scala", enabled = true, build = true },
				new LocalStackStep { name = "docker: api deps + caddy", enabled = true },
			}
		};
		var autoBuild = new HashSet<LocalStackStep>();

		var jarPath = Path.Combine(Path.GetTempPath(), "no-such-core-" + Guid.NewGuid() + ".jar");
		var result = LocalStackUpCommand.ShouldForceBuildScalaForMissingCore(config, autoBuild, jarPath);
		Assert.That(result, Is.Null);
	}

	// ----------------------------------------------------------------------------------
	// Offline maven-dependency-plugin probe (the second half of the negative-cache trap:
	// core is present, but `mvn -o dependency:build-classpath` can't resolve the `dependency`
	// plugin prefix offline, so every scala service fails and --build never fixes it).
	// ----------------------------------------------------------------------------------

	private static LocalStackConfig ProbeConfig(string scalaDir, string mavenHome, params (string name, bool enabled)[] steps) =>
		new LocalStackConfig
		{
			repos = new LocalStackRepos { scalaDir = scalaDir },
			toolchain = mavenHome == null ? null : new LocalStackToolchain { maven = mavenHome },
			steps = steps.Select(s => new LocalStackStep { name = s.name, enabled = s.enabled }).ToList()
		};

	[Test]
	public void PlanDependencyPluginProbe_DerivesModuleFromScalaLaunchStep()
	{
		// A scala service will launch, so the launcher's offline classpath resolve will run — the probe is relevant.
		// The module it targets mirrors the launcher's `-pl tools/$SVC`, derived from the step name after "scala: ".
		var config = ProbeConfig(Path.GetTempPath(), Path.Combine(Path.GetTempPath(), "mvn-home"),
			("build: scala", true), ("scala: account", true), ("scala: dbflake", true));

		var plan = LocalStackUpCommand.PlanDependencyPluginProbe(config);

		Assert.That(plan.shouldProbe, Is.True, plan.skipReason);
		Assert.That(plan.probeModule, Is.EqualTo("tools/account"), "first scala launch step wins");
		Assert.That(plan.scalaDir, Is.EqualTo(Path.GetTempPath()));
		Assert.That(plan.mvnCommand, Does.Contain("mvn"), "resolves the toolchain's mvn from ${maven}");
	}

	[Test]
	public void PlanDependencyPluginProbe_SkipsWhenNoScalaLaunchStep()
	{
		// No scala service means no launcher, so the offline classpath resolve never runs — don't pay for a probe.
		var config = ProbeConfig(Path.GetTempPath(), Path.Combine(Path.GetTempPath(), "mvn-home"),
			("build: scala", true), ("docker: api deps + caddy", true));

		var plan = LocalStackUpCommand.PlanDependencyPluginProbe(config);

		Assert.That(plan.shouldProbe, Is.False);
		Assert.That(plan.skipReason, Is.EqualTo("no scala launch step"));
	}

	[Test]
	public void PlanDependencyPluginProbe_SkipsWhenScalaStepIsDisabled()
	{
		// A disabled scala step won't launch, so it must not drag the probe in.
		var config = ProbeConfig(Path.GetTempPath(), Path.Combine(Path.GetTempPath(), "mvn-home"),
			("scala: account", false));

		var plan = LocalStackUpCommand.PlanDependencyPluginProbe(config);

		Assert.That(plan.shouldProbe, Is.False);
		Assert.That(plan.skipReason, Is.EqualTo("no scala launch step"));
	}

	[Test]
	public void PlanDependencyPluginProbe_SkipsWhenScalaDirIsAPlaceholder()
	{
		// An un-edited manifest carries the EditPlaceholder for repo paths; there's no real dir to run mvn in.
		var config = ProbeConfig("<" + "EDIT-ME" + ">", Path.Combine(Path.GetTempPath(), "mvn-home"),
			("scala: account", true));
		// Force the actual placeholder token in case the constant differs from the guess above.
		config.repos.scalaDir = LocalStackConfigIO.EditPlaceholder;

		var plan = LocalStackUpCommand.PlanDependencyPluginProbe(config);

		Assert.That(plan.shouldProbe, Is.False);
		Assert.That(plan.skipReason, Is.EqualTo("no BeamableBackend path on the manifest"));
	}

	[Test]
	public void PlanDependencyPluginProbe_ResolvesBareMvn_WithoutAToolchain()
	{
		// No toolchain (never ran `beam local setup`): ${maven} falls back to the bare command, which is still a
		// valid thing to probe with — it resolves via PATH just like the reactor step would.
		var config = ProbeConfig(Path.GetTempPath(), mavenHome: null, ("scala: account", true));

		var plan = LocalStackUpCommand.PlanDependencyPluginProbe(config);

		Assert.That(plan.shouldProbe, Is.True, plan.skipReason);
		Assert.That(plan.mvnCommand, Does.Contain("mvn"));
	}

	// ----------------------------------------------------------------------------------
	// In-memory manifest migration for pre-Maven-cache-fix arguments
	// ----------------------------------------------------------------------------------

	[Test]
	public void MigrateBuildScala_SwapsPackageForInstallAndAddsU()
	{
		// The exact string an older CLI wrote into local-stack.json. Rewriting only in-memory (never on disk)
		// lets an existing manifest self-heal without asking every user to re-run `beam local init`.
		var before = "-q -pl core,tools/dbflake,tools/gateway -am clean package -DskipTests";
		var after = LocalStackUpCommand.MigrateBuildScalaArguments(before);
		Assert.That(after, Does.Contain("clean install -DskipTests"),
			"install (not package) is what writes core to ~/.m2 — the load-bearing part of the migration");
		Assert.That(after, Does.Contain("-U "),
			"-U invalidates any cached 'not found in nexus' entry — the reason --build didn't help on Pedro's box");
		Assert.That(after, Does.StartWith("-q -U "),
			"flag order should match what the current template emits so diffs stay readable");
	}

	[Test]
	public void MigrateBuildScala_IsIdempotent()
	{
		// The migration runs on every `up`; running it twice on the same input must not double-add -U or
		// re-swap install back. Otherwise a stray retry loop could produce nonsense arguments.
		var first = LocalStackUpCommand.MigrateBuildScalaArguments(
			"-q -pl core,tools/dbflake -am clean package -DskipTests");
		var second = LocalStackUpCommand.MigrateBuildScalaArguments(first);
		Assert.That(second, Is.EqualTo(first));
	}

	[Test]
	public void MigrateBuildScala_NoOp_OnAlreadyMigrated()
	{
		var current = "-q -U -pl core,tools/dbflake -am clean install -DskipTests";
		Assert.That(LocalStackUpCommand.MigrateBuildScalaArguments(current), Is.EqualTo(current));
	}

	[Test]
	public void MigrateScalaLauncher_InsertsOfflineFlagOnce()
	{
		// The launcher script (a whole PowerShell / sh program) has exactly one `dependency:build-classpath`
		// call. Inserting `-o` right before that goal is the least intrusive rewrite — order of `mvn` flags
		// doesn't matter to Maven, so this is safe.
		var before =
			"if ($stale) { & '${maven}' -q -pl \"tools/$svc\" -am dependency:build-classpath \"-Dmdep.outputFile=$cpf\" }";
		var after = LocalStackUpCommand.MigrateScalaLauncherArguments(before);
		Assert.That(after, Does.Contain("-o dependency:build-classpath"),
			"launcher must run offline — sidesteps any stale negative-cache entry for com.kickstand:core");
		// Idempotence.
		Assert.That(LocalStackUpCommand.MigrateScalaLauncherArguments(after), Is.EqualTo(after));
	}

	[Test]
	public void MigrateScalaLauncher_NoOp_WhenAlreadyOffline()
	{
		var already =
			"& '${maven}' -q -o -pl \"tools/$svc\" -am dependency:build-classpath \"-Dmdep.outputFile=$cpf\"";
		Assert.That(LocalStackUpCommand.MigrateScalaLauncherArguments(already), Is.EqualTo(already));
	}

	[Test]
	public void MigrateScalaLauncher_NoOp_OnEmptyOrUnrelatedContent()
	{
		Assert.That(LocalStackUpCommand.MigrateScalaLauncherArguments(null), Is.Null);
		Assert.That(LocalStackUpCommand.MigrateScalaLauncherArguments(string.Empty), Is.EqualTo(string.Empty));
		Assert.That(LocalStackUpCommand.MigrateScalaLauncherArguments("echo hi"), Is.EqualTo("echo hi"),
			"a launcher that doesn't call mvn should be left alone");
	}

	[Test]
	public void MigrateStaleScalaArguments_RewritesBuildScalaAndEveryScalaLauncher()
	{
		// End-to-end shape: the whole manifest walk should hit the reactor step and every scala: launcher step,
		// but leave unrelated steps (docker, c#, portal frontend) untouched.
		var config = new LocalStackConfig
		{
			steps = new List<LocalStackStep>
			{
				new LocalStackStep
				{
					name = "build: scala",
					arguments = "-q -pl core,tools/dbflake -am clean package -DskipTests"
				},
				new LocalStackStep
				{
					name = "scala: dbflake",
					arguments = "& '${maven}' -q -pl \"tools/$svc\" -am dependency:build-classpath \"-Dmdep.outputFile=$cpf\""
				},
				new LocalStackStep
				{
					name = "scala: gateway",
					arguments = "mvn -q -pl tools/$SVC -am dependency:build-classpath -Dmdep.outputFile=$CPF"
				},
				new LocalStackStep
				{
					name = "docker: api deps + caddy",
					arguments = "compose up -d --wait"
				},
			}
		};

		LocalStackUpCommand.MigrateStaleScalaArguments(config);

		Assert.That(config.steps[0].arguments, Does.Contain("-U ").And.Contains("clean install"));
		Assert.That(config.steps[1].arguments, Does.Contain("-o dependency:build-classpath"));
		Assert.That(config.steps[2].arguments, Does.Contain("-o dependency:build-classpath"));
		Assert.That(config.steps[3].arguments, Is.EqualTo("compose up -d --wait"),
			"unrelated docker/c#/frontend steps must be left alone");
	}

	[Test]
	public void MissingCoreInM2_LeftAlone_WhenCoreJarPresent()
	{
		var config = new LocalStackConfig
		{
			steps = new List<LocalStackStep>
			{
				new LocalStackStep { name = "build: scala", enabled = true, build = true },
				new LocalStackStep { name = "scala: dbflake", enabled = true },
			}
		};
		var autoBuild = new HashSet<LocalStackStep>();

		var jarPath = Path.GetTempFileName();
		try
		{
			var result = LocalStackUpCommand.ShouldForceBuildScalaForMissingCore(config, autoBuild, jarPath);
			Assert.That(result, Is.Null,
				"a machine with core already installed to ~/.m2 does not need an unrequested rebuild");
		}
		finally { File.Delete(jarPath); }
	}

	// ----------------------------------------------------------------------------------
	// Scala Mongo-startup-race retry heuristic
	// ----------------------------------------------------------------------------------

	private const string MongoTimeoutLine =
		"[scala: leaderboards] com.mongodb.MongoTimeoutException: Timed out after 5000 ms while waiting to connect. " +
		"Client view of cluster state is {type=UNKNOWN, servers=[{address=localhost:27015, type=UNKNOWN, state=CONNECTING}]";

	[Test]
	public void MongoStartupFailure_Detected_WhenTailShowsUnrecoveredException()
	{
		// The real bug: three Scala services hit MongoTimeoutException on startup, parked, never bound a port,
		// and the CLI's readyRetries could not fire because StepIsDeadOnItsPort needs `step.port` set. This
		// gate has to say YES on that exact tail so the retry branch is unblocked.
		var log = Path.GetTempFileName();
		try
		{
			File.WriteAllText(log,
				"[scala: leaderboards] INFO Some earlier startup line\n" +
				MongoTimeoutLine + "\n" +
				"[scala: leaderboards]   at com.mongodb.internal.connection.BaseCluster.getDescription(BaseCluster.java:185)\n" +
				"[scala: leaderboards] still starting — 60/120s\n");
			Assert.That(LocalStackUpCommand.LogTailShowsMongoStartupFailure(log), Is.True);
		}
		finally { File.Delete(log); }
	}

	[Test]
	public void MongoStartupFailure_NotDetected_WhenServiceRecoveredAndBound()
	{
		// A service that lost the first Mongo attempt but then reconnected and logged "Service Started" (BASIC/
		// OBJECT provider) is not stuck. Retrying it would kill a service that is already serving traffic —
		// exactly the false-positive the developer's block-comment about StepIsDeadOnItsPort warns against.
		var log = Path.GetTempFileName();
		try
		{
			File.WriteAllText(log,
				MongoTimeoutLine + "\n" +
				"[scala: leaderboards] INFO reconnected to mongo, continuing startup\n" +
				"[scala: leaderboards] INFO basic Service Started: leaderboards\n");
			Assert.That(LocalStackUpCommand.LogTailShowsMongoStartupFailure(log), Is.False);
		}
		finally { File.Delete(log); }
	}

	[Test]
	public void MongoStartupFailure_NotDetected_WhenAkkaHttpBoundAfterMongoRace()
	{
		// The gateway/analytics-gateway variant: the readiness log line is "Serving traffic ..." rather than
		// "Service Started". Same treatment — if bind followed the timeout, it recovered on its own.
		var log = Path.GetTempFileName();
		try
		{
			File.WriteAllText(log,
				MongoTimeoutLine + "\n" +
				"[scala: gateway] INFO Serving traffic on 0.0.0.0:9002\n");
			Assert.That(LocalStackUpCommand.LogTailShowsMongoStartupFailure(log), Is.False);
		}
		finally { File.Delete(log); }
	}

	[Test]
	public void MongoStartupFailure_NotDetected_OnHealthyStartup()
	{
		// A service that never touched Mongo timeout at all must not trip the gate — otherwise every healthy
		// Scala service on a `readyTimeoutSeconds=120` gate would be killed and relaunched.
		var log = Path.GetTempFileName();
		try
		{
			File.WriteAllText(log,
				"[scala: account] INFO Preloaded signing key and cookie\n" +
				"[scala: account] INFO basic Service Started: account\n");
			Assert.That(LocalStackUpCommand.LogTailShowsMongoStartupFailure(log), Is.False);
		}
		finally { File.Delete(log); }
	}

	[Test]
	public void MongoStartupFailure_NotDetected_OnMissingOrEmptyLog()
	{
		// Detached runs may not have flushed anything yet; missing/empty files must NOT be treated as failure,
		// or the retry would fire against every fresh launch.
		Assert.That(LocalStackUpCommand.LogTailShowsMongoStartupFailure(null), Is.False);
		Assert.That(LocalStackUpCommand.LogTailShowsMongoStartupFailure(string.Empty), Is.False);
		Assert.That(LocalStackUpCommand.LogTailShowsMongoStartupFailure(
			Path.Combine(Path.GetTempPath(), "no-such-file-" + Guid.NewGuid())), Is.False);

		var empty = Path.GetTempFileName();
		try
		{
			Assert.That(LocalStackUpCommand.LogTailShowsMongoStartupFailure(empty), Is.False);
		}
		finally { File.Delete(empty); }
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
