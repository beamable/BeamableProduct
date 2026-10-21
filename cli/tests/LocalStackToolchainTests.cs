using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cli.Commands.LocalStack;
using cli.Services.LocalStack;
using NUnit.Framework;

namespace tests;

/// <summary>
/// Covers the pinned toolchain <c>beam local setup</c> provisions and how a manifest resolves commands against
/// it: the platform-specific layout rules, the token substitution that replaces bare <c>mvn</c>/<c>npm</c>/
/// <c>dotnet</c>, the PATH prefix that keeps nested invocations inside the toolchain, and the fallbacks that keep
/// a manifest working on a machine where setup was never run.
/// </summary>
public class LocalStackToolchainTests
{
	private static readonly string Root = Path.Combine(Path.GetPathRoot(Path.GetTempPath()) ?? "/", "toolchain-tests");

	// ----------------------------------------------------------------------------------
	// Layout
	// ----------------------------------------------------------------------------------

	[Test]
	public void DotnetExecutableSitsAtTheInstallRoot()
	{
		// dotnet-install puts `dotnet` at the root of --install-dir, not in a bin/ subdirectory. Getting this
		// wrong makes every resolved path miss by one directory.
		Assert.That(ToolchainService.BinSubdir(ToolchainPins.Dotnet), Is.Empty);
	}

	[Test]
	public void NodeExecutableLocationIsPlatformSpecific()
	{
		// The Node tarball has bin/node on POSIX, but the Windows zip puts node.exe and npm.cmd at the archive
		// root. A single hard-coded "bin" would break npm resolution on exactly one platform.
		Assert.That(ToolchainService.BinSubdir(ToolchainPins.Node),
			Is.EqualTo(OperatingSystem.IsWindows() ? string.Empty : "bin"));
	}

	[Test]
	public void PnpmSharesNodesLayout()
	{
		// `npm --global --prefix` lays pnpm out exactly like Node itself: bin/ on POSIX, shims at the root on
		// Windows. Getting this wrong makes the resolved path miss by one directory on one platform only.
		Assert.That(ToolchainService.BinSubdir(ToolchainPins.Pnpm),
			Is.EqualTo(ToolchainService.BinSubdir(ToolchainPins.Node)));
		Assert.That(ToolchainService.ProbeExecutable(ToolchainPins.Pnpm),
			Is.EqualTo(OperatingSystem.IsWindows() ? "pnpm.cmd" : "pnpm"));
	}

	[Test]
	public void PnpmPinIsExactBecauseTheWebPackagesPinIt()
	{
		// web/package.json and beam-portal-toolkit/package.json both declare `packageManager: pnpm@<ver>`, and pnpm
		// refuses to run a project pinned to a different version of itself — so "close enough" is not a thing.
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Pnpm, ToolchainPins.PnpmVersion), Is.True);
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Pnpm, "v" + ToolchainPins.PnpmVersion), Is.True);
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Pnpm, "9.0.0"), Is.False);
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Pnpm, null), Is.False);
	}

	[Test]
	public void PnpmIsInstalledAfterNodeBecauseItNeedsNpm()
	{
		// pnpm is installed BY the toolchain's npm, so Node has to be in place first.
		var ids = ToolchainPins.ToolIds.ToList();
		Assert.That(ids, Contains.Item(ToolchainPins.Pnpm));
		Assert.That(ids.IndexOf(ToolchainPins.Pnpm), Is.GreaterThan(ids.IndexOf(ToolchainPins.Node)));
		Assert.That(ToolchainPins.AllStepIds, Contains.Item(ToolchainPins.Pnpm));
	}

	[Test]
	public void PnpmIsOnThePathPrefixBecauseItHasNoToken()
	{
		// `beam web publish` shells out to a bare `pnpm`, so there is no token to substitute — the only thing that
		// makes it resolvable is the step's PATH.
		var dir = Directory.CreateTempSubdirectory("beam-pnpm-path");
		try
		{
			var binDir = OperatingSystem.IsWindows()
				? dir.FullName
				: Directory.CreateDirectory(Path.Combine(dir.FullName, "bin")).FullName;

			var config = new LocalStackConfig
			{
				toolchain = new LocalStackToolchain { pnpm = dir.FullName }
			};

			Assert.That(LocalStackConfigIO.ToolchainPathPrefix(config), Contains.Item(binDir));
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void JdkAndMavenUseBinSubdirectory()
	{
		Assert.That(ToolchainService.BinSubdir(ToolchainPins.Jdk), Is.EqualTo("bin"));
		Assert.That(ToolchainService.BinSubdir(ToolchainPins.Maven), Is.EqualTo("bin"));
	}

	[Test]
	public void ExecutablePathJoinsHomeBinAndExecutable()
	{
		var home = Path.Combine(Root, "jdk8", "8.0.502");
		var expected = Path.Combine(home, "bin", OperatingSystem.IsWindows() ? "java.exe" : "java");

		Assert.That(ToolchainService.ExecutablePath(ToolchainPins.Jdk, home), Is.EqualTo(expected));
	}

	// ----------------------------------------------------------------------------------
	// Pins
	// ----------------------------------------------------------------------------------

	[Test]
	public void NodePinAcceptsAnyPatchOfTheMajorButNotAnotherMajor()
	{
		// The portal is built against Node 22 (its Dockerfile is node:22-alpine). A newer major installs
		// different transitive deps, so it must NOT satisfy the pin — that drift is the reason for pinning.
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Node, "v22.23.2"), Is.True);
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Node, "22.1.0"), Is.True);
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Node, "v25.2.1"), Is.False);
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Node, "v20.11.0"), Is.False);
	}

	[Test]
	public void MavenPinIsExact()
	{
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Maven, ToolchainPins.MavenVersion), Is.True);
		// 3.9.2 is what a machine with sdkman-installed Maven typically has; it is not the pin.
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Maven, "3.9.2"), Is.False);
	}

	[Test]
	public void JdkPinAcceptsTheJava8VersionStrings()
	{
		// `java -version` on 8 reports 1.8.0_xxx; Azul's metadata reports 8.0.502.
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Jdk, "1.8.0_502"), Is.True);
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Jdk, "8.0.502"), Is.True);
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Jdk, "17.0.19"), Is.False);
		Assert.That(ToolchainService.SatisfiesPin(ToolchainPins.Jdk, "21.0.8"), Is.False);
	}

	[Test]
	public void EmptyVersionNeverSatisfiesAPin()
	{
		foreach (var toolId in ToolchainPins.ToolIds)
		{
			Assert.That(ToolchainService.SatisfiesPin(toolId, null), Is.False, toolId);
			Assert.That(ToolchainService.SatisfiesPin(toolId, "  "), Is.False, toolId);
		}
	}

	[Test]
	public void NodeArchiveSuffixMatchesTheCurrentPlatform()
	{
		var suffix = ToolchainPins.NodePlatformSuffix();
		var expectedOs = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "darwin" : "linux";
		var expectedArch = ToolchainPins.IsArm64 ? "arm64" : "x64";

		Assert.That(suffix, Is.EqualTo($"{expectedOs}-{expectedArch}"));
	}

	[Test]
	public void EveryStepIdIsRoutable()
	{
		// --only/--skip accept the tool ids plus the two non-tool steps; a typo here silently makes a step
		// unselectable.
		Assert.That(ToolchainPins.AllStepIds, Is.SupersetOf(ToolchainPins.ToolIds));
		Assert.That(ToolchainPins.AllStepIds, Contains.Item(ToolchainPins.ScalaConfig));
		Assert.That(ToolchainPins.AllStepIds, Contains.Item(ToolchainPins.Aws));
	}

	// ----------------------------------------------------------------------------------
	// Token substitution
	// ----------------------------------------------------------------------------------

	[Test]
	public void CommandTokensFallBackToBareCommandsWithoutAToolchain()
	{
		// A manifest that predates `beam local setup`, or a machine where it was never run, must behave exactly as
		// before: the tokens resolve to bare names and PATH does the rest.
		var config = new LocalStackConfig();

		Assert.That(LocalStackConfigIO.MavenCommand(config),
			Is.EqualTo(OperatingSystem.IsWindows() ? "mvn.cmd" : "mvn"));
		Assert.That(LocalStackConfigIO.NpmCommand(config),
			Is.EqualTo(OperatingSystem.IsWindows() ? "npm.cmd" : "npm"));
		Assert.That(LocalStackConfigIO.DotnetCommand(config),
			Is.EqualTo(OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"));
	}

	[Test]
	public void CommandTokensFallBackWhenTheToolchainDirectoryIsGone()
	{
		// A manifest can name a toolchain that has since been deleted (a cleaned temp dir, a shared manifest from
		// another machine). Resolving to a path that does not exist would fail with "no such file"; falling back
		// to PATH at least runs.
		var config = new LocalStackConfig
		{
			toolchain = new LocalStackToolchain { maven = Path.Combine(Root, "does-not-exist") }
		};

		Assert.That(LocalStackConfigIO.MavenCommand(config),
			Is.EqualTo(OperatingSystem.IsWindows() ? "mvn.cmd" : "mvn"));
	}

	[Test]
	public void CommandTokensFallBackForAnUneditedPlaceholder()
	{
		var config = new LocalStackConfig
		{
			toolchain = new LocalStackToolchain { maven = $"{LocalStackConfigIO.EditPlaceholder} absolute path to maven>" }
		};

		Assert.That(LocalStackConfigIO.MavenCommand(config),
			Is.EqualTo(OperatingSystem.IsWindows() ? "mvn.cmd" : "mvn"));
	}

	[Test]
	public void SubstituteResolvesCommandTokensInStepArguments()
	{
		// The Scala launch script embeds a `mvn dependency:build-classpath` call in its body, so the token has to
		// be substituted inside `arguments`, not just in `command`.
		var config = new LocalStackConfig();
		var substituted = LocalStackConfigIO.Substitute($"JAVA_HOME=x {LocalStackTemplate.MavenToken} -q -pl tools/auth", config);

		Assert.That(substituted, Does.Not.Contain(LocalStackTemplate.MavenToken));
		Assert.That(substituted, Does.Contain("mvn"));
	}

	[Test]
	public void SubstituteStillResolvesTheJavaHomeToken()
	{
		var config = new LocalStackConfig { javaHome = Path.Combine(Root, "jdk8", "home") };

		Assert.That(LocalStackConfigIO.Substitute("${java}/bin/java", config),
			Is.EqualTo(config.javaHome + "/bin/java"));
	}

	// ----------------------------------------------------------------------------------
	// PATH prefix
	// ----------------------------------------------------------------------------------

	[Test]
	public void PathPrefixIsEmptyWithoutAToolchain()
	{
		Assert.That(LocalStackConfigIO.ToolchainPathPrefix(new LocalStackConfig()), Is.Empty);
		Assert.That(LocalStackConfigIO.ToolchainPathPrefix(null), Is.Empty);
	}

	[Test]
	public void PathPrefixSkipsDirectoriesThatDoNotExist()
	{
		var config = new LocalStackConfig
		{
			toolchain = new LocalStackToolchain
			{
				java = Path.Combine(Root, "missing-jdk"),
				maven = Path.Combine(Root, "missing-maven")
			}
		};

		Assert.That(LocalStackConfigIO.ToolchainPathPrefix(config), Is.Empty);
	}

	[Test]
	public void PathPrefixPutsJavaFirst()
	{
		// Java leads deliberately: it is the tool every other one picks up implicitly (Maven resolves its JDK from
		// JAVA_HOME/PATH), so a stale JDK earlier on PATH is the drift that is hardest to notice.
		var dir = Directory.CreateTempSubdirectory("beam-toolchain-test");
		try
		{
			var javaBin = Directory.CreateDirectory(Path.Combine(dir.FullName, "jdk", "bin")).FullName;
			var mavenBin = Directory.CreateDirectory(Path.Combine(dir.FullName, "maven", "bin")).FullName;

			var config = new LocalStackConfig
			{
				toolchain = new LocalStackToolchain
				{
					java = Path.Combine(dir.FullName, "jdk"),
					maven = Path.Combine(dir.FullName, "maven")
				}
			};

			var prefix = LocalStackConfigIO.ToolchainPathPrefix(config).ToList();
			Assert.That(prefix, Is.EqualTo(new List<string> { javaBin, mavenBin }));
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	// ----------------------------------------------------------------------------------
	// Toolchain directory resolution
	// ----------------------------------------------------------------------------------

	[Test]
	public void ExplicitToolchainDirWins()
	{
		var explicitDir = Path.Combine(Root, "explicit");
		Assert.That(ToolchainService.ResolveDir(explicitDir), Is.EqualTo(Path.GetFullPath(explicitDir)));
	}

	[Test]
	public void ToolchainDirDefaultsUnderTheUserProfile()
	{
		// Under the user profile, not the workspace: several checkouts share one install, and a JDK per repo
		// would be gigabytes for no benefit.
		var expected = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ToolchainService.DefaultDirName);

		var previous = Environment.GetEnvironmentVariable(ToolchainService.EnvVarToolchainDir);
		try
		{
			Environment.SetEnvironmentVariable(ToolchainService.EnvVarToolchainDir, null);
			Assert.That(ToolchainService.ResolveDir(null), Is.EqualTo(expected));
		}
		finally
		{
			Environment.SetEnvironmentVariable(ToolchainService.EnvVarToolchainDir, previous);
		}
	}

	[Test]
	public void ToolchainDirCanComeFromTheEnvironment()
	{
		var previous = Environment.GetEnvironmentVariable(ToolchainService.EnvVarToolchainDir);
		try
		{
			var fromEnv = Path.Combine(Root, "from-env");
			Environment.SetEnvironmentVariable(ToolchainService.EnvVarToolchainDir, fromEnv);

			Assert.That(ToolchainService.ResolveDir(null), Is.EqualTo(Path.GetFullPath(fromEnv)));
			// An explicit value still beats the environment.
			Assert.That(ToolchainService.ResolveDir(Path.Combine(Root, "explicit")),
				Is.EqualTo(Path.GetFullPath(Path.Combine(Root, "explicit"))));
		}
		finally
		{
			Environment.SetEnvironmentVariable(ToolchainService.EnvVarToolchainDir, previous);
		}
	}

	// ----------------------------------------------------------------------------------
	// setup/init ordering
	// ----------------------------------------------------------------------------------

	[Test]
	public void TheHomeDirectoryIsNeverTreatedAsAWorkspace()
	{
		// Workspace lookup walks up from the working directory, so a ~/.beamable makes EVERY command run outside a
		// workspace resolve to the home folder — silently writing a manifest, config and logs there, and then
		// picking that phantom workspace up on later runs from unrelated directories.
		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

		Assert.That(LocalStackCommand.IsHomeWorkspace(Path.Combine(home, ".beamable")), Is.True);
		Assert.That(LocalStackCommand.IsHomeWorkspace(Path.Combine(home, ".beamable") + Path.DirectorySeparatorChar), Is.True);

		// A real workspace under the home directory is still a real workspace.
		Assert.That(LocalStackCommand.IsHomeWorkspace(Path.Combine(home, "repos", "portal", ".beamable")), Is.False);
		Assert.That(LocalStackCommand.IsHomeWorkspace(Path.Combine(Root, "somewhere", ".beamable")), Is.False);
	}

	[Test]
	public void RepoDiscoveryWalksUpFromTheWorkingDirectory()
	{
		// `setup` has to find the checkouts itself, because it is meant to run BEFORE any manifest exists.
		var dir = Directory.CreateTempSubdirectory("beam-repo-discovery");
		try
		{
			var api = Directory.CreateDirectory(Path.Combine(dir.FullName, LocalStackCommand.ApiRepoName)).FullName;
			var nested = Directory.CreateDirectory(Path.Combine(dir.FullName, "a", "b")).FullName;

			Assert.That(LocalStackCommand.FindRepoDir(nested, LocalStackCommand.ApiRepoName), Is.EqualTo(api));
			Assert.That(LocalStackCommand.FindRepoDir(nested, "NoSuchRepo"), Is.Null);
			// Bounded: it must not climb to the filesystem root looking for a name that is not there.
			Assert.That(LocalStackCommand.FindRepoDir(nested, LocalStackCommand.ApiRepoName, maxLevels: 0), Is.Null);
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void RepoDiscoveryPrefersTheSameNameSuffix()
	{
		// Repo folders can sit next to each other as <Repo> and <Repo>_<suffix>; a stack set up from a suffixed
		// one must use the siblings with the same suffix, not the plain-named repos also in the parent folder.
		var dir = Directory.CreateTempSubdirectory("beam-repo-discovery");
		try
		{
			string Make(string name) => Directory.CreateDirectory(Path.Combine(dir.FullName, name)).FullName;
			Make(LocalStackCommand.ApiRepoName);
			Make(LocalStackCommand.PortalRepoName);
			var scala = Make(LocalStackCommand.ScalaRepoName);
			var api = Make(LocalStackCommand.ApiRepoName + "_feat");
			var portal = Make(LocalStackCommand.PortalRepoName + "_feat");
			var nested = Directory.CreateDirectory(Path.Combine(portal, "bundles")).FullName;

			Assert.That(LocalStackCommand.FindRepoSuffix(nested), Is.EqualTo("feat"));
			Assert.That(LocalStackCommand.FindRepoDir(portal, LocalStackCommand.PortalRepoName), Is.EqualTo(portal));
			Assert.That(LocalStackCommand.FindRepoDir(nested, LocalStackCommand.PortalRepoName), Is.EqualTo(portal));
			Assert.That(LocalStackCommand.FindRepoDir(nested, LocalStackCommand.ApiRepoName), Is.EqualTo(api));
			// No BeamableBackend_feat: the plain-named repo is used.
			Assert.That(LocalStackCommand.FindRepoDir(nested, LocalStackCommand.ScalaRepoName), Is.EqualTo(scala));
			// A plain-named repo has no suffix.
			Assert.That(LocalStackCommand.FindRepoSuffix(Path.Combine(dir.FullName, LocalStackCommand.ApiRepoName)), Is.Null);
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void InitTargetsADotBeamableFolderWhenThereIsNoWorkspace()
	{
		// A loose local-stack.json in the current directory is not a workspace as far as the rest of the CLI is
		// concerned, so `init` targets `.beamable/` and creates it.
		var expected = Path.GetFullPath(Path.Combine(
			Directory.GetCurrentDirectory(), ".beamable", "local", "stack-settings.json"));

		Assert.That(LocalStackCommand.ResolveManifestPathForInit(null, null), Is.EqualTo(expected));
		// An explicit --config still wins outright.
		var custom = Path.Combine(Root, "custom", "stack.json");
		Assert.That(LocalStackCommand.ResolveManifestPathForInit(null, custom), Is.EqualTo(Path.GetFullPath(custom)));
	}

	[Test]
	public void EnsureManifestDirectoryCreatesOnlyWhenAllowed()
	{
		var dir = Directory.CreateTempSubdirectory("beam-manifest-dir");
		try
		{
			var manifest = Path.Combine(dir.FullName, ".beamable", "local-stack.json");

			// Declined: nothing is created, and the caller can report why.
			Assert.That(LocalStackCommand.EnsureManifestDirectory(manifest, _ => false, out var declined), Is.False);
			Assert.That(Directory.Exists(declined), Is.False);

			// Allowed: created.
			Assert.That(LocalStackCommand.EnsureManifestDirectory(manifest, _ => true, out var created), Is.True);
			Assert.That(Directory.Exists(created), Is.True);

			// Already there: the confirm callback must not fire again for an existing workspace.
			var asked = false;
			Assert.That(LocalStackCommand.EnsureManifestDirectory(manifest, _ => { asked = true; return false; }, out _), Is.True);
			Assert.That(asked, Is.False);
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void TheTemplateCarriesTheToolchainInitAdopted()
	{
		// init no longer PROMPTS for a Java home — `setup` installs it and init adopts what it recorded. So the
		// toolchain has to survive from Options into the written manifest, javaHome included.
		var toolchain = new LocalStackToolchain
		{
			dir = Path.Combine(Root, "tc"),
			java = Path.Combine(Root, "tc", "jdk8", "home"),
			maven = Path.Combine(Root, "tc", "maven"),
		};

		var config = LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			toolchain = toolchain,
			javaHome = toolchain.java,
		});

		Assert.That(config.toolchain, Is.SameAs(toolchain));
		Assert.That(config.javaHome, Is.EqualTo(toolchain.java));
	}

	[Test]
	public void AnUnreadableToolchainManifestDegradesToEmpty()
	{
		// A corrupt toolchain.json must not brick setup: the tools on disk are re-probed anyway and the file is
		// rewritten at the end of the run.
		var file = Path.GetTempFileName();
		try
		{
			File.WriteAllText(file, "{ this is not json");
			var manifest = ToolchainService.LoadManifest(file);

			Assert.That(manifest, Is.Not.Null);
			Assert.That(manifest.tools, Is.Empty);
		}
		finally
		{
			File.Delete(file);
		}
	}

	// ---- Path variables and custom variables -------------------------------------------------------------

	[Test]
	public void PathVariablesResolveAgainstTheWorkspaceRoot()
	{
		// Repo folders are stored relative to the workspace, so the same settings work wherever the checkouts live.
		var root = Path.Combine(Root, "ws", "agentic-portal");
		var config = new LocalStackConfig
		{
			workspaceRoot = root,
			paths = new Dictionary<string, string> { [LocalStackConfigIO.ApiPathVar] = "../BeamableAPI" },
		};

		var expected = Path.GetFullPath(Path.Combine(root, "../BeamableAPI")) + "/BeamableGateway";
		Assert.That(LocalStackConfigIO.Substitute("${BeamAPIPath}/BeamableGateway", config), Is.EqualTo(expected));
	}

	[Test]
	public void CustomVariablesNestAndUseBuiltInTokens()
	{
		var root = Path.Combine(Root, "ws", "agentic-portal");
		var config = new LocalStackConfig
		{
			workspaceRoot = root,
			paths = new Dictionary<string, string> { [LocalStackConfigIO.PortalPathVar] = "." },
			variables = new Dictionary<string, string>
			{
				["FixturesPath"] = "${PortalPath}/fixtures",
				["SeedUrl"] = "${host}/seed",
				["SeedArgs"] = "--from ${FixturesPath} --to ${SeedUrl}",
			},
		};

		var portal = Path.GetFullPath(root);
		Assert.That(LocalStackConfigIO.Substitute("${SeedArgs}", config),
			Is.EqualTo($"--from {portal}/fixtures --to http://localhost:8080/seed"));
	}

	[Test]
	public void UnknownVariablesAreLeftAsWrittenAndReported()
	{
		// An unknown name may be intended, so it is never an error, but `validate` lists it. Shell steps are not
		// scanned: a script can use ${NAME} for its own variables.
		var config = new LocalStackConfig
		{
			steps = new List<LocalStackStep>
			{
				new LocalStackStep { name = "seed", command = "node", arguments = "seed.js ${SeedOrg}" },
				new LocalStackStep { name = "script", shell = true, arguments = "echo ${HOME}" },
			},
		};

		Assert.That(LocalStackConfigIO.Substitute("seed.js ${SeedOrg}", config), Is.EqualTo("seed.js ${SeedOrg}"));
		Assert.That(LocalStackConfigIO.FindUnknownVariables(config),
			Is.EqualTo(new[] { ("step 'seed'", "SeedOrg") }));
	}

	[Test]
	public void AVariableCycleIsReportedAndCannotBeSubstituted()
	{
		var config = new LocalStackConfig
		{
			variables = new Dictionary<string, string> { ["A"] = "${B}/x", ["B"] = "${A}/y" },
		};

		var cycles = LocalStackConfigIO.FindVariableCycles(config);
		Assert.That(cycles, Has.Count.EqualTo(1));
		Assert.That(cycles[0], Does.Contain("${A}").And.Contain("${B}"));
		Assert.Throws<VariableCycleException>(() => LocalStackConfigIO.Substitute("${A}", config));
	}

	// ---- Saving and converting manifests --------------------------------------------------------------------

	[Test]
	public void SaveStoresEachRepoFolderOnceAsAPathVariable()
	{
		var root = Directory.CreateTempSubdirectory("beam-path-vars").FullName;
		try
		{
			var portal = Path.Combine(root, "agentic-portal");
			var api = Path.Combine(root, "BeamableAPI");
			var product = Path.Combine(root, "BeamableProduct");
			var registry = Path.Combine(product, "portal-localdev");
			var settings = Path.Combine(portal, ".beamable", "local", "stack-settings.json");

			var config = new LocalStackConfig
			{
				repos = new LocalStackRepos { portalDir = portal, apiDir = api, productDir = product, webRegistryDir = registry },
				steps = new List<LocalStackStep>
				{
					new LocalStackStep { name = "c# gateway", workingDirectory = Path.Combine(api, "BeamableGateway") },
					new LocalStackStep { name = "docker: web registry", workingDirectory = registry.Replace('\\', '/') },
					// A sibling whose name starts with a repo's name is a different folder and must stay as written.
					new LocalStackStep { name = "other", arguments = $"--dir \"{api}_payments\"" },
				},
			};

			LocalStackConfigIO.Save(settings, config);
			var json = File.ReadAllText(settings);
			var saved = LocalStackConfigIO.Load(settings);

			Assert.That(json, Does.Not.Contain("\"repos\""));
			Assert.That(saved.paths[LocalStackConfigIO.PortalPathVar], Is.EqualTo("."));
			Assert.That(saved.paths[LocalStackConfigIO.ApiPathVar], Is.EqualTo("../BeamableAPI"));
			Assert.That(saved.paths[LocalStackConfigIO.WebRegistryPathVar], Is.EqualTo("${BeamProductPath}/portal-localdev"));

			Assert.That(saved.steps[0].workingDirectory, Is.EqualTo("${BeamAPIPath}" + Path.DirectorySeparatorChar + "BeamableGateway"));
			Assert.That(saved.steps[1].workingDirectory, Is.EqualTo("${WebRegistryPath}"));
			Assert.That(saved.steps[2].arguments, Does.Contain(api + "_payments"));

			// Loading gives the in-memory repos view back, as absolute folders.
			Assert.That(saved.repos.apiDir, Is.EqualTo(api));
			Assert.That(saved.repos.webRegistryDir, Is.EqualTo(registry));
			Assert.That(LocalStackConfigIO.Substitute(saved.steps[0].workingDirectory, saved),
				Is.EqualTo(Path.Combine(api, "BeamableGateway")));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestCase("build: c# gateway", "Build C# gateway", new[] { "build", "csharp" })]
	[TestCase("scala: auth", "Scala service auth", new[] { "scala" })]
	[TestCase("docker: api deps + caddy", "Docker api deps + caddy", new[] { "docker" })]
	[TestCase("docker: web registry", "Docker web registry", new[] { "web-registry" })]
	[TestCase("build: web packages", "Build web packages", new[] { "build", "web-registry" })]
	[TestCase("build: scala", "Build Scala", new[] { "build", "scala" })]
	[TestCase("build: portal deps", "Build portal deps", new[] { "build", "portal" })]
	[TestCase("portal frontend", "Portal frontend", new[] { "portal" })]
	[TestCase("portal extension: campaigns", "Portal extension campaigns", new[] { "extensions" })]
	[TestCase("microservice: CampaignService", "Microservice CampaignService", new[] { "extensions" })]
	public void StepsGetALabelAndTagsFromTheirName(string name, string label, string[] tags)
	{
		Assert.That(LocalStackConfigIO.InferLabel(name), Is.EqualTo(label));
		Assert.That(LocalStackConfigIO.InferTags(name), Is.EqualTo(tags));
	}

	// ---- CLI version pins ----------------------------------------------------------------------------------------

	private const string Running = "0.0.123.3001";

	private static string ToolManifest(string version) =>
		"{\n  \"version\": 1,\n  \"isRoot\": true,\n  \"tools\": {\n    \"beamable.tools\": {\n      \"version\": \"" + version
		+ "\",\n      \"commands\": [\"beam\"],\n      \"rollForward\": false\n    }\n  }\n}";

	private static string WritePortal(string root, string version)
	{
		var portal = Path.Combine(root, "agentic-portal");
		Directory.CreateDirectory(Path.Combine(portal, ".config"));
		Directory.CreateDirectory(Path.Combine(portal, "bundles", ".config"));
		Directory.CreateDirectory(Path.Combine(portal, ".beamable"));
		Directory.CreateDirectory(Path.Combine(portal, "bundles", "services", "Svc"));
		Directory.CreateDirectory(Path.Combine(portal, "node_modules", "pkg", ".config"));

		File.WriteAllText(Path.Combine(portal, ".config", "dotnet-tools.json"), ToolManifest(version));
		File.WriteAllText(Path.Combine(portal, "bundles", ".config", "dotnet-tools.json"), ToolManifest(version));
		File.WriteAllText(Path.Combine(portal, ".beamable", "config.beam.json"),
			"{\n  \"cliVersion\": \"" + version + "\",\n  \"keep\": \"me\"\n}");
		File.WriteAllText(Path.Combine(portal, "bundles", "services", "Svc", "Svc.csproj"),
			"<Project><ItemGroup><PackageReference Include=\"Beamable.Microservice.Runtime\" Version=\"$(BeamableVersion)\" /></ItemGroup></Project>");
		// Dependencies are never searched.
		File.WriteAllText(Path.Combine(portal, "node_modules", "pkg", ".config", "dotnet-tools.json"), ToolManifest(version));
		return portal;
	}

	[Test]
	public void LocalBuildPinsMoveToTheRunningLocalBuild()
	{
		var root = Directory.CreateTempSubdirectory("beam-cli-pins").FullName;
		try
		{
			var portal = WritePortal(root, "0.0.123.5");
			var calls = new List<string>();
			var result = LocalStackCliVersions.Check(portal, Running, fix: true, run: (file, arguments, dir) =>
			{
				calls.Add($"{file} {arguments} @ {Path.GetFileName(dir)}");
				return new cli.Utils.StartProcessResult { exit = 0 };
			});

			Assert.That(result.pins, Has.Count.EqualTo(3));
			Assert.That(result.pins.Select(p => p.status), Is.All.EqualTo("repinned"));
			Assert.That(File.ReadAllText(Path.Combine(portal, "bundles", ".config", "dotnet-tools.json")), Does.Contain(Running));
			var config = File.ReadAllText(Path.Combine(portal, ".beamable", "config.beam.json"));
			Assert.That(config, Does.Contain(Running).And.Contain("\"keep\": \"me\""));

			Assert.That(calls, Does.Contain("dotnet tool restore @ agentic-portal"));
			Assert.That(calls, Does.Contain("dotnet tool restore @ bundles"));
			Assert.That(calls.Count(c => c.Contains("Svc.csproj")), Is.EqualTo(1));
			Assert.That(result.restored, Has.Count.EqualTo(1));
			Assert.That(File.ReadAllText(Path.Combine(portal, "node_modules", "pkg", ".config", "dotnet-tools.json")),
				Does.Contain("0.0.123.5"), "node_modules is never touched");
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[Test]
	public void APublishedPinIsOnlyReported()
	{
		var root = Directory.CreateTempSubdirectory("beam-cli-pins").FullName;
		try
		{
			var portal = WritePortal(root, "5.2.0");
			var calls = 0;
			var result = LocalStackCliVersions.Check(portal, Running, fix: true, run: (_, _, _) =>
			{
				calls++;
				return new cli.Utils.StartProcessResult { exit = 0 };
			});

			Assert.That(result.pins.Select(p => p.status), Is.All.EqualTo("mismatch"));
			Assert.That(File.ReadAllText(Path.Combine(portal, ".config", "dotnet-tools.json")), Does.Contain("5.2.0"));
			Assert.That(calls, Is.Zero);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[Test]
	public void WithoutFixDriftIsReportedButNothingChanges()
	{
		var root = Directory.CreateTempSubdirectory("beam-cli-pins").FullName;
		try
		{
			var portal = WritePortal(root, "0.0.123.5");
			var result = LocalStackCliVersions.Check(portal, Running, fix: false);

			Assert.That(result.pins.Select(p => p.status), Is.All.EqualTo("mismatch"));
			Assert.That(File.ReadAllText(Path.Combine(portal, ".config", "dotnet-tools.json")), Does.Contain("0.0.123.5"));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[Test]
	public void NoPortalMeansNoPins()
	{
		Assert.That(LocalStackCliVersions.Check(null, Running, fix: true).pins, Is.Empty);
		Assert.That(LocalStackCliVersions.Check(Path.Combine(Root, "does-not-exist"), Running, fix: true).pins, Is.Empty);
	}

	// ----------------------------------------------------------------------------------
	// Stacks: the layers, extends, variants and how a name resolves to steps
	// ----------------------------------------------------------------------------------

	/// <summary>A catalogue as `beam local setup` writes one, with one extension and one service group.</summary>
	private static LocalStackConfig StackCatalogue() =>
		LocalStackTemplate.Create(new LocalStackTemplate.Options
		{
			apiDir = Path.Combine(Root, "BeamableAPI"),
			scalaDir = Path.Combine(Root, "BeamableBackend"),
			portalDir = Path.Combine(Root, "portal"),
			includeWebRegistry = true,
			webRegistryDir = Path.Combine(Root, "BeamableProduct", "portal-localdev"),
			extensions = new List<string> { "my-ext" },
			groups = new List<string> { "player-engagement" },
		});

	private static List<string> StepNames(LocalStackResolvedStack resolved) => resolved.steps.Select(s => s.name).ToList();

	private static LocalStackStackDef Def(string description = null, string extends = null, string[] include = null,
		string[] exclude = null, string[] extensions = null) =>
		new LocalStackStackDef
		{
			description = description,
			extends = extends,
			include = include?.ToList(),
			exclude = exclude?.ToList(),
			extensions = extensions?.ToList(),
		};

	private static Dictionary<string, LocalStackVariantDef> Variant(string name, string[] before = null, string[] after = null,
		string[] remove = null) =>
		new Dictionary<string, LocalStackVariantDef>
		{
			[name] = new LocalStackVariantDef
			{
				description = name, before = before?.ToList(), after = after?.ToList(), remove = remove?.ToList()
			}
		};

	[Test]
	public void BuiltInStacksSelectByTag()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var config = StackCatalogue();

		var backend = StepNames(stacks.Resolve("backend", config));
		Assert.That(backend, Does.Contain("c# gateway").And.Contain("scala: auth").And.Contain("docker: api deps + caddy"));
		Assert.That(backend, Does.Not.Contain("portal frontend"));
		Assert.That(backend, Does.Not.Contain(LocalStackTemplate.WebRegistryStepName), "web steps only run when a stack asks for them");

		var full = StepNames(stacks.Resolve("full", config));
		Assert.That(full, Does.Contain("portal frontend").And.Contain("portal extension: my-ext").And.Contain("group: player-engagement"));

		// The catalogue's order is kept: readiness gates depend on it.
		var catalogueOrder = config.steps.Select(s => s.name).Where(full.Contains).ToList();
		Assert.That(full, Is.EqualTo(catalogueOrder));
	}

	[Test]
	public void ScalaCoreIsTheMinimalLoginSet()
	{
		var names = StepNames(LocalStackStacks.BuiltIn().Resolve("csharp-portal", StackCatalogue()));

		Assert.That(names, Does.Contain("scala: auth").And.Contain("scala: account").And.Contain("scala: gateway")
			.And.Contain("scala: dbflake").And.Contain("build: scala"));
		Assert.That(names, Does.Not.Contain("scala: stats"));
		Assert.That(names, Does.Contain("portal frontend"));
	}

	[Test]
	public void HigherLayerFieldsWinAndVariantsMerge()
	{
		var stacks = LocalStackStacks.BuiltIn();
		stacks.Add("team", LocalStackLayer.Shared, "shared.json", new LocalStackStackDef
		{
			description = "Team stack", include = new List<string> { "docker" },
			variants = Variant("with-portal", after: new[] { "portal" })
		});
		// A personal entry changes the selection and adds its own variant, without copying the shared one.
		stacks.Add("team", LocalStackLayer.Local, "mine.json", new LocalStackStackDef
		{
			include = new List<string> { "csharp" },
			variants = Variant("mine", after: new[] { "scala: auth" })
		});

		var config = StackCatalogue();
		var team = stacks.Resolve("team", config);
		Assert.That(team.description, Is.EqualTo("Team stack"), "an unset field falls through to the lower layer");
		Assert.That(StepNames(team), Does.Contain("c# gateway").And.Not.Contain("docker: api deps + caddy"),
			"the local include replaces the shared one");

		Assert.That(StepNames(stacks.Resolve("team:with-portal", config)), Does.Contain("portal frontend"));
		Assert.That(StepNames(stacks.Resolve("team:mine", config)), Does.Contain("scala: auth"));

		var listed = stacks.List().Single(s => s.name == "team");
		Assert.That(listed.layer, Is.EqualTo(LocalStackLayer.Shared));
		Assert.That(listed.changedBy, Is.EqualTo(new[] { LocalStackLayer.Local }));
		Assert.That(listed.variants.Single(v => v.name == "mine").layer, Is.EqualTo(LocalStackLayer.Local));
	}

	[Test]
	public void VariantsChainLeftToRight()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var def = Def("Chain", include: new[] { "docker", "csharp" });
		def.variants = new Dictionary<string, LocalStackVariantDef>
		{
			["a"] = new LocalStackVariantDef { before = new List<string> { "scala: redis" }, after = new List<string> { "portal frontend" } },
			["b"] = new LocalStackVariantDef
			{
				before = new List<string> { "scala: dbflake", "scala: redis" },
				after = new List<string> { "scala: auth" },
				remove = new List<string> { "c# analytics loader" }
			},
		};
		stacks.Add("chain", LocalStackLayer.Local, "chain.json", def);

		var names = StepNames(stacks.Resolve("chain:a:b", StackCatalogue()));

		// a.before, b.before, the stack's own steps, a.after, b.after. scala: redis is listed twice and runs once,
		// at its first position.
		Assert.That(names.Take(2), Is.EqualTo(new[] { "scala: redis", "scala: dbflake" }));
		Assert.That(names.TakeLast(2), Is.EqualTo(new[] { "portal frontend", "scala: auth" }));
		Assert.That(names.Count(n => n == "scala: redis"), Is.EqualTo(1));
		Assert.That(names, Does.Not.Contain("c# analytics loader"), "b's remove applies to the stack's own steps");
		Assert.That(names, Does.Contain("c# gateway"));
	}

	[Test]
	public void RemovePlusBeforeOverridesAStep()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var def = Def("Debug gateway", extends: "backend");
		def.steps = new Dictionary<string, LocalStackStep>
		{
			["c# gateway (debug)"] = new LocalStackStep { command = "dotnet", arguments = "run", workingDirectory = "${BeamAPIPath}" }
		};
		def.variants = Variant("debug", before: new[] { "c# gateway (debug)" }, remove: new[] { "c# gateway" });
		stacks.Add("gw", LocalStackLayer.Local, "gw.json", def);

		var config = StackCatalogue();
		var names = StepNames(stacks.Resolve("gw:debug", config));

		Assert.That(names, Does.Not.Contain("c# gateway"));
		Assert.That(names[0], Is.EqualTo("c# gateway (debug)"));

		// The custom step only runs when something asks for it.
		Assert.That(StepNames(stacks.Resolve("gw", config)), Does.Not.Contain("c# gateway (debug)"));
	}

	[Test]
	public void ExtendsAddsAndExcludesAndExtensionsNarrow()
	{
		var stacks = LocalStackStacks.BuiltIn();
		stacks.Add("campaigns", LocalStackLayer.Shared, "campaigns.json",
			Def("Campaigns", extends: "full", exclude: new[] { "c# analytics loader" }, extensions: new[] { "player-engagement" }));

		var names = StepNames(stacks.Resolve("campaigns", StackCatalogue()));

		Assert.That(names, Does.Contain("c# gateway").And.Contain("portal frontend"), "inherited from full");
		Assert.That(names, Does.Not.Contain("c# analytics loader"));
		Assert.That(names, Does.Contain("group: player-engagement"));
		Assert.That(names, Does.Not.Contain("portal extension: my-ext"), "extensions narrows the extension steps");
	}

	[Test]
	public void BuildStepsAreRequestedOnlyWhenAskedFor()
	{
		var stacks = LocalStackStacks.BuiltIn();
		stacks.Add("named", LocalStackLayer.Local, "named.json", Def(include: new[] { "build: scala", "scala: auth" }));
		var config = StackCatalogue();

		var named = stacks.Resolve("named", config);
		Assert.That(named.requested.Select(s => s.name), Is.EquivalentTo(new[] { "build: scala", "scala: auth" }));

		// Selected through the `scala` tag, a build step is only there to run when its output is missing.
		var backend = stacks.Resolve("backend", config);
		Assert.That(backend.requested, Is.Empty);
		Assert.That(stacks.Resolve("backend:build", config).forceBuild, Is.True);
	}

	[Test]
	public void ProblemsAreReportedBeforeAnythingRuns()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var config = StackCatalogue();
		stacks.Add("typo", LocalStackLayer.Local, "typo.json", Def(include: new[] { "c# gatewy" }));
		stacks.Add("loop-a", LocalStackLayer.Local, "loop.json", Def(extends: "loop-b"));
		stacks.Add("loop-b", LocalStackLayer.Local, "loop.json", Def(extends: "loop-a"));

		Assert.That(() => stacks.Resolve("typo", config),
			Throws.InstanceOf<LocalStackStackException>().With.Message.Contains("no step or tag 'c# gatewy'"));
		Assert.That(() => stacks.Resolve("nope", config),
			Throws.InstanceOf<LocalStackStackException>().With.Message.Contains("No stack named 'nope'"));
		Assert.That(() => stacks.Resolve("full:nope", config),
			Throws.InstanceOf<LocalStackStackException>().With.Message.Contains("no variant 'nope'"));
		Assert.That(() => stacks.Resolve("full::web", config),
			Throws.InstanceOf<LocalStackStackException>().With.Message.Contains("not a stack name"));
		Assert.That(() => stacks.Resolve("loop-a", config),
			Throws.InstanceOf<LocalStackStackException>().With.Message.Contains("loop"));

		// A tag no step has yet, such as the log forwarding `:no-logs` removes, is not an error.
		Assert.That(StepNames(stacks.Resolve("full:no-logs", config)), Is.EqualTo(StepNames(stacks.Resolve("full", config))));
	}

	[Test]
	public void OneFolderCantDefineAStackTwice()
	{
		var dir = Directory.CreateTempSubdirectory("beam-stacks");
		try
		{
			File.WriteAllText(Path.Combine(dir.FullName, "a.json"), "{ \"mine\": { \"include\": [\"docker\"] } }");
			File.WriteAllText(Path.Combine(dir.FullName, "b.json"), "{ \"mine\": { \"include\": [\"csharp\"] } }");

			var stacks = LocalStackStacks.BuiltIn();
			Assert.That(() => stacks.LoadFolder(dir.FullName, LocalStackLayer.Local),
				Throws.InstanceOf<LocalStackStackException>().With.Message.Contains("a.json").And.Message.Contains("b.json"));
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void CreateOffersWhatAVariantCanAddOrRemove()
	{
		var config = StackCatalogue();
		var running = LocalStackStacks.BuiltIn().Resolve("backend", config).steps;

		var addable = LocalStackCreateCommand.AddableSteps(config, running).Select(s => s.name).ToList();
		Assert.That(addable, Does.Contain("portal frontend"));
		Assert.That(addable, Does.Not.Contain("c# gateway"), "the stack already runs it");

		Assert.That(LocalStackCreateCommand.RemovableSteps(running).Select(s => s.name), Is.EqualTo(running.Select(s => s.name)));
	}

	[Test]
	public void CreateGroupsStepsByDomain()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var config = StackCatalogue();
		string Domain(string name) => LocalStackCreateCommand.DomainOf(stacks, config.steps.Single(s => s.name == name));

		Assert.That(Domain("docker: api deps + caddy"), Is.EqualTo("docker"));
		Assert.That(Domain("build: c# gateway"), Is.EqualTo("csharp"), "a build step sits with what it builds");
		Assert.That(Domain("scala: auth"), Is.EqualTo("scala"));
		Assert.That(Domain("portal frontend"), Is.EqualTo("portal"));
		Assert.That(Domain(LocalStackTemplate.WebPublishStepName), Is.EqualTo("web-registry"), "not with the other builds");
		Assert.That(Domain("portal extension: my-ext"), Is.EqualTo("extensions"));
		Assert.That(Domain("group: player-engagement"), Is.EqualTo("extensions"));

		// A custom step sits with the others someone added, whatever else it is tagged.
		var custom = new LocalStackStep { name = "seed: data", custom = true, tags = new List<string> { "custom", "portal" } };
		Assert.That(LocalStackCreateCommand.DomainOf(stacks, custom), Is.EqualTo("custom"));
	}

	[Test]
	public void CreateWritesAWholeTagAsTheTag()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var config = StackCatalogue();
		var all = config.steps;
		HashSet<string> Names(Func<LocalStackStep, bool> where) => all.Where(where).Select(s => s.name).ToHashSet();

		// Every docker step ticked is the tag; one scala step is just that step.
		var picked = LocalStackCreateCommand.CollapseToTags(stacks, all, all,
			Names(s => stacks.TagsOf(s).Contains("docker") || s.name == "scala: stats"));
		Assert.That(picked, Is.EqualTo(new[] { "docker", "scala: stats" }));

		// The whole login set, without the rest of Scala, is scala-core.
		picked = LocalStackCreateCommand.CollapseToTags(stacks, all, all, Names(s => stacks.TagsOf(s).Contains("scala-core")));
		Assert.That(picked, Is.EqualTo(new[] { "scala-core" }));

		// Offered only part of a tag (the rest is already in the stack): all of it ticked is still single steps, or
		// the tag would pull in the steps that weren't offered.
		var addable = all.Where(s => s.name != "c# gateway").ToList();
		picked = LocalStackCreateCommand.CollapseToTags(stacks, all, addable,
			addable.Where(s => stacks.TagsOf(s).Contains("csharp")).Select(s => s.name).ToHashSet());
		Assert.That(picked, Does.Not.Contain("csharp").And.Contain("build: c# gateway"));
	}

	/// <summary>A scripted wizard: each question answers from <paramref name="moves"/>, recording that it was asked.</summary>
	private static (LocalStackWizard wizard, List<string> asked) ScriptedWizard(Queue<LocalStackWizardMove> moves,
		params (string group, Func<bool> applies)[] questions)
	{
		var wizard = new LocalStackWizard();
		var asked = new List<string>();
		foreach (var (group, applies) in questions)
		{
			var name = group;
			wizard.questions.Add(new LocalStackWizard.Question
			{
				group = name, applies = applies ?? (() => true),
				ask = _ => { asked.Add(name); return moves.Dequeue(); }
			});
		}

		return (wizard, asked);
	}

	[Test]
	public void WizardBackGoesToThePreviousQuestionThatApplies()
	{
		var next = LocalStackWizardMove.Next;
		var back = LocalStackWizardMove.Back;
		var (wizard, asked) = ScriptedWizard(new Queue<LocalStackWizardMove>(new[] { next, next, back, next, next }),
			("a", null), ("skipped", () => false), ("b", null), ("c", null));

		Assert.That(wizard.Run(), Is.True);
		// a, b, then back from c to b (skipping the one that doesn't apply), b again, c.
		Assert.That(asked, Is.EqualTo(new[] { "a", "b", "c", "b", "c" }));
	}

	[Test]
	public void WizardRevisitsOneGroupAndReturnsOnBack()
	{
		var (wizard, asked) = ScriptedWizard(new Queue<LocalStackWizardMove>(new[]
			{
				LocalStackWizardMove.Next, LocalStackWizardMove.Next, // first pass
				LocalStackWizardMove.Next,                           // change b
				LocalStackWizardMove.Back                            // change a, then back out to the review
			}),
			("a", null), ("b", null));

		wizard.Run();
		Assert.That(wizard.Run(1, "b"), Is.True);
		Assert.That(wizard.Run(0, "a"), Is.False, "back from the first question of a revisited group returns to the review");
		Assert.That(asked, Is.EqualTo(new[] { "a", "b", "b", "a" }));
		Assert.That(wizard.Groups(), Is.EqualTo(new[] { "a", "b" }));
	}

	[Test]
	public void ExtendedByFindsTheStacksBuiltOnAnother()
	{
		var stacks = LocalStackStacks.BuiltIn();
		stacks.Add("mine", LocalStackLayer.Local, "mine.json", Def(extends: "backend"));

		Assert.That(stacks.ExtendedBy("backend"), Is.EqualTo(new[] { "mine" }));
		Assert.That(stacks.ExtendedBy("mine"), Is.Empty);
	}

	[Test]
	public void EveryKnownTagIsExplained()
	{
		foreach (var tag in LocalStackStacks.KnownTags)
			Assert.That(LocalStackStacks.TagDescriptions.ContainsKey(tag), Is.True, $"no description for '{tag}'");
	}

	/// <summary>`create` previews and checks a draft; the draft must never leak into the set it was checked against.</summary>
	[Test]
	public void ResolvingADraftLeavesTheStacksUnchanged()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var draft = Def("Draft", extends: "backend", exclude: new[] { "c# analytics loader" });

		var resolved = stacks.ResolveDraft("draft", LocalStackLayer.Local, "draft.json", draft, StackCatalogue());

		Assert.That(StepNames(resolved), Does.Contain("c# gateway").And.Not.Contain("c# analytics loader"));
		Assert.That(stacks.Contains("draft"), Is.False);
	}

	[Test]
	public void SharedStacksAndVariantsNeedADescription()
	{
		var stacks = LocalStackStacks.BuiltIn();

		Assert.That(() => stacks.Add("team", LocalStackLayer.Shared, "team.json", Def(include: new[] { "docker" })),
			Throws.InstanceOf<LocalStackStackException>().With.Message.Contains("needs a description"));

		// A shared entry adding to a built-in stack needs no description of its own, but its variants do.
		var undescribed = new LocalStackStackDef
		{
			variants = new Dictionary<string, LocalStackVariantDef> { ["x"] = new LocalStackVariantDef { after = new List<string> { "portal" } } }
		};
		Assert.That(() => stacks.Add("full", LocalStackLayer.Shared, "full.json", undescribed),
			Throws.InstanceOf<LocalStackStackException>().With.Message.Contains("full:x"));

		Assert.That(() => stacks.Add("mine", LocalStackLayer.Local, "mine.json", Def(include: new[] { "docker" })), Throws.Nothing);
	}
}
