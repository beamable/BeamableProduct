using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using cli.Commands.LocalStack;
using cli.Services.LocalStack;
using NUnit.Framework;
using Spectre.Console.Testing;

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
		// web: build shells out to a bare `pnpm`, so there is no token to substitute — the only thing that
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
		// Without a toolchain the tokens resolve to bare names and PATH does the rest.
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
	[TestCase("web: registry", "Web packages registry", new[] { "web-registry" })]
	[TestCase("web: build", "Web packages build", new[] { "web-registry" })]
	[TestCase("build: scala", "Build Scala", new[] { "build", "scala" })]
	[TestCase("build: portal deps", "Build portal deps", new[] { "build", "portal" })]
	[TestCase("portal frontend", "Portal frontend", new[] { "portal" })]
	[TestCase("portal extension: campaigns", "Portal extension campaigns", new[] { "extensions" })]
	[TestCase("microservice: CampaignService", "Microservice CampaignService", new[] { "microservices" })]
	[TestCase("group: player-engagement", "Service group player-engagement", new[] { "microservices", "extensions" })]
	public void StepsGetALabelAndTagsFromTheirName(string name, string label, string[] tags)
	{
		Assert.That(LocalStackConfigIO.InferLabel(name), Is.EqualTo(label));
		Assert.That(LocalStackConfigIO.InferTags(name), Is.EqualTo(tags));
	}

	/// <summary>
	/// The workspace's services found when a stack is resolved become steps: one per microservice and extension, an
	/// extension depending on the microservices it calls, no groups, and a step of that name in the settings wins.
	/// </summary>
	[Test]
	public void Found_services_become_steps_unless_the_settings_have_them()
	{
		var steps = new List<LocalStackStep> { new LocalStackStep { name = "microservice: Edited", arguments = "mine" } };
		var found = new WorkspaceServices
		{
			services = new List<string> { "Edited", "CampaignService" },
			extensions = new List<string> { "campaigns" },
			groups = new Dictionary<string, string[]> { ["player-engagement"] = new[] { "campaigns" } },
			calls = new Dictionary<string, List<string>> { ["campaigns"] = new List<string> { "CampaignService" } }
		};

		found.AddTo(steps);

		Assert.That(steps.Select(s => s.name), Is.EqualTo(new[] { "microservice: Edited", "microservice: CampaignService", "portal extension: campaigns" }));
		Assert.That(steps[0].arguments, Is.EqualTo("mine"));
		Assert.That(steps[2].dependsOn, Is.EqualTo(new[] { "microservice: CampaignService" }));

		var full = LocalStackStacks.BuiltIn().Resolve("full", new LocalStackConfig { steps = steps }).steps.Select(s => s.name);
		Assert.That(full, Does.Contain("microservice: CampaignService").And.Contain("portal extension: campaigns"));
	}

	/// <summary>
	/// The microservices found are built together first, so their starts, together, don't compile what they share at
	/// the same time; extensions start a few at a time and are up once they say so.
	/// </summary>
	[Test]
	public void Found_microservices_are_built_together_then_start_together()
	{
		var workspace = Path.Combine(Path.GetTempPath(), "local-stack-build-tests", Path.GetRandomFileName());
		string Project(string folder, string reference = null)
		{
			var path = Path.Combine(workspace, folder, folder + ".csproj");
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllText(path, "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup>"
			                        + (reference == null ? "" : $"<ProjectReference Include=\"..\\{reference}\\{reference}.csproj\" />")
			                        + "</ItemGroup></Project>");
			return path;
		}

		try
		{
			Project("Storage");
			var found = new WorkspaceServices
			{
				workspace = workspace,
				services = new List<string> { "A", "B" },
				extensions = new List<string> { "ext" },
				projects = new Dictionary<string, string> { ["A"] = Project("A", "Storage"), ["B"] = Project("B", "Storage") }
			};
			var steps = new List<LocalStackStep>();
			found.AddTo(steps);

			Assert.That(steps.Select(s => s.name), Is.EqualTo(new[] { "build: microservices", "microservice: A", "microservice: B", "portal extension: ext" }));
			var build = steps[0];
			Assert.That(build.build && build.waitForExit, Is.True);
			Assert.That(LocalStackRunCommand.WaitsForItsPlace(build), Is.True, "it generates clients from the backend, so not before it is up");
			Assert.That(build.inputs.Select(Path.GetFileName), Is.EqualTo(new[] { "A", "B", "Storage" }), "each project's folder and the shared one");
			var solution = File.ReadAllText(Path.Combine(workspace, ".beamable", "temp", "local-stack", "microservices.slnx"));
			Assert.That(solution, Does.Contain("A.csproj").And.Contain("B.csproj").And.Not.Contain("Storage.csproj"));

			Assert.That(steps[1].group, Is.EqualTo(LocalStackTemplate.MicroservicesGroup));
			Assert.That(steps[3].group, Is.EqualTo(LocalStackTemplate.ExtensionsGroup));
			Assert.That(steps[3].readyWhenLogContains, Does.StartWith("Portal extension started successfully"));

			// The build comes with the microservices in a stack, and it's what restarting one of them would build.
			var config = new LocalStackConfig { steps = steps };
			var stacks = LocalStackStacks.BuiltIn();
			Assert.That(stacks.Resolve("full", config).steps.Select(s => s.name), Does.Contain("build: microservices"));
			Assert.That(LocalStackControl.BuildsFor(steps[1], steps, stacks.TagsOf).Select(s => s.name), Is.EqualTo(new[] { "build: microservices" }));
			Assert.That(LocalStackControl.BuildsFor(steps[3], steps, stacks.TagsOf), Is.Empty);
		}
		finally
		{
			Directory.Delete(workspace, true);
		}
	}

	/// <summary>Without microservice projects (outside a workspace, or none found) there's nothing to build.</summary>
	[Test]
	public void No_microservices_build_without_projects()
	{
		var steps = new List<LocalStackStep>();
		new WorkspaceServices { services = new List<string> { "A" } }.AddTo(steps);
		Assert.That(steps.Select(s => s.name), Is.EqualTo(new[] { "microservice: A" }));
	}

	/// <summary>
	/// A group with a start limit never has more steps coming up than the limit, starts the next one as soon as one is
	/// up (or failed), and an item that started nothing takes no slot.
	/// </summary>
	[Test]
	public async Task Pooled_starts_keep_to_the_limit()
	{
		var gates = Enumerable.Range(0, 6).Select(_ => new TaskCompletionSource()).ToList();
		var inFlight = 0;
		var most = 0;
		var started = new List<int>();
		var run = LocalStackRunCommand.RunPooled(Enumerable.Range(0, 7).ToList(), 2, _ => TimeSpan.Zero, i =>
		{
			if (i == 3) return Task.FromResult<Task>(null); // already running: nothing started
			started.Add(i);
			most = Math.Max(most, ++inFlight);
			var gate = gates[i < 3 ? i : i - 1].Task;
			return Task.FromResult<Task>(gate.ContinueWith(_ => inFlight--));
		}, CancellationToken.None);

		Assert.That(started, Is.EqualTo(new[] { 0, 1 }), "two at a time");
		gates[1].SetResult();
		await Task.Delay(50);
		Assert.That(started, Is.EqualTo(new[] { 0, 1, 2 }), "one up, the next starts");
		gates[2].SetException(new Exception("failed")); // a failure frees its slot too
		await Task.Delay(50);
		Assert.That(started, Is.EqualTo(new[] { 0, 1, 2, 4 }), "3 started nothing and took no slot");
		foreach (var gate in gates) gate.TrySetResult();
		await run;

		Assert.That(started, Is.EqualTo(new[] { 0, 1, 2, 4, 5, 6 }));
		Assert.That(most, Is.LessThanOrEqualTo(2));
	}

	/// <summary>
	/// Saving the run-state while another process reads it (`beam local ps`, opened the old way, without letting a
	/// writer replace it) waits for the reader instead of failing: a failed save ended the run and tore the stack down.
	/// </summary>
	[Test]
	public async Task Run_state_saves_wait_for_a_reader()
	{
		var path = Path.Combine(Path.GetTempPath(), "local-stack-io-tests", Path.GetRandomFileName(), "local-stack.run.json");
		LocalStackRunStateIO.Save(path, new LocalStackRunState());
		try
		{
			var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			var release = Task.Delay(300).ContinueWith(_ => reader.Dispose());
			LocalStackRunStateIO.Save(path, new LocalStackRunState { steps = { new LocalStackRunEntry { name = "saved" } } });
			await release;

			Assert.That(LocalStackRunStateIO.Load(path).steps.Single().name, Is.EqualTo("saved"));
			Assert.That(Directory.GetFiles(Path.GetDirectoryName(path)!), Has.Length.EqualTo(1), "no temp file left behind");
		}
		finally
		{
			Directory.Delete(Path.GetDirectoryName(path)!, true);
		}
	}

	/// <summary>A restart a newer one replaces stops starting steps as soon as it is cancelled, and says it didn't finish.</summary>
	[Test]
	public async Task Pooled_starts_stop_when_cancelled()
	{
		var never = new TaskCompletionSource();
		var started = new List<int>();
		using var cancel = new CancellationTokenSource();
		var run = LocalStackRunCommand.RunPooled(Enumerable.Range(0, 5).ToList(), 2, _ => TimeSpan.Zero, i =>
		{
			started.Add(i);
			return Task.FromResult<Task>(never.Task.WaitAsync(cancel.Token).ContinueWith(_ => { }));
		}, cancel.Token);

		Assert.That(started, Is.EqualTo(new[] { 0, 1 }), "waiting for a slot");
		cancel.Cancel();
		Assert.ThrowsAsync<TaskCanceledException>(async () => await run);
		Assert.That(started, Is.EqualTo(new[] { 0, 1 }), "nothing more started");
	}

	/// <summary>The built-in stacks that run this workspace's services name both kinds, so microservices still run.</summary>
	[Test]
	public void Full_runs_the_workspace_microservices_and_extensions()
	{
		var config = StackCatalogue();
		config.steps.Add(LocalStackTemplate.MicroserviceStep("CampaignService"));
		var names = LocalStackStacks.BuiltIn().Resolve("full", config).steps.Select(s => s.name).ToList();

		Assert.That(names, Does.Contain("microservice: CampaignService").And.Contain("portal extension: my-ext").And.Contain("group: player-engagement"));
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

	/// <summary>csharp-portal runs the Scala login set and the Scala services the C# hosts call, and no other.</summary>
	[Test]
	public void CsharpPortalRunsTheScalaTheCsharpHostsNeed()
	{
		var names = StepNames(LocalStackStacks.BuiltIn().Resolve("csharp-portal", StackCatalogue()));

		Assert.That(names, Does.Contain("scala: auth").And.Contain("scala: account").And.Contain("scala: gateway")
			.And.Contain("scala: dbflake").And.Contain("build: scala"));
		Assert.That(names, Does.Contain("scala: stats").And.Contain("scala: content").And.Contain("scala: session"), "the C# gateway calls them");
		Assert.That(names, Does.Not.Contain("scala: leaderboards"));
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

		// `:no-logs` leaves out the log forwarding, and nothing else.
		Assert.That(StepNames(stacks.Resolve("full:no-logs", config)),
			Is.EqualTo(StepNames(stacks.Resolve("full", config)).Where(n => n != LocalStackTemplate.LogsForwardStepName)));
	}

	/// <summary>
	/// The log forwarding runs after the docker dependencies (the collector), in the local stacks, and doesn't log in;
	/// each line goes out as an OTLP record with the stack, workspace, step and kind, at the level it names.
	/// </summary>
	[Test]
	public void Logs_are_forwarded_with_their_step()
	{
		var config = StackCatalogue();
		var names = config.steps.Select(s => s.name).ToList();
		Assert.That(names.IndexOf(LocalStackTemplate.LogsForwardStepName), Is.EqualTo(names.IndexOf("docker: api deps + caddy") + 1));
		var forward = config.steps.Single(s => s.name == LocalStackTemplate.LogsForwardStepName);
		Assert.That((forward.beam, forward.arguments, forward.tags.Single()), Is.EqualTo((true, "local logs --forward http://localhost:4318", "observability")));
		Assert.That(StepNames(LocalStackStacks.BuiltIn().Resolve("csharp-portal", config)), Does.Contain(LocalStackTemplate.LogsForwardStepName));

		var payload = LogForwarder.Payload(new[] { ("scala: auth", "2026 ERROR boom"), ("scala: auth", "started"), ("c# gateway", "WARN slow") },
			"full", "agentic-portal", new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc));
		var resources = payload["resourceLogs"]!;
		Assert.That(resources.Count(), Is.EqualTo(2), "one resource per step");
		var auth = resources[0]!;
		string Attr(string key) => auth["resource"]!["attributes"]!.Single(a => (string)a["key"] == key)["value"]!["stringValue"]!.ToString();
		Assert.That((Attr("beam.step"), Attr("beam.stack"), Attr("beam.workspace"), Attr("beam.kind")), Is.EqualTo(("scala: auth", "full", "agentic-portal", "scala")));
		var records = auth["scopeLogs"]![0]!["logRecords"]!;
		Assert.That(records.Select(r => (string)r["severityText"]), Is.EqualTo(new[] { "ERROR", "INFO" }));
		Assert.That((string)records[0]!["timeUnixNano"], Is.EqualTo("1793016000000000000"));
		Assert.That(LogForwarder.SeverityOf("[main] WARNING disk"), Is.EqualTo("WARN"));
		Assert.That((LogForwarder.SeverityOf("TRACE tick"), LogForwarder.SeverityOf("[VERBOSE] x"), LogForwarder.SeverityOf("DEBUG y")), Is.EqualTo(("TRACE", "TRACE", "DEBUG")));
		Assert.That(LogForwarder.SeverityOf("{ \"t\": \"2026\", \"l\": \"Error\", \"m\": \"0 warnings\" }"), Is.EqualTo("ERROR"), "a JSON line's own level");
		Assert.That(LogForwarder.SeverityOf("  \"l\": \"Information\","), Is.EqualTo("INFO"));

		// Against the local backend, the workspace's services send their own telemetry to the same collector.
		var env = LocalStackRunCommand.WithLocalTelemetry(new Dictionary<string, string> { ["BEAM_OTEL_EXPORTER_OTLP_PROTOCOL"] = "Grpc" });
		Assert.That((env["BEAM_OTEL_EXPORTER_OTLP_ENDPOINT"], env["BEAM_OTEL_EXPORTER_OTLP_PROTOCOL"]), Is.EqualTo(("http://localhost:4318", "Grpc")), "a value already set wins");
		Assert.That((env["BEAM_LOCAL_OTEL"], env["BEAM_DISABLE_STANDARD_OTEL"]), Is.EqualTo(("true", "true")), "exporters on, Beamable's collector off");
	}

	/// <summary>Loki and Tempo get the retention the settings keep, in hours.</summary>
	[Test]
	public void Log_retention_is_substituted_in_hours()
	{
		Assert.That(LocalStackConfigIO.Substitute("${logRetention}", new LocalStackConfig()), Is.EqualTo("120h"));
		Assert.That(LocalStackConfigIO.Substitute("${logRetention}", new LocalStackConfig { logRetentionDays = 2 }), Is.EqualTo("48h"));
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
		Assert.That(Domain("group: player-engagement"), Is.EqualTo("microservices"), "a group runs microservices and extensions");

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

	// ----------------------------------------------------------------------------------
	// The progress board
	// ----------------------------------------------------------------------------------

	private static readonly DateTime BoardStart = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

	private static LocalStackBoardRow BoardRow(string name, LocalStackStepState state = LocalStackStepState.Waiting,
		int startedSecondsAgo = -1, int timeout = 0, bool once = false) => new LocalStackBoardRow
	{
		name = name, label = "Label " + name, state = state, timeoutSeconds = timeout, once = once,
		startedUtc = startedSecondsAgo < 0 ? null : BoardStart.AddSeconds(-startedSecondsAgo), changedUtc = BoardStart
	};

	/// <summary>A board in lines mode on a clock the test moves, with what it prints collected.</summary>
	private static (LocalStackBoard board, List<string> lines, Action<int> advance) LinesBoard(params LocalStackBoardRow[] rows)
	{
		var now = BoardStart;
		var lines = new List<string>();
		var board = new LocalStackBoard(new LocalStackBoardSnapshot { stack = "test", rows = rows.ToList() },
			LocalStackBoardMode.Lines, file: null, now: () => now);
		board.WriteLine = lines.Add;
		return (board, lines, seconds => now = now.AddSeconds(seconds));
	}

	/// <summary>Without a terminal each real change is one line with its time, and a repeat of the same state is not.</summary>
	[Test]
	public void Board_prints_one_line_per_state_change_without_a_terminal()
	{
		var (board, lines, advance) = LinesBoard(BoardRow("scala: auth", timeout: 180));

		board.Set("scala: auth", LocalStackStepState.Starting);
		advance(41);
		board.Set("scala: auth", LocalStackStepState.Starting, "still starting");
		board.Set("scala: auth", LocalStackStepState.Ready);

		Assert.That(lines, Has.Count.EqualTo(2));
		Assert.That(lines[0], Does.EndWith("Label scala: auth  Waiting -> Starting"));
		Assert.That(lines[1], Does.EndWith("Label scala: auth  Starting -> Ready (41s)"));
		Assert.That(lines[1], Does.Match(@"^\d\d:\d\d:\d\d  "));
	}

	/// <summary>A failure says what failed, shows its last log lines and where the rest is; a quiet one only moves the row.</summary>
	[Test]
	public void Board_prints_a_failure_with_its_last_lines_and_where_to_look()
	{
		var (board, lines, _) = LinesBoard(BoardRow("scala: notification"), BoardRow("csharp: gateway"));

		board.Set("scala: notification", LocalStackStepState.Failed, "exit 1",
			new[] { "ERROR Could not connect to broker at localhost:61616" }, "beam local logs \"scala: notification\"");
		board.Set("csharp: gateway", LocalStackStepState.Failed, "container died", announce: false);

		Assert.That(lines, Is.EqualTo(new[]
		{
			lines[0],
			"x Label scala: notification failed: exit 1",
			"    ERROR Could not connect to broker at localhost:61616",
			"    beam local logs \"scala: notification\""
		}));
		Assert.That(board.Row("csharp: gateway").state, Is.EqualTo(LocalStackStepState.Failed));
		Assert.That(board.Summary(), Is.EqualTo("0 of 2 ready, 2 failed"));
	}

	[Test]
	public void Board_bar_and_time_follow_the_state()
	{
		var starting = BoardRow("portal", LocalStackStepState.Starting, startedSecondsAgo: 30, timeout: 120);
		Assert.That(LocalStackBoard.Time(starting, BoardStart), Is.EqualTo("30s / 120s"));
		Assert.That(LocalStackBoard.Bar(starting, BoardStart), Is.EqualTo("[yellow]#####[/][grey]---------------[/]"));

		var waiting = BoardRow("mail");
		Assert.That(LocalStackBoard.Time(waiting, BoardStart), Is.Empty);
		Assert.That(LocalStackBoard.Bar(waiting, BoardStart), Is.EqualTo("[grey][/][grey]--------------------[/]"));

		// Settled: the time stops when the state was reached, the bar is full.
		var ready = BoardRow("docker", LocalStackStepState.Ready, startedSecondsAgo: 38, timeout: 300);
		Assert.That(LocalStackBoard.Time(ready, BoardStart.AddMinutes(5)), Is.EqualTo("38s"));
		Assert.That(LocalStackBoard.Bar(ready, BoardStart), Is.EqualTo("[green]####################[/][grey][/]"));

		// A build can't report how far it is: a block slides along instead.
		var building = BoardRow("build: scala", LocalStackStepState.Building, startedSecondsAgo: 2, once: true);
		Assert.That(LocalStackBoard.Bar(building, BoardStart), Is.EqualTo("[blue]--#####-------------[/]"));
		Assert.That(LocalStackBoard.Time(building, BoardStart), Is.EqualTo("2s"));
	}

	/// <summary>A narrow terminal gets the name and the state only.</summary>
	[Test]
	public void Board_drops_the_bar_and_time_when_narrow()
	{
		var snapshot = new LocalStackBoardSnapshot
		{
			stack = "backend",
			rows = { BoardRow("scala: auth", LocalStackStepState.Starting, startedSecondsAgo: 41, timeout: 180), BoardRow("scala: mail") }
		};
		snapshot.rows[0].port = 9002;

		string Draw(int width)
		{
			var console = new TestConsole().Width(width);
			console.Write(LocalStackBoard.Render(snapshot, width, BoardStart));
			return console.Output;
		}

		var wide = Draw(120);
		Assert.That(wide, Does.Contain("Running stack backend"));
		Assert.That(wide, Does.Contain("41s / 180s"));
		Assert.That(wide, Does.Contain(":9002"));
		Assert.That(wide, Does.Contain("#####"));
		Assert.That(wide, Does.Contain("0 of 2 ready"));

		var narrow = Draw(60);
		Assert.That(narrow, Does.Contain("Label scala: auth"));
		Assert.That(narrow, Does.Contain("Starting"));
		Assert.That(narrow, Does.Not.Contain("41s"));
		Assert.That(narrow, Does.Not.Contain(":9002"));
		Assert.That(narrow, Does.Not.Contain("###"));
	}

	/// <summary>A stack of 14 steps: 6 ready, 1 failed, 1 starting, 6 waiting.</summary>
	private static List<LocalStackBoardRow> TallBoard()
	{
		var rows = new List<LocalStackBoardRow>();
		for (var i = 0; i < 6; i++) rows.Add(BoardRow("ready " + i, LocalStackStepState.Ready));
		rows.Add(BoardRow("failed", LocalStackStepState.Failed));
		rows.Add(BoardRow("starting", LocalStackStepState.Starting, startedSecondsAgo: 5, timeout: 60));
		for (var i = 0; i < 6; i++) rows.Add(BoardRow("waiting " + i));
		return rows;
	}

	/// <summary>Following, the view keeps the step in progress near the top, with two rows of what came before.</summary>
	[Test]
	public void Board_follows_the_step_in_progress()
	{
		var rows = TallBoard();
		Assert.That(LocalStackBoard.FollowOffset(rows, 5), Is.EqualTo(5), "two rows above 'starting', at index 7");
		Assert.That(LocalStackBoard.FollowOffset(rows, 20), Is.EqualTo(0), "everything fits");

		// Nothing in progress: the first waiting step; nothing left at all: the end.
		rows[7].state = LocalStackStepState.Ready;
		Assert.That(LocalStackBoard.FollowOffset(rows, 5), Is.EqualTo(6));
		foreach (var row in rows) row.state = LocalStackStepState.Ready;
		Assert.That(LocalStackBoard.FollowOffset(rows, 5), Is.EqualTo(9), "the last full page");
	}

	/// <summary>Every row can be reached with the keys; a scroll stops following until F.</summary>
	[Test]
	public void Board_scrolls_with_the_keys()
	{
		var rows = TallBoard();
		var view = new LocalStackBoardView();
		ConsoleKeyInfo Key(ConsoleKey key) => new ConsoleKeyInfo('\0', key, false, false, false);

		Assert.That(view.Following, Is.True);
		Assert.That(view.Offset(rows, 5), Is.EqualTo(5));

		view.Press(Key(ConsoleKey.Home), rows, 5);
		Assert.That((view.Following, view.Offset(rows, 5)), Is.EqualTo((false, 0)));
		view.Press(Key(ConsoleKey.UpArrow), rows, 5);
		Assert.That(view.Offset(rows, 5), Is.EqualTo(0), "can't go above the top");
		view.Press(Key(ConsoleKey.PageDown), rows, 5);
		view.Press(Key(ConsoleKey.DownArrow), rows, 5);
		Assert.That(view.Offset(rows, 5), Is.EqualTo(6));
		view.Press(Key(ConsoleKey.End), rows, 5);
		Assert.That(view.Offset(rows, 5), Is.EqualTo(9), "the last 5 of 14");
		view.Press(Key(ConsoleKey.PageDown), rows, 5);
		Assert.That(view.Offset(rows, 5), Is.EqualTo(9), "can't go past the end");

		// A step moving on doesn't move a view the person scrolled.
		rows[7].state = LocalStackStepState.Ready;
		Assert.That(view.Offset(rows, 5), Is.EqualTo(9));

		view.Press(Key(ConsoleKey.F), rows, 5);
		Assert.That((view.Following, view.Offset(rows, 5)), Is.EqualTo((true, 6)));
		view.Press(Key(ConsoleKey.A), rows, 5);
		Assert.That(view.Following, Is.True, "other keys do nothing");
	}

	/// <summary>A frame shows which rows it holds, the summary, the latest messages and the keys, within the screen.</summary>
	[Test]
	public void Board_frame_fits_the_screen()
	{
		var snapshot = new LocalStackBoardSnapshot { stack = "full", rows = TallBoard() };
		var messages = new List<string> { "WRN [scala: mail] exited early (code 1); retrying (2 left)" };
		var visible = LocalStackBoard.VisibleCount(10, messages.Count);
		Assert.That(visible, Is.EqualTo(5), "10 lines less the title, summary, one message, the keys and the empty last row");

		var console = new TestConsole().Width(120);
		console.Write(LocalStackBoard.Frame(snapshot, 120, 5, visible, messages, following: true, BoardStart));
		var lines = console.Output.TrimEnd('\n').Split('\n');

		Assert.That(lines, Has.Length.EqualTo(9));
		Assert.That(lines[0], Does.Contain("Running stack full").And.Contain("rows 6-10 of 14, following"));
		Assert.That(lines[1], Does.Contain("Label ready 5"));
		Assert.That(lines[5], Does.Contain("Label waiting 1"));
		Assert.That(lines[6], Does.Contain("6 of 14 ready, 1 failed"));
		Assert.That(lines[7], Does.Contain("exited early"));
		Assert.That(lines[8], Does.Contain("PgUp/PgDn").And.Contain("Ctrl+C stops the stack"));

		Assert.That(LocalStackBoard.LastLines("a\r\n\r\nb\nc\nd\n", 3), Is.EqualTo(new[] { "b", "c", "d" }));
	}

	/// <summary>The message pane shows what was written, from any number of threads, not the writer's type name.</summary>
	[Test]
	public void Board_messages_keep_what_was_written()
	{
		var messages = new LocalStackBoardMessages();
		System.Threading.Tasks.Parallel.For(0, 50, i => messages.Writer.WriteLine($"warning {i}"));

		var lines = LocalStackBoard.LastLines(messages.Text, 100);
		Assert.That(lines, Is.EquivalentTo(Enumerable.Range(0, 50).Select(i => $"warning {i}")));
		Assert.That(messages.Text, Does.Not.Contain("TextWriter"));
	}

	/// <summary>
	/// `ps` prints the run's board: an attached run's board goes with its process, and a detached step that was up
	/// whose process is gone shows as stopped.
	/// </summary>
	[Test]
	public void Ps_shows_the_board_and_what_stopped_since()
	{
		var board = new LocalStackBoardSnapshot
		{
			stack = "backend", runnerPid = 4242, attached = false, host = "http://localhost:8080",
			rows =
			{
				BoardRow("scala: auth", LocalStackStepState.Ready, startedSecondsAgo: 40),
				BoardRow("scala: mail", LocalStackStepState.Ready, startedSecondsAgo: 30),
				BoardRow("docker: deps", LocalStackStepState.Done, startedSecondsAgo: 50),
			}
		};
		var state = new LocalStackRunState
		{
			steps =
			{
				new LocalStackRunEntry { name = "scala: auth", pid = 11 },
				new LocalStackRunEntry { name = "scala: mail", pid = 12 },
			}
		};

		var result = new LocalStackPsCommandResult();
		LocalStackPsCommand.Merge(result, board, state, entry => entry.pid == 11);

		Assert.That(result.stack, Is.EqualTo("backend"));
		Assert.That(result.steps.Select(s => s.state), Is.EqualTo(new[] { "Ready", "Stopped", "Done" }));
		Assert.That(result.steps[1].running, Is.False);
		Assert.That(result.steps[0].label, Is.EqualTo("Label scala: auth"));

		Assert.That(LocalStackBoardIO.IsStale(board, _ => false), Is.False, "a detached board lasts until stop");
		board.attached = true;
		Assert.That(LocalStackBoardIO.IsStale(board, pid => pid == 4242), Is.False);
		Assert.That(LocalStackBoardIO.IsStale(board, _ => false), Is.True, "the attached run is gone");
	}

	/// <summary>The board file round-trips, and clearing the run-state takes it away with it.</summary>
	[Test]
	public void Board_file_is_written_on_every_change_and_cleared_with_the_run_state()
	{
		var dir = Path.Combine(Path.GetTempPath(), "board-test-" + Guid.NewGuid().ToString("N"));
		var runState = Path.Combine(dir, LocalStackRunStateIO.RunStateFileName);
		var file = LocalStackBoardIO.PathFor(runState);
		try
		{
			LocalStackRunStateIO.Save(runState, new LocalStackRunState());
			var board = new LocalStackBoard(new LocalStackBoardSnapshot { stack = "full", rows = { BoardRow("portal") } },
				LocalStackBoardMode.Silent, file);
			board.Set("portal", LocalStackStepState.Starting);

			var saved = LocalStackBoardIO.Load(file);
			Assert.That(saved.stack, Is.EqualTo("full"));
			Assert.That(saved.rows.Single().state, Is.EqualTo(LocalStackStepState.Starting));
			Assert.That(File.ReadAllText(file), Does.Contain("\"Starting\""), "states are written by name");

			LocalStackRunStateIO.Clear(runState);
			Assert.That(File.Exists(file), Is.False);
		}
		finally
		{
			if (Directory.Exists(dir)) Directory.Delete(dir, true);
		}
	}

	/// <summary>Piped, a bare run and a dry run send their content as data instead of printing nothing.</summary>
	[Test]
	public void Listing_and_dry_run_are_data_too()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var listing = LocalStackRunCommand.Listing(stacks);
		Assert.That(listing.stacks.Select(s => s.name), Does.Contain("full").And.Contain("csharp-portal"));
		Assert.That(listing.stacks.First(s => s.name == "web-dev").variants.Select(v => v.name), Does.Contain("non-publish"));
		Assert.That(listing.stacks.Select(s => s.layer).Distinct(), Is.EqualTo(new[] { "built-in" }));
		Assert.That(listing.builtInVariants, Does.Contain("build").And.Contain("web"));

		var config = StackCatalogue();
		var resolved = stacks.Resolve("backend", config);
		var selected = resolved.steps.Where(s => !s.build).ToList();
		var dryRun = LocalStackRunCommand.DryRun(resolved, selected, new HashSet<LocalStackStep>(), config,
			new HashSet<string>(), new List<LocalStackDependencyCheck>());
		Assert.That(dryRun.stack, Is.EqualTo("backend"));
		Assert.That(dryRun.steps, Has.Count.EqualTo(resolved.steps.Count));
		var build = dryRun.steps.First(s => resolved.steps.First(r => r.name == s.name).build);
		Assert.That(build.selected, Is.False);
		Assert.That(build.runs, Does.StartWith("up to date"));
		Assert.That(dryRun.steps.First(s => s.selected).label, Is.Not.Empty);
	}

	// ----------------------------------------------------------------------------------
	// Builds: only what changed
	// ----------------------------------------------------------------------------------

	private static string RunGit(string dir, params string[] args)
	{
		var psi = new System.Diagnostics.ProcessStartInfo("git") { WorkingDirectory = dir, RedirectStandardOutput = true, RedirectStandardError = true };
		foreach (var arg in args) psi.ArgumentList.Add(arg);
		using var process = System.Diagnostics.Process.Start(psi)!;
		var output = process.StandardOutput.ReadToEnd();
		process.StandardError.ReadToEnd();
		process.WaitForExit();
		Assert.That(process.ExitCode, Is.EqualTo(0), $"git {string.Join(" ", args)}");
		return output;
	}

	/// <summary>A git repo with a committed src/ folder and README, and a build step that compiles src/.</summary>
	private static (string repo, LocalStackStep step, LocalStackConfig config) BuildRepo()
	{
		var repo = Path.Combine(Path.GetTempPath(), "build-inputs-" + Guid.NewGuid().ToString("N"), "MyRepo");
		Directory.CreateDirectory(Path.Combine(repo, "src"));
		RunGit(repo, "init", "-q");
		RunGit(repo, "config", "user.email", "test@example.com");
		RunGit(repo, "config", "user.name", "test");
		File.WriteAllText(Path.Combine(repo, "src", "a.cs"), "class A {}");
		File.WriteAllText(Path.Combine(repo, "README.md"), "readme");
		RunGit(repo, "add", ".");
		RunGit(repo, "commit", "-q", "-m", "first");

		var step = new LocalStackStep { name = "build: thing", build = true, waitForExit = true, workingDirectory = repo, inputs = new List<string> { "src" } };
		return (repo, step, new LocalStackConfig { steps = new List<LocalStackStep> { step } });
	}

	/// <summary>
	/// The fingerprint follows what the step compiles: an edit, a second edit of the same file, a new file and a commit
	/// change it; undoing the edit brings it back; a change outside the inputs doesn't touch it.
	/// </summary>
	[Test]
	public void Build_fingerprint_follows_the_inputs_only()
	{
		var (repo, step, config) = BuildRepo();
		try
		{
			string Print() => LocalStackBuilds.Fingerprint(step, config, out _);
			var clean = Print();
			Assert.That(clean, Is.Not.Null);
			Assert.That(Print(), Is.EqualTo(clean), "stable");

			File.WriteAllText(Path.Combine(repo, "README.md"), "changed outside the inputs");
			Assert.That(Print(), Is.EqualTo(clean));

			var file = Path.Combine(repo, "src", "a.cs");
			File.WriteAllText(file, "class A { int x; }");
			var edited = Print();
			Assert.That(edited, Is.Not.EqualTo(clean));
			File.WriteAllText(file, "class A { int y; }");
			Assert.That(Print(), Is.Not.EqualTo(edited), "a second edit of an already changed file counts");

			File.WriteAllText(file, "class A {}");
			Assert.That(Print(), Is.EqualTo(clean), "undone");

			File.WriteAllText(Path.Combine(repo, "src", "b.cs"), "class B {}");
			var added = Print();
			Assert.That(added, Is.Not.EqualTo(clean), "a new file");

			RunGit(repo, "add", ".");
			RunGit(repo, "commit", "-q", "-m", "second");
			var committed = Print();
			Assert.That(committed, Is.Not.EqualTo(clean), "a commit (or a pull) changes the tree");
			Assert.That(committed, Is.Not.EqualTo(added));
		}
		finally
		{
			DeleteTree(Path.GetDirectoryName(repo));
		}
	}

	/// <summary>A step builds until it has built once, then only when its inputs change; the reason names the repo.</summary>
	[Test]
	public void Build_runs_when_never_built_or_changed_and_not_otherwise()
	{
		var (repo, step, config) = BuildRepo();
		var stateFile = Path.Combine(Path.GetDirectoryName(repo)!, LocalStackBuilds.FileName);
		try
		{
			var state = LocalStackBuilds.Load(stateFile);
			Assert.That(LocalStackBuilds.WhyBuild(step, config, state), Is.EqualTo("not built by beam local run yet"));

			LocalStackBuilds.Record(step, config, state, stateFile);
			state = LocalStackBuilds.Load(stateFile);
			Assert.That(LocalStackBuilds.WhyBuild(step, config, state), Is.Null, "up to date");
			Assert.That(LocalStackBuilds.WhyBuild(new LocalStackStep { name = "BUILD: THING", build = true, workingDirectory = repo, inputs = step.inputs },
				config, state), Is.Null, "step names compare without case");

			File.WriteAllText(Path.Combine(repo, "src", "a.cs"), "class A { }");
			Assert.That(LocalStackBuilds.WhyBuild(step, config, state), Is.EqualTo("MyRepo changed since the last build"));

			// Outside git, or without inputs, there is nothing to compare: the old rule (output missing) applies.
			var plain = new LocalStackStep { name = "build: plain", build = true, workingDirectory = Path.GetTempPath(), inputs = new List<string> { "." } };
			var outside = Path.Combine(Path.GetTempPath(), "no-git-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(outside);
			plain.workingDirectory = outside;
			Assert.That(LocalStackBuilds.WhyBuild(plain, config, state), Is.Null);
			Directory.Delete(outside);
			Assert.That(LocalStackBuilds.WhyBuild(new LocalStackStep { name = "build: x", build = true, workingDirectory = repo }, config, state), Is.Null);
		}
		finally
		{
			DeleteTree(Path.GetDirectoryName(repo));
		}
	}

	[Test]
	public void Build_status_parsing_and_declared_inputs()
	{
		Assert.That(LocalStackBuilds.ChangedFiles(" M src/b.cs\0?? src/a.cs\0R  src/new.cs\0src/old.cs\0"),
			Is.EqualTo(new[] { "src/a.cs", "src/b.cs", "src/new.cs" }));
		Assert.That(LocalStackBuilds.ChangedFiles(string.Empty), Is.Empty);

		// The generated catalogue declares what each build compiles.
		var catalogue = StackCatalogue();
		Assert.That(catalogue.steps.Where(s => s.build && s.name.StartsWith("build: c# ")).Select(s => s.inputs), Has.All.EqualTo(new[] { "." }));
		Assert.That(catalogue.steps.Single(s => s.name == "build: portal deps").inputs, Is.EqualTo(new[] { "package.json", "package-lock.json" }));

		Assert.That(LocalStackBoard.Text(LocalStackStepState.UpToDate), Is.EqualTo("Up to date"));
		Assert.That(LocalStackBoard.Summary(new[] { BoardRow("a", LocalStackStepState.UpToDate), BoardRow("b") }), Is.EqualTo("1 of 2 ready"));
	}

	private static void DeleteTree(string dir)
	{
		if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
		// git marks its objects read-only.
		foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
			File.SetAttributes(file, FileAttributes.Normal);
		Directory.Delete(dir, true);
	}

	// ----------------------------------------------------------------------------------
	// Backend targets
	// ----------------------------------------------------------------------------------

	/// <summary>
	/// A flag beats its environment override, which beats the .beamable files; only host, cid, pid and the tokens
	/// can be overridden, as BEAM_HOST, BEAM_CID, BEAM_PID, BEAM_REFRESH_TOKEN and BEAM_ACCESS_TOKEN.
	/// </summary>
	[Test]
	public void Environment_overrides_sit_between_flags_and_config()
	{
		Assert.That(cli.ConfigService.EnvironmentVariableFor("refresh-token"), Is.EqualTo("BEAM_REFRESH_TOKEN"));
		Assert.That(cli.ConfigService.EnvironmentVariableFor("host"), Is.EqualTo("BEAM_HOST"));

		var previous = Environment.GetEnvironmentVariable("BEAM_HOST");
		var previousEngine = Environment.GetEnvironmentVariable("BEAM_ENGINE");
		try
		{
			Environment.SetEnvironmentVariable("BEAM_HOST", null);
			Assert.That(cli.ConfigService.PickSetting(null, "host", () => "from-config"), Is.EqualTo("from-config"));

			Environment.SetEnvironmentVariable("BEAM_HOST", "https://dev.api.beamable.com");
			Assert.That(cli.ConfigService.PickSetting(null, "host", () => "from-config"), Is.EqualTo("https://dev.api.beamable.com"));
			Assert.That(cli.ConfigService.PickSetting("from-flag", "host", () => "from-config"), Is.EqualTo("from-flag"));

			// Anything else ignores the environment.
			Environment.SetEnvironmentVariable("BEAM_ENGINE", "unity");
			Assert.That(cli.ConfigService.EnvironmentOverride("engine"), Is.Null);
			Assert.That(cli.ConfigService.PickSetting(null, "engine", () => "from-config"), Is.EqualTo("from-config"));
		}
		finally
		{
			Environment.SetEnvironmentVariable("BEAM_HOST", previous);
			Environment.SetEnvironmentVariable("BEAM_ENGINE", previousEngine);
		}
	}

	/// <summary>A stack runs against its target; a variant named after a target switches it; the listing says which.</summary>
	[Test]
	public void Stacks_run_against_their_target_and_a_variant_switches_it()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var config = StackCatalogue();
		config.targets = new Dictionary<string, LocalStackTarget> { ["staging"] = new LocalStackTarget { host = "https://staging.example" } };

		Assert.That(stacks.Resolve("full", config).target, Is.EqualTo("local"));
		Assert.That(stacks.Resolve("portal-extensions", config).target, Is.EqualTo("dev"));
		Assert.That(stacks.Resolve("web-dev", config).target, Is.EqualTo("dev"));
		Assert.That(stacks.Resolve("remote", config).target, Is.EqualTo("dev"));
		Assert.That(stacks.Resolve("portal-extensions:local", config).target, Is.EqualTo("local"));
		Assert.That(stacks.Resolve("full:dev", config).target, Is.EqualTo("dev"));
		Assert.That(stacks.Resolve("full:build:staging", config).target, Is.EqualTo("staging"));
		Assert.That(stacks.Resolve("full:STAGING", config).target, Is.EqualTo("staging"), "names compare without case");

		// A target variant changes only the backend, not the steps.
		Assert.That(StepNames(stacks.Resolve("full:dev", config)), Is.EqualTo(StepNames(stacks.Resolve("full", config))));

		Assert.That(() => stacks.Resolve("full:nope", config),
			Throws.InstanceOf<LocalStackStackException>().With.Message.Contains(":dev").And.Message.Contains(":staging"));

		var listing = stacks.List();
		Assert.That(listing.First(s => s.name == "portal-extensions").target, Is.EqualTo("dev"));
		Assert.That(listing.First(s => s.name == "full").target, Is.EqualTo("local"));
		Assert.That(LocalStackRunCommand.Listing(stacks, config).targets, Is.EqualTo(new[] { "local", "lan", "dev", "staging", "prod" }));
	}

	/// <summary>A user's stack can name a target, and a stack extending it inherits it.</summary>
	[Test]
	public void A_stack_names_its_target_and_extends_passes_it_on()
	{
		var stacks = LocalStackStacks.BuiltIn();
		stacks.Add("mine", LocalStackLayer.Local, "mine.json", new LocalStackStackDef { extends = "backend", target = "dev" });
		stacks.Add("child", LocalStackLayer.Local, "child.json", new LocalStackStackDef { extends = "mine" });
		var config = StackCatalogue();

		Assert.That(stacks.Resolve("mine", config).target, Is.EqualTo("dev"));
		Assert.That(stacks.Resolve("child", config).target, Is.EqualTo("dev"));
		Assert.That(stacks.Resolve("child:local", config).target, Is.EqualTo("local"));
	}

	/// <summary>
	/// Where a target points: local is the settings' host, dev is built in, the settings add or change targets; a
	/// login becomes the environment of a stack's beam steps, without the access token.
	/// </summary>
	[Test]
	public void Targets_resolve_and_become_the_beam_steps_environment()
	{
		var config = new LocalStackConfig { host = "http://localhost:8080" };
		Assert.That(LocalStackTargets.Get("local", config).host, Is.EqualTo("http://localhost:8080"));
		Assert.That(LocalStackTargets.Get("dev", config).host, Is.EqualTo("https://dev.api.beamable.com"));
		Assert.That(() => LocalStackTargets.Get("nope", config), Throws.InstanceOf<LocalStackStackException>().With.Message.Contains("local, lan, dev"));

		config.targets = new Dictionary<string, LocalStackTarget>
		{
			["DEV"] = new LocalStackTarget { cid = "my-org", pid = "DE_1" },
			["staging"] = new LocalStackTarget { host = "https://staging.example/" },
			["broken"] = new LocalStackTarget()
		};
		var dev = LocalStackTargets.Get("dev", config);
		Assert.That((dev.host, dev.cid, dev.pid), Is.EqualTo(("https://dev.api.beamable.com", "my-org", "DE_1")), "settings fill in the built-in target");
		Assert.That(LocalStackTargets.Get("staging", config).host, Is.EqualTo("https://staging.example"));
		Assert.That(() => LocalStackTargets.Get("broken", config), Throws.InstanceOf<LocalStackStackException>().With.Message.Contains("host"));

		var login = new LocalStackTargetLogin { host = "https://dev.api.beamable.com", cid = "123", pid = "DE_1", accessToken = "access", refreshToken = "refresh" };
		Assert.That(LocalStackTargets.Environment(login), Is.EquivalentTo(new Dictionary<string, string>
		{
			["BEAM_HOST"] = "https://dev.api.beamable.com", ["BEAM_CID"] = "123", ["BEAM_PID"] = "DE_1", ["BEAM_REFRESH_TOKEN"] = "refresh"
		}));
		Assert.That(LocalStackTargets.Environment(null), Is.Empty, "no login yet: the steps use the workspace's");

		var root = Path.Combine(Path.GetTempPath(), "targets-" + Guid.NewGuid().ToString("N"));
		try
		{
			LocalStackTargets.Save(root, "Dev", login);
			Assert.That(File.Exists(Path.Combine(root, ".beamable", "local", "targets", "dev", "login.json")), Is.True);
			Assert.That(LocalStackTargets.Load(root, "dev").refreshToken, Is.EqualTo("refresh"));
			Assert.That(LocalStackTargets.Load(root, "staging"), Is.Null);
		}
		finally
		{
			if (Directory.Exists(root)) Directory.Delete(root, true);
		}
	}

	// ----------------------------------------------------------------------------------
	// Restarting steps and changing a running stack
	// ----------------------------------------------------------------------------------

	/// <summary>Steps are named by name, label or tag; a tag leaves the build steps out; order follows the stack.</summary>
	[Test]
	public void Running_steps_are_matched_by_name_label_or_tag()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var steps = stacks.Resolve("full", StackCatalogue()).steps;

		Assert.That(LocalStackControl.Match(new[] { "portal frontend" }, steps, stacks.TagsOf).Select(s => s.name), Is.EqualTo(new[] { "portal frontend" }));
		Assert.That(LocalStackControl.Match(new[] { "C# GATEWAY" }, steps, stacks.TagsOf).Select(s => s.name), Is.EqualTo(new[] { "c# gateway" }));
		Assert.That(LocalStackControl.Match(new[] { "Scala service auth" }, steps, stacks.TagsOf).Select(s => s.name), Is.EqualTo(new[] { "scala: auth" }), "by label");

		var portal = LocalStackControl.Match(new[] { "portal" }, steps, stacks.TagsOf).Select(s => s.name).ToList();
		Assert.That(portal, Does.Contain("portal frontend"));
		Assert.That(portal, Has.None.StartsWith("build:"), "builds come through the build check");

		var two = LocalStackControl.Match(new[] { "scala: auth", "c# gateway" }, steps, stacks.TagsOf).Select(s => s.name).ToList();
		Assert.That(two, Is.EqualTo(new[] { "c# gateway", "scala: auth" }), "stack order");

		Assert.That(() => LocalStackControl.Match(new[] { "nope" }, steps, stacks.TagsOf),
			Throws.InstanceOf<cli.CliException>().With.Message.Contains("isn't a step, label or tag"));
	}

	/// <summary>A step's own build: the one named after it, otherwise the one of its kind.</summary>
	[Test]
	public void A_step_is_rebuilt_by_its_own_build()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var steps = stacks.Resolve("full", StackCatalogue()).steps;
		List<string> Builds(string name) => LocalStackControl.BuildsFor(steps.Single(s => s.name == name), steps, stacks.TagsOf).Select(s => s.name).ToList();

		Assert.That(Builds("c# gateway"), Is.EqualTo(new[] { "build: c# gateway" }));
		Assert.That(Builds("c# campaign runtime"), Is.EqualTo(new[] { "build: c# campaign runtime" }));
		Assert.That(Builds("scala: auth"), Is.EqualTo(new[] { "build: scala" }));
		Assert.That(Builds("portal frontend"), Is.EqualTo(new[] { "build: portal deps" }));
		Assert.That(Builds("docker: api deps + caddy"), Is.Empty);
	}

	/// <summary>A request is a file the running stack takes, answers, and the asker reads.</summary>
	[Test]
	public async System.Threading.Tasks.Task Control_requests_are_taken_and_answered()
	{
		var dir = Path.Combine(Path.GetTempPath(), "control-" + Guid.NewGuid().ToString("N"));
		var runState = Path.Combine(dir, LocalStackRunStateIO.RunStateFileName);
		Directory.CreateDirectory(dir);
		try
		{
			var runner = System.Threading.Tasks.Task.Run(async () =>
			{
				for (var i = 0; i < 100; i++)
				{
					foreach (var request in LocalStackControl.Take(runState))
					{
						LocalStackControl.Answer(runState, new LocalStackControlResponse
						{
							id = request.id, ok = true, message = $"{request.action} {string.Join("+", request.items)}", steps = request.items
						});
						return;
					}

					await System.Threading.Tasks.Task.Delay(50);
				}
			});

			var answer = await LocalStackControl.Ask(runState, new LocalStackControlRequest { action = "restart", items = { "portal", "scala" } },
				TimeSpan.FromSeconds(10), System.Threading.CancellationToken.None);
			await runner;

			Assert.That(answer, Is.Not.Null);
			Assert.That(answer.message, Is.EqualTo("restart portal+scala"));
			Assert.That(Directory.GetFiles(LocalStackControl.DirFor(runState)), Is.Empty, "both files are gone");

			var none = await LocalStackControl.Ask(runState, new LocalStackControlRequest { action = "stop" }, TimeSpan.FromMilliseconds(300),
				System.Threading.CancellationToken.None);
			Assert.That(none, Is.Null, "nobody answered");
			Assert.That(Directory.GetFiles(LocalStackControl.DirFor(runState)), Is.Empty, "an unanswered request is taken back");
		}
		finally
		{
			Directory.Delete(dir, true);
		}
	}

	[Test]
	public void Board_rows_follow_a_changed_stack()
	{
		var board = new LocalStackBoard(new LocalStackBoardSnapshot { stack = "a", rows = { BoardRow("one"), BoardRow("two") } },
			LocalStackBoardMode.Silent, file: null);
		board.AddRows(new[] { BoardRow("two"), BoardRow("three") });
		board.RemoveRows(new[] { "ONE" });
		board.Rename("a:web");

		Assert.That(board.Name, Is.EqualTo("a:web"));
		Assert.That(board.Rows.Select(r => r.name), Is.EqualTo(new[] { "two", "three" }));
	}

	/// <summary>
	/// An alias lookup's answer names the org by its cid. The local backend answers an alias in use with its cid and
	/// "available": true; reading that as "no such org" left a stack without a login to the local realm.
	/// </summary>
	[Test]
	public void An_alias_lookup_finds_the_org_by_its_cid()
	{
		Assert.That(LocalStackTargets.CidOfAlias(Newtonsoft.Json.Linq.JObject.Parse("{\"alias\":\"beam-project\",\"available\":true,\"cid\":106100219883553792}")),
			Is.EqualTo("106100219883553792"));
		Assert.That(LocalStackTargets.CidOfAlias(Newtonsoft.Json.Linq.JObject.Parse("{\"alias\":\"x\",\"available\":false,\"cid\":\"42\"}")), Is.EqualTo("42"));
		Assert.That(LocalStackTargets.CidOfAlias(Newtonsoft.Json.Linq.JObject.Parse("{\"alias\":\"free\",\"available\":true}")), Is.Null);
		Assert.That(LocalStackTargets.CidOfAlias(Newtonsoft.Json.Linq.JObject.Parse("{\"alias\":\"free\",\"cid\":null}")), Is.Null);
	}

	/// <summary>
	/// The org's realms come from its customer listing, which has all three: the games listing has only each game's
	/// root, so the stack's login used to land on prod, and only prod got its zone.
	/// </summary>
	[Test]
	public void The_local_stack_logs_in_to_the_dev_realm_and_every_realm_gets_its_zone()
	{
		var customer = Newtonsoft.Json.Linq.JObject.Parse(@"{""customer"":{""cid"":1,""alias"":""beam-project"",""projects"":[
			{""projectName"":""beam-project-prod"",""pid"":""DE_44"",""children"":[""DE_45""],""archived"":false},
			{""projectName"":""beam-project-staging"",""pid"":""DE_45"",""parent"":""DE_44"",""children"":[""DE_46""],""archived"":false},
			{""projectName"":""beam-project-dev"",""pid"":""DE_46"",""parent"":""DE_45"",""children"":[],""archived"":false}]}}");

		var realms = LocalStackTargets.RealmsOf("1", customer);

		Assert.That(realms.Select(r => r.Pid).OrderBy(p => p), Is.EqualTo(new[] { "DE_44", "DE_45", "DE_46" }));
		Assert.That(LocalStackTargets.LocalRealm(realms).Pid, Is.EqualTo("DE_46"));
		Assert.That(LocalStackTargets.RealmChoices(realms, LocalStackTargets.LocalRealm(realms)).Select(r => r.Pid), Is.EqualTo(new[] { "DE_46", "DE_45", "DE_44" }),
			"a remote target's realm picker offers dev first, then up to prod");
		Assert.That(LocalStackTargets.RealmChoices(realms, realms.First(r => r.Pid == "DE_44")).Select(r => r.Pid), Is.EqualTo(new[] { "DE_44", "DE_46", "DE_45" }),
			"the realm in use comes first when changing it");
		Assert.That(LocalStackTargets.ZonePlan(realms, new[] { "dev", "staging", "prod" }).bind.OrderBy(b => b.pid),
			Is.EqualTo(new[] { ("DE_44", "prod"), ("DE_45", "staging"), ("DE_46", "dev") }));
	}

	/// <summary>
	/// The local org gets the zones dev, staging and prod: the ones it lacks are created (dev and prod come with the
	/// first zone call, so a fresh org lacks staging), and every realm in use is bound to its tier's zone.
	/// </summary>
	[Test]
	public void The_local_org_gets_a_zone_per_realm_tier()
	{
		Beamable.Common.Api.Realms.RealmView Realm(string pid, int depth, bool archived = false) =>
			new Beamable.Common.Api.Realms.RealmView { Pid = pid, Depth = depth, Archived = archived };
		var realms = new[] { Realm("prod-pid", 0), Realm("staging-pid", 1), Realm("dev-pid", 2), Realm("old-pid", 2, archived: true) };

		var (create, bind) = LocalStackTargets.ZonePlan(realms, new[] { "dev", "prod" });

		Assert.That(create, Is.EqualTo(new[] { "staging" }));
		Assert.That(bind, Is.EqualTo(new[] { ("prod-pid", "prod"), ("staging-pid", "staging"), ("dev-pid", "dev") }));
		Assert.That(LocalStackTargets.ZonePlan(realms, new[] { "Dev", "staging", "prod" }).create, Is.Empty, "names match in any case");
	}

	/// <summary>
	/// lan is the local backend at this machine's LAN address; staging and prod are Beamable's, and prod asks first;
	/// an address is a backend of its own, whose login gets a folder named after its host.
	/// </summary>
	[Test]
	public void Targets_include_lan_staging_prod_and_addresses()
	{
		var config = new LocalStackConfig { host = "http://localhost:8080" };

		var lan = LocalStackTargets.Get("lan", config, () => "192.168.1.20");
		Assert.That((lan.host, lan.local), Is.EqualTo(("http://192.168.1.20:8080", (bool?)true)));
		Assert.That(() => LocalStackTargets.Get("lan", config, () => null),
			Throws.InstanceOf<LocalStackStackException>().With.Message.Contains("No LAN address"));
		config.targets = new Dictionary<string, LocalStackTarget> { ["lan"] = new LocalStackTarget { host = "http://10.0.0.5:8080" } };
		Assert.That(LocalStackTargets.Get("lan", config, () => throw new InvalidOperationException("not asked")).host,
			Is.EqualTo("http://10.0.0.5:8080"), "the settings pin it");

		Assert.That(LocalStackTargets.Get("staging", config).host, Is.EqualTo("https://staging.api.beamable.com"));
		var prod = LocalStackTargets.Get("prod", config);
		Assert.That((prod.host, prod.confirm), Is.EqualTo(("https://api.beamable.com", (bool?)true)));

		var address = LocalStackTargets.Get("https://gw.example.com/", config);
		Assert.That((address.host, address.local), Is.EqualTo(("https://gw.example.com", (bool?)null)));
		Assert.That(LocalStackTargets.FolderName("https://gw.example.com/"), Is.EqualTo("gw.example.com_443"));
		Assert.That(LocalStackTargets.FolderName("Dev"), Is.EqualTo("dev"));

		Assert.That(LocalStackTargets.RunsLocal("local", config), Is.True);
		Assert.That(LocalStackTargets.RunsLocal("lan", config), Is.True, "the settings' lan keeps the built-in's local");
		Assert.That(LocalStackTargets.RunsLocal("dev", config), Is.False);
		Assert.That(LocalStackTargets.RunsLocal("https://gw.example.com", config), Is.False);

		Assert.That(LocalStackTargets.IsVirtualAdapter("vEthernet (WSL)", "Hyper-V Virtual Ethernet Adapter"), Is.True);
		Assert.That(LocalStackTargets.IsVirtualAdapter("Ethernet 2", "VirtualBox Host-Only Ethernet Adapter"), Is.True);
		Assert.That(LocalStackTargets.IsVirtualAdapter("Wi-Fi", "Intel(R) Wi-Fi 6E AX211 160MHz"), Is.False);
		Assert.That(LocalStackTargets.WithAddress("http://localhost:8080", "192.168.1.20"), Is.EqualTo("http://192.168.1.20:8080"));
	}

	/// <summary>A stack can name a backend by address; one that is neither a target nor an address doesn't resolve.</summary>
	[Test]
	public void A_backend_is_a_target_or_an_address()
	{
		var stacks = LocalStackStacks.BuiltIn();
		stacks.Add("gw", LocalStackLayer.Local, "gw.json", new LocalStackStackDef { extends = "backend", target = "https://gw.example.com" });
		stacks.Add("odd", LocalStackLayer.Local, "odd.json", new LocalStackStackDef { extends = "backend", target = "nowhere" });
		var config = StackCatalogue();

		Assert.That(stacks.Resolve("gw", config).target, Is.EqualTo("https://gw.example.com"));
		Assert.That(stacks.Resolve("full:prod", config).target, Is.EqualTo("prod"));
		Assert.That(() => stacks.Resolve("odd", config),
			Throws.InstanceOf<LocalStackStackException>().With.Message.Contains("'nowhere' isn't a target"));
	}

	// ----------------------------------------------------------------------------------
	// Builds side by side
	// ----------------------------------------------------------------------------------

	/// <summary>One lane per folder: the C# hosts share BeamableAPI's, the Scala reactor and the portal have their own.</summary>
	[Test]
	public void Builds_run_in_one_lane_per_folder()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var config = StackCatalogue();

		var lanes = LocalStackRunCommand.BuildLanes(stacks.Resolve("full", config).steps, config)
			.Select(l => l.Select(s => s.name).ToList()).ToList();
		var csharp = lanes.Single(l => l.Any(n => n.StartsWith("build: c# ")));
		Assert.That(csharp, Is.EqualTo(csharp.OrderBy(n => stacks.Resolve("full", config).steps.FindIndex(s => s.name == n)).ToList()), "stack order");
		Assert.That(csharp, Has.All.StartsWith("build: c# "));
		Assert.That(lanes, Has.Some.EqualTo(new[] { "build: scala" }));
		Assert.That(lanes, Has.Some.EqualTo(new[] { "build: portal deps" }));

	}

	/// <summary>Lanes run side by side and each lane's builds in order; a failed build fails the rest of its lane unrun.</summary>
	[Test]
	public async System.Threading.Tasks.Task Lanes_run_side_by_side_and_in_order()
	{
		LocalStackStep Build(string name) => new LocalStackStep { name = name, build = true };
		var (a1, a2, b1) = (Build("a1"), Build("a2"), Build("b1"));
		var ran = new System.Collections.Concurrent.ConcurrentQueue<string>();
		var b1Started = new System.Threading.Tasks.TaskCompletionSource();

		var tasks = LocalStackRunCommand.RunInLanes(new[] { new List<LocalStackStep> { a1, a2 }, new List<LocalStackStep> { b1 } }, async step =>
		{
			ran.Enqueue(step.name);
			if (step == b1) b1Started.TrySetResult();
			// a1 only finishes once b1 has started: one after the other, this would never end.
			if (step == a1) await b1Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
			if (step == a2) throw new InvalidOperationException("a2 failed");
		});

		await tasks[a1];
		await tasks[b1];
		Assert.That(async () => await tasks[a2], Throws.InstanceOf<InvalidOperationException>());
		Assert.That(ran.ToList().IndexOf("a1"), Is.LessThan(ran.ToList().IndexOf("a2")));

		var c1 = Build("c1");
		var c2 = Build("c2");
		var after = new List<string>();
		var failing = LocalStackRunCommand.RunInLanes(new[] { new List<LocalStackStep> { c1, c2 } }, step =>
		{
			after.Add(step.name);
			return step == c1 ? System.Threading.Tasks.Task.FromException(new InvalidOperationException("c1 failed")) : System.Threading.Tasks.Task.CompletedTask;
		});
		Assert.That(async () => await failing[c2], Throws.InstanceOf<InvalidOperationException>().With.Message.EqualTo("c1 failed"));
		Assert.That(after, Is.EqualTo(new[] { "c1" }), "c2 never ran");
	}

	// ----------------------------------------------------------------------------------
	// Restarting from a list, and from the board
	// ----------------------------------------------------------------------------------

	/// <summary>
	/// What restart lists when nothing is named: the services by tag in the known order, no builds or run-once
	/// steps; as text a service shows under each of its tags, in the picker only under its first.
	/// </summary>
	[Test]
	public void Restart_lists_the_services_by_tag()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var steps = stacks.Resolve("full", StackCatalogue()).steps;
		var rows = steps.Select(s => new LocalStackBoardRow
		{
			name = s.name, label = s.label, once = s.build || s.waitForExit,
			state = s.name == "portal frontend" ? LocalStackStepState.Stopped : LocalStackStepState.Ready
		}).ToList();

		var text = LocalStackPsCommand.Choices(rows, steps, stacks.TagsOf, starting: false, firstTagOnly: false);
		var tags = text.Select(c => c.tag).ToList();
		Assert.That(tags, Does.Not.Contain("build"));
		Assert.That(tags, Is.EqualTo(tags.OrderBy(t => Array.IndexOf(LocalStackStacks.KnownTags, t) is var i && i < 0 ? int.MaxValue : i).ToList()));
		var all = text.SelectMany(c => c.steps).Select(s => s.name).ToList();
		Assert.That(all, Has.None.StartsWith("build:"));
		Assert.That(all, Does.Not.Contain("docker: api deps + caddy"), "runs once");
		Assert.That(text.First(c => c.tag == "scala").steps.Select(s => s.name), Does.Contain("scala: auth"));
		Assert.That(text.First(c => c.tag == "scala-core").steps.Select(s => s.name), Does.Contain("scala: auth"));

		var picker = LocalStackPsCommand.Choices(rows, steps, stacks.TagsOf, starting: false, firstTagOnly: true);
		var listed = picker.SelectMany(c => c.steps).Select(s => s.name).ToList();
		Assert.That(listed, Is.Unique);
		Assert.That(picker.First(c => c.tag == "scala").steps.Select(s => s.name), Does.Not.Contain("scala: auth"), "already under scala-core");

		var start = LocalStackPsCommand.Choices(rows, steps, stacks.TagsOf, starting: true, firstTagOnly: true);
		Assert.That(start.SelectMany(c => c.steps).Select(s => s.name), Is.EqualTo(new[] { "portal frontend" }), "only what isn't up");
	}

	/// <summary>
	/// On a board that can act, the arrows select a row and keep it on screen; Space asks what to do with it, Enter
	/// does it (S stops instead), Esc drops it; a build, or anything before the stack is up, only says why not.
	/// </summary>
	/// <summary>L opens Grafana's dashboards, whatever is selected.</summary>
	[Test]
	public void L_opens_Grafana()
	{
		var rows = TallBoard();
		ConsoleKeyInfo Key(ConsoleKey key) => new ConsoleKeyInfo(' ', key, false, false, false);
		var view = new LocalStackBoardView { Actions = true };

		Assert.That(view.Press(Key(ConsoleKey.L), rows, 5), Is.EqualTo((LocalStackBoardView.GrafanaAction, (string)null)));
		view.Press(Key(ConsoleKey.Home), rows, 5);
		Assert.That(view.Press(Key(ConsoleKey.L), rows, 5), Is.EqualTo((LocalStackBoardView.GrafanaAction, (string)null)));
	}

	[Test]
	public void Board_keys_select_a_row_and_act_on_it()
	{
		var rows = TallBoard();
		rows[0].once = true;
		rows[3].state = LocalStackStepState.Stopped;
		ConsoleKeyInfo Key(ConsoleKey key) => new ConsoleKeyInfo(key == ConsoleKey.Spacebar ? ' ' : '\0', key, false, false, false);
		var view = new LocalStackBoardView { Actions = true };

		Assert.That(view.Press(Key(ConsoleKey.Spacebar), rows, 5, stackUp: true), Is.Null);
		Assert.That(view.Bar, Does.Contain("Select a step"));

		view.Press(Key(ConsoleKey.Home), rows, 5);
		Assert.That(view.SelectedName(rows), Is.EqualTo(rows[0].name));
		view.Press(Key(ConsoleKey.Spacebar), rows, 5, stackUp: true);
		Assert.That(view.Bar, Does.Contain("runs once"));

		view.Press(Key(ConsoleKey.DownArrow), rows, 5);
		view.Press(Key(ConsoleKey.Spacebar), rows, 5, stackUp: false);
		Assert.That(view.Bar, Does.Contain("Wait until the stack is up"));
		Assert.That(view.Press(Key(ConsoleKey.Enter), rows, 5, stackUp: false), Is.Null, "nothing was asked");

		view.Press(Key(ConsoleKey.Spacebar), rows, 5, stackUp: true);
		Assert.That(view.Bar, Does.StartWith("Restart"));
		Assert.That(view.Press(Key(ConsoleKey.Enter), rows, 5, stackUp: true), Is.EqualTo(("restart", rows[1].name)));
		Assert.That(view.Bar, Is.Null);

		view.Press(Key(ConsoleKey.Spacebar), rows, 5, stackUp: true);
		Assert.That(view.Press(Key(ConsoleKey.S), rows, 5, stackUp: true), Is.EqualTo(("stop", rows[1].name)));
		view.Press(Key(ConsoleKey.Spacebar), rows, 5, stackUp: true);
		Assert.That(view.Press(Key(ConsoleKey.Escape), rows, 5, stackUp: true), Is.Null);

		view.Press(Key(ConsoleKey.DownArrow), rows, 5);
		view.Press(Key(ConsoleKey.DownArrow), rows, 5);
		view.Press(Key(ConsoleKey.Spacebar), rows, 5, stackUp: true);
		Assert.That(view.Bar, Does.StartWith("Start"));
		Assert.That(view.Press(Key(ConsoleKey.Enter), rows, 5, stackUp: true), Is.EqualTo(("start", rows[3].name)));

		view.Press(Key(ConsoleKey.End), rows, 5);
		Assert.That(view.SelectedName(rows), Is.EqualTo(rows[^1].name));
		Assert.That(view.Offset(rows, 5), Is.EqualTo(rows.Count - 5), "the selection stays on screen");
		view.Press(Key(ConsoleKey.F), rows, 5);
		Assert.That((view.Following, view.SelectedName(rows)), Is.EqualTo((true, (string)null)));
	}

	/// <summary>A frame marks the selected row, shows the question and the keys it was given, and once up, where the stack is.</summary>
	[Test]
	public void Board_frame_marks_the_selection_and_asks()
	{
		var snapshot = new LocalStackBoardSnapshot { stack = "full", rows = TallBoard(), up = true, portalUrl = "http://localhost:4950", host = "http://localhost:8080" };
		var console = new TestConsole().Width(140);
		console.Write(LocalStackBoard.Frame(snapshot, 140, 0, 5, new List<string>(), following: false, BoardStart,
			selected: snapshot.rows[1].name, bar: "Restart Label 1?  Enter restart, S stop, Esc cancel", keys: LocalStackBoardView.ActionKeys));
		var lines = console.Output.TrimEnd('\n').Split('\n');

		Assert.That(lines[2], Does.Contain("> "));
		Assert.That(lines[1], Does.Not.Contain("> "));
		Assert.That(lines[^3], Does.Contain("portal http://localhost:4950").And.Contain("backend http://localhost:8080"));
		Assert.That(lines[^2], Does.Contain("Restart Label 1?"));
		Assert.That(lines[^1], Does.Contain("Space acts on it"));
	}

	/// <summary>
	/// An attached stack keeps no run-state, so `beam local logs` finds its steps' output through the board, which says
	/// where the run writes it: a step with a log file is listed, one without isn't.
	/// </summary>
	[Test]
	public void Logs_find_an_attached_stacks_output_through_its_board()
	{
		var dir = Directory.CreateTempSubdirectory("beam-attached-logs");
		try
		{
			var runStatePath = Path.Combine(dir.FullName, "local-stack.run.json");
			var logsDir = Directory.CreateDirectory(Path.Combine(dir.FullName, "run")).FullName;
			File.WriteAllText(Path.Combine(logsDir, LocalStackRunCommand.SafeName("portal extension: campaigns") + ".err.log"), "boom");
			LocalStackBoardIO.Save(LocalStackBoardIO.PathFor(runStatePath), new LocalStackBoardSnapshot
			{
				stack = "full", attached = true, runnerPid = Environment.ProcessId, logsDir = logsDir,
				rows = new List<LocalStackBoardRow> { new LocalStackBoardRow { name = "portal extension: campaigns" }, new LocalStackBoardRow { name = "never started" } }
			});

			var state = LocalStackLogsCommand.FromAttachedBoard(runStatePath);

			Assert.That(state.steps.Select(s => s.name), Is.EqualTo(new[] { "portal extension: campaigns" }));
			Assert.That(File.ReadAllText(state.steps[0].stderrLog), Is.EqualTo("boom"));
			Assert.That(LocalStackLogsCommand.FromAttachedBoard(Path.Combine(dir.FullName, "elsewhere", "local-stack.run.json")), Is.Null);
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	/// <summary>The local web package versions get a line of their own, on the live board and on ps's snapshot.</summary>
	[Test]
	public void The_local_web_packages_get_their_own_line()
	{
		var snapshot = new LocalStackBoardSnapshot
		{
			stack = "full:web", rows = TallBoard(),
			webPackages = new Dictionary<string, string> { ["@beamable/portal-toolkit"] = "0.6.0-0.20261023132716", ["@beamable/sdk"] = "1.4.0-0.20261023132716" }
		};
		const string line = "Local web packages: @beamable/portal-toolkit 0.6.0-0.20261023132716, @beamable/sdk 1.4.0-0.20261023132716";

		var live = new TestConsole().Width(140);
		live.Write(LocalStackBoard.Frame(snapshot, 140, 0, 5, new List<string>(), following: false, BoardStart));
		Assert.That(live.Output, Does.Contain(line));

		var once = new TestConsole().Width(140);
		once.Write(LocalStackBoard.Render(snapshot, 140, BoardStart));
		Assert.That(once.Output, Does.Contain(line));

		snapshot.webPackages = null;
		Assert.That(LocalStackBoard.WebPackagesLine(snapshot), Is.Null);
	}

	/// <summary>
	/// A drawn frame stays off the screen's last column and last row, selected or not, with the question or without:
	/// a line reaching either made the terminal wrap or scroll, and the board flickered blank on the arrow keys.
	/// </summary>
	[Test]
	public void A_drawn_board_never_reaches_the_screens_edge()
	{
		var rows = TallBoard().Concat(TallBoard().Select(r => { r.name += " again"; r.detail = new string('x', 300); return r; })).ToList();
		var snapshot = new LocalStackBoardSnapshot { stack = "full", rows = rows, up = true, portalUrl = "http://localhost:4950", host = "http://localhost:8080" };
		var messages = new List<string> { new string('m', 400) };
		foreach (var width in new[] { 60, 100, 200 })
		foreach (var height in new[] { 10, 30 })
		foreach (var (selected, bar) in new[] { ((string)null, (string)null), (rows[3].name, (string)null), (rows[3].name, "Restart it?  Enter restart, S stop, Esc cancel") })
		{
			var visible = LocalStackBoard.VisibleCount(height, messages.Count + (bar != null ? 1 : 0));
			var frame = LocalStackBoard.Frame(snapshot, width, 0, visible, messages, following: false, BoardStart, selected, bar,
				LocalStackBoardView.ActionKeys, marks: true);
			var drawn = LocalStackBoard.Draw(frame, new TestConsole().Width(width), width, height);
			var lines = System.Text.RegularExpressions.Regex.Replace(drawn, "\u001b\\[[0-9;?]*[A-Za-z]", string.Empty).Split('\n');

			Assert.That(lines.Length, Is.LessThan(height), $"{width}x{height}, selected {selected}, bar {bar != null}");
			Assert.That(lines.Max(l => l.Length), Is.LessThan(width), $"{width}x{height}, selected {selected}, bar {bar != null}");
		}
	}

	// ----------------------------------------------------------------------------------
	// What steps need
	// ----------------------------------------------------------------------------------

	/// <summary>
	/// A step's needs are followed through: the C# gateway needs the docker deps and its build; a Scala service
	/// needs Redis, the reactor, dbflake and the gateway; a microservice on the local backend needs the backend to
	/// log into; an extension needs the microservices it calls. On a remote backend a service needs nothing local.
	/// </summary>
	[Test]
	public void Missing_dependencies_are_found_and_followed()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var catalogue = StackCatalogue().steps;
		LocalStackStep Step(string name) => catalogue.First(s => s.name == name);
		List<string> Missing(bool local, params LocalStackStep[] running) =>
			LocalStackDependencies.Missing(running, catalogue, stacks.TagsOf, local).Select(m => m.name).ToList();

		var gateway = Missing(true, Step("c# gateway"));
		Assert.That(gateway, Does.Contain(LocalStackDependencies.ApiDepsStep).And.Contain("scala: auth"));
		Assert.That(gateway, Has.None.StartsWith("build:"), "builds come with their steps");

		var scala = Missing(true, Step("scala: auth"));
		Assert.That(scala, Is.SupersetOf(new[] { "scala: redis", "scala: dbflake", "c# gateway", LocalStackDependencies.ApiDepsStep }));
		Assert.That(scala, Has.None.StartsWith("build:"), "builds come with their steps");

		var service = new LocalStackStep { name = LocalStackTemplate.MicroservicePrefix + "Stats", beam = true };
		var forService = Missing(true, service);
		Assert.That(forService, Does.Contain("c# gateway").And.Contain("scala: auth").And.Contain("scala: account"), "the services needed to log in");
		Assert.That(forService, Does.Not.Contain(LocalStackTemplate.PortalFrontendStepName), "a microservice isn't shown in the portal");
		Assert.That(Missing(false, service), Is.Empty, "against dev nothing local is needed");

		var extension = new LocalStackStep { name = LocalStackTemplate.ExtensionPrefix + "stats-page", dependsOn = new List<string> { "microservice: Stats" } };
		var forExtension = LocalStackDependencies.Missing(new[] { extension }, catalogue, stacks.TagsOf, false);
		Assert.That(forExtension.Single().name, Is.EqualTo("microservice: Stats"));
		Assert.That(forExtension.Single().step, Is.Null, "the stack has to define it");
		Assert.That(forExtension.Single().reason, Does.Contain("calls it"));
		Assert.That(Missing(true, extension), Does.Contain(LocalStackTemplate.PortalFrontendStepName));
	}

	/// <summary>
	/// Picking the C# hosts brings the Scala services they call: the gateway the login set and the services behind its
	/// requests, the workers the Scala gateway, the campaign runtime the message rail runtime.
	/// </summary>
	[Test]
	public void The_csharp_hosts_bring_the_scala_services_they_call()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var catalogue = StackCatalogue().steps;
		List<string> Missing(params string[] names) =>
			LocalStackDependencies.Missing(catalogue.Where(s => names.Contains(s.name)).ToList(), catalogue, stacks.TagsOf, true).Select(m => m.name).ToList();

		Assert.That(Missing("c# gateway"), Is.SupersetOf(new[] { "scala: auth", "scala: account", "scala: gateway", "scala: content", "scala: notification", "scala: session", "scala: stats" }));
		Assert.That(Missing("c# campaign runtime"), Does.Contain("scala: gateway").And.Contain("c# message rail runtime"));
		Assert.That(Missing("c# segmentation runtime"), Does.Contain("scala: notification").And.Contain("c# gateway"));
		Assert.That(Missing("c# analytics loader"), Does.Contain("c# gateway"));
	}

	/// <summary>The built-in stacks run everything their steps need.</summary>
	[Test]
	public void The_built_in_stacks_miss_nothing()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var config = StackCatalogue();
		foreach (var name in new[] { "full", "backend", "csharp-portal", "portal-extensions", "web-dev", "remote", "full:web" })
		{
			var resolved = stacks.Resolve(name, config);
			var missing = LocalStackDependencies.Missing(resolved.steps, config.steps, stacks.TagsOf, LocalStackTargets.RunsLocal(resolved.target, config));
			Assert.That(missing.Select(m => $"{m.name} ({m.reason})"), Is.Empty, name);
		}
	}

	/// <summary>Each built-in stack says which work it is for, then what it runs, like the shared ones.</summary>
	[Test]
	public void Built_in_descriptions_name_the_work_first()
	{
		foreach (var stack in LocalStackStacks.BuiltIn().List())
			Assert.That(stack.description, Does.Match(@"^[A-Z][^:]+ work[^:]*: "), stack.name);
	}

	/// <summary>
	/// A stack gets the builds of the steps it runs without naming them; `:no-build` drops them, and a build the
	/// stack excludes stays out.
	/// </summary>
	[Test]
	public void A_steps_build_comes_with_it()
	{
		var stacks = LocalStackStacks.BuiltIn();
		stacks.Add("gw", LocalStackLayer.Local, "gw.json", new LocalStackStackDef { include = new List<string> { "docker", "c# gateway", "portal frontend", "scala: auth" } });
		stacks.Add("gw-prebuilt", LocalStackLayer.Local, "gw-prebuilt.json", new LocalStackStackDef { extends = "gw", exclude = new List<string> { "build: c# gateway" } });
		var config = StackCatalogue();

		var names = StepNames(stacks.Resolve("gw", config));
		Assert.That(names, Does.Contain("build: c# gateway").And.Contain("build: portal deps").And.Contain("build: scala"));
		Assert.That(names, Does.Not.Contain("build: c# campaign runtime"));
		Assert.That(StepNames(stacks.Resolve("gw:no-build", config)), Has.None.StartsWith("build:"));
		Assert.That(StepNames(stacks.Resolve("gw-prebuilt", config)), Does.Not.Contain("build: c# gateway"));
	}

	/// <summary>A stack's own step runs where it says: first, after a named step, or last.</summary>
	[Test]
	public void A_stacks_own_steps_run_where_they_say()
	{
		var stacks = LocalStackStacks.BuiltIn();
		stacks.Add("mine", LocalStackLayer.Local, "mine.json", new LocalStackStackDef
		{
			include = new List<string> { "docker", "seed", "watch", "smoke" },
			steps = new Dictionary<string, LocalStackStep>
			{
				["seed"] = new LocalStackStep { shell = true, arguments = "echo", first = true },
				["watch"] = new LocalStackStep { shell = true, arguments = "echo", after = LocalStackDependencies.ApiDepsStep },
				["smoke"] = new LocalStackStep { shell = true, arguments = "echo" },
			}
		});

		var names = StepNames(stacks.Resolve("mine", StackCatalogue()));
		Assert.That(names.First(), Is.EqualTo("seed"));
		Assert.That(names.IndexOf("watch"), Is.EqualTo(names.IndexOf(LocalStackDependencies.ApiDepsStep) + 1));
		Assert.That(names.Last(), Is.EqualTo("smoke"));
	}

	/// <summary>
	/// Each row of the service picker is one line that fits the terminal: a group says how many microservices and
	/// extensions it runs instead of naming them, an extension's calls are cut to fit.
	/// </summary>
	[Test]
	public void Service_picker_rows_fit_on_one_line()
	{
		var members = Enumerable.Range(1, 20).Select(i => $"PlayerEngagementService{i}").Concat(Enumerable.Range(1, 30).Select(i => $"core-development-extension-{i}")).ToArray();
		var workspace = new WorkspaceServices
		{
			services = members.Take(20).ToList(),
			extensions = members.Skip(20).ToList(),
			groups = new Dictionary<string, string[]> { ["player-engagement"] = members },
			calls = new Dictionary<string, List<string>> { ["core-development-extension-1"] = members.Take(20).ToList() }
		};
		string Text(string key, int width) => Spectre.Console.Markup.Remove(LocalStackCreateCommand.ServiceRow(workspace, key, width));

		Assert.That(Text(LocalStackCreateCommand.GroupKey + "player-engagement", 80), Is.EqualTo("player-engagement  20 microservices, 30 extensions"));
		foreach (var width in new[] { 40, 80, 120 })
		{
			var calls = Text(LocalStackCreateCommand.ExtensionKey + "core-development-extension-1", width);
			Assert.That(calls.Length, Is.LessThanOrEqualTo(width), $"{width}: {calls}");
			Assert.That(calls, Does.StartWith("core-development-extension-1  calls P"));
			Assert.That(calls, Does.Not.Contain("\n"));
		}

		Assert.That(Text(LocalStackCreateCommand.ServiceKey + "PlayerEngagementService1", 80), Is.EqualTo("PlayerEngagementService1"));
	}

	/// <summary>A microservice an extension's package.json names but this workspace doesn't have isn't one it calls here.</summary>
	[Test]
	public void An_extension_calls_only_the_workspaces_microservices()
	{
		var workspace = new WorkspaceServices
		{
			services = new List<string> { "CampaignService" },
			calls = new Dictionary<string, List<string>> { ["campaigns"] = new List<string> { "CampaignServiceZone", "campaignservice" } }
		};

		Assert.That(workspace.CallsOf("campaigns"), Is.EqualTo(new[] { "campaignservice" }));
	}

	/// <summary>
	/// The review fits a short screen: the choices keep three rows and page, the rest of the screen shows what fits,
	/// and the last line shown says how many were left out. A tall screen shows everything and every choice.
	/// </summary>
	[Test]
	public void The_review_fits_the_screen()
	{
		var lines = Enumerable.Range(1, 20).Select(i => $"line {i}").ToList();

		var (shown, pageSize) = LocalStackCreateCommand.FitReview(lines, 12, 15);
		Assert.That(pageSize, Is.EqualTo(3));
		Assert.That(shown, Has.Count.EqualTo(15 - 5 - 3));
		Assert.That(shown[^1], Does.Contain("14 more lines"));

		(shown, pageSize) = LocalStackCreateCommand.FitReview(lines, 12, 60);
		Assert.That((shown.Count, pageSize), Is.EqualTo((20, 12)));
	}

	/// <summary>A program typed with its arguments is split into the program and the arguments, the typed ones first.</summary>
	[Test]
	public void A_program_typed_with_its_arguments_is_split()
	{
		Assert.That(LocalStackCustomSteps.SplitProgram("echo \"Hello\"", ""), Is.EqualTo(("echo", "\"Hello\"")));
		Assert.That(LocalStackCustomSteps.SplitProgram("npx vite", "--port 5173"), Is.EqualTo(("npx", "vite --port 5173")));
		Assert.That(LocalStackCustomSteps.SplitProgram("\"C:\\Program Files\\node\\node.exe\" app.js", null), Is.EqualTo(("C:\\Program Files\\node\\node.exe", "app.js")));
		Assert.That(LocalStackCustomSteps.SplitProgram("node", "app.js"), Is.EqualTo(("node", "app.js")));
	}

	/// <summary>The review sums a big stack up in one line per kind that fits the screen, builds counted apart.</summary>
	[Test]
	public void Review_sums_a_big_stack_up_to_fit_the_screen()
	{
		var stacks = LocalStackStacks.BuiltIn();
		var steps = stacks.Resolve("full", StackCatalogue()).steps;
		var lines = LocalStackCreateCommand.StepSummary(stacks, steps, 60).Select(Spectre.Console.Markup.Remove).ToList();

		Assert.That(lines.Count, Is.LessThanOrEqualTo(8), string.Join("\n", lines));
		Assert.That(lines, Has.All.Length.LessThanOrEqualTo(60));
		Assert.That(lines[0], Does.Match(@"It runs \d+ steps \(\+\d+ builds when needed\):"));
		Assert.That(lines, Has.Some.Contains("Scala services (").And.Some.Contains("C# hosts ("));
	}
}
