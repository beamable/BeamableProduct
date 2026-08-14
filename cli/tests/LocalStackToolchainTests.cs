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
	public void InitTargetsADotBeamableFolderWhenThereIsNoWorkspace()
	{
		// A loose local-stack.json in the current directory is not a workspace as far as the rest of the CLI is
		// concerned, so `init` targets `.beamable/` and creates it.
		var expected = Path.GetFullPath(Path.Combine(
			Directory.GetCurrentDirectory(), ".beamable", "local-stack.json"));

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
}
