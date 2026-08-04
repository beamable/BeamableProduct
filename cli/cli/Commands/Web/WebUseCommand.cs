using Beamable.Common;
using Beamable.Server;
using cli.Portal;
using cli.Services;
using cli.Services.Web;
using cli.Utils;
using System.CommandLine;

namespace cli.Web;

public class WebUseCommandArgs : CommandArgs
{
	public string Workspace;
	public string Registry;
	public string Version;
	public bool SkipInstall;
}

public class WebUsedProject
{
	public string name;
	public string directory;
	public string previousVersion;
}

public class WebUseCommandResults
{
	/// <summary>The version every updated project now pins.</summary>
	public string version;
	public List<WebUsedProject> updated = new List<WebUsedProject>();
	public List<string> skipped = new List<string>();
}

/// <summary>
/// Repoints the portal extensions in a directory tree at a locally published build of
/// <c>@beamable/portal-toolkit</c>, then installs it.
///
/// <para>
/// This is the offline counterpart to <c>beam portal extension update-toolkit --local</c>. That command
/// discovers extensions through the Beamo service manifest, which makes it authenticate against the
/// configured host first — so it fails outright when the backend isn't running, even though rewriting a
/// version pin is a purely local file edit. This command scans the filesystem instead and needs no
/// workspace, manifest or network beyond the local registry itself.
/// </para>
/// <para>
/// It edits TRACKED files. That is inherent to the incrementing-version model — the pin has to name the
/// new build — but it must not be committed; see web/LOCAL_DEV.md for the revert step.
/// </para>
/// </summary>
public class WebUseCommand : AtomicCommand<WebUseCommandArgs, WebUseCommandResults>, IStandaloneCommand, ISkipManifest
{
	public WebUseCommand() : base("use",
		"Point the portal extensions in a directory at a locally published build of the Portal Toolkit and install it")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--workspace", "Directory tree to scan for portal extensions; defaults to the working directory"),
			(args, i) => args.Workspace = i);
		AddOption(new Option<string>("--registry", () => WebLocalRegistryService.DefaultRegistry, "The local npm registry holding the build"),
			(args, i) => args.Registry = i);
		AddOption(new Option<string>("--version", "The version to pin; defaults to the newest local build, from the registry's 'local' dist-tag"),
			(args, i) => args.Version = i);
		AddOption(new Option<bool>("--skip-install", "Rewrite the pins without running npm install"),
			(args, i) => args.SkipInstall = i);
	}

	public override async Task<WebUseCommandResults> GetResult(WebUseCommandArgs args)
	{
		var registry = string.IsNullOrEmpty(args.Registry) ? WebLocalRegistryService.DefaultRegistry : args.Registry;
		var workspace = string.IsNullOrEmpty(args.Workspace)
			? Directory.GetCurrentDirectory()
			: Path.GetFullPath(args.Workspace);

		if (!Directory.Exists(workspace))
		{
			throw new CliException($"--workspace [{workspace}] does not exist");
		}

		var version = await ResolveVersion(args, registry);
		var results = new WebUseCommandResults { version = version };

		var projects = WebLocalRegistryService.FindExtensionProjects(workspace);
		if (projects.Count == 0)
		{
			throw new CliException(
				$"Found no portal extensions or extension libraries under [{workspace}]. " +
				"Pass --workspace pointing at the repository that holds them");
		}

		Log.Information($"Pointing {projects.Count} project(s) under [{workspace}] at {WebLocalRegistryService.ToolkitPackage}@{version}");

		var directoriesToInstall = new List<(string name, string directory)>();
		foreach (var (name, packageJsonPath) in projects)
		{
			string previousVersion;
			bool found;
			try
			{
				// Reuse the pure rewriter from update-toolkit — same edit, same dependency blocks.
				found = PortalExtensionUpdateToolkitCommand.RewriteToolkitVersion(packageJsonPath, version, out previousVersion);
			}
			catch (Exception e)
			{
				throw new CliException($"Could not update {WebLocalRegistryService.ToolkitPackage} in [{name}]. Message = [{e.Message}]");
			}

			if (!found)
			{
				Log.Verbose($"[{name}] has no {WebLocalRegistryService.ToolkitPackage} dependency - skipping");
				results.skipped.Add(name);
				continue;
			}

			var directory = Path.GetDirectoryName(packageJsonPath);

			// Deliberately NO "already pins this version, skip" shortcut. The version is fixed, so after the
			// first run every project already pins it — skipping on that basis would skip everything and
			// never deliver a new build. The pin edit is a no-op from then on; the reinstall is the point.
			results.updated.Add(new WebUsedProject
			{
				name = name,
				directory = directory,
				previousVersion = previousVersion
			});
			directoriesToInstall.Add((name, directory));
		}

		if (!args.SkipInstall)
		{
			RunInstalls(directoriesToInstall, version, registry);
		}

		Log.Information($"Updated {results.updated.Count} project(s); skipped {results.skipped.Count}.");
		if (results.updated.Count > 0)
		{
			Log.Warning("These are tracked files. Before committing, run: " +
				"git restore '**/package.json' '**/package-lock.json'");
		}

		return results;
	}

	/// <summary>
	/// The version to pin: explicit <c>--version</c>, else whatever the registry's <c>local</c> dist-tag
	/// points at — which <c>beam web publish</c> moves on every publish, so this always picks up the build
	/// that was just made.
	/// </summary>
	private async Task<string> ResolveVersion(WebUseCommandArgs args, string registry)
	{
		if (!string.IsNullOrEmpty(args.Version))
		{
			return args.Version;
		}

		var versionService = args.Provider.GetService<VersionService>();
		var packument = await versionService.GetNpmPackument(WebLocalRegistryService.ToolkitPackage, registry, throwOnError: false);
		if (packument?.DistTags == null
			|| !packument.DistTags.TryGetValue(WebLocalRegistryService.LocalDistTag, out var local)
			|| string.IsNullOrEmpty(local))
		{
			throw new CliException(
				$"No local build of {WebLocalRegistryService.ToolkitPackage} found on [{registry}]. " +
				"Publish one first with 'beam web publish' (or ./dev-web.sh), or pass --version");
		}

		return local;
	}

	/// <summary>
	/// Installs in each updated project, bounded by processor count. Each has its own node_modules so the
	/// installs are independent; only npm's cache is shared, and npm locks that itself.
	/// </summary>
	private static void RunInstalls(List<(string name, string directory)> directories, string version, string registry)
	{
		if (directories.Count == 0)
		{
			return;
		}

		Log.Information($"Installing {WebLocalRegistryService.ToolkitPackage}@{version} in {directories.Count} project(s) from [{registry}]");

		Parallel.ForEach(directories,
			new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount) },
			target =>
			{
				try
				{
					// ForceReinstall, not a plain install: the version is already pinned and already present,
					// so npm would consider the tree satisfied and never fetch the build we just published.
					if (!WebLocalRegistryService.ForceReinstall(
							target.directory, WebLocalRegistryService.ToolkitPackage, version, registry, out var error))
					{
						// Best-effort, as in update-toolkit: package.json is the source of truth and the run
						// flow installs again before building, so a failed install only warns.
						Log.Warning($"Repointed [{target.name}], but installing {WebLocalRegistryService.ToolkitPackage}@{version} " +
							$"failed there. Run it manually to resolve packages. Errors: \n{error}");
					}
				}
				catch (Exception e)
				{
					Log.Warning($"Repointed [{target.name}], but the install could not be run: {e.Message}");
				}
			});
	}
}
