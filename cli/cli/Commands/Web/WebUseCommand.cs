using Beamable.Common;
using Beamable.Server;
using cli.Portal;
using cli.Services;
using cli.Services.Web;
using cli.Utils;
using Newtonsoft.Json.Linq;
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

	/// <summary>
	/// Projects whose pin was rewritten but whose install failed. These are NOT usable: the extension will not
	/// build or start. Reported separately because a project appearing in <see cref="updated"/> used to be the
	/// only signal, which made a run where every install failed look completely successful.
	/// </summary>
	public List<string> failed = new List<string>();
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

		// One packument read for the whole run: the integrity the registry is currently serving for the
		// version being pinned. Null when it cannot be determined (unreachable registry, unexpected shape),
		// which simply disables the skip and restores the previous always-reinstall behaviour.
		var registryIntegrity = await ResolveRegistryIntegrity(args, registry, version);
		var alreadyCurrent = 0;

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

			results.updated.Add(new WebUsedProject
			{
				name = name,
				directory = directory,
				previousVersion = previousVersion
			});

			// Deliberately NO "already pins this version, skip" shortcut. The version is a fixed sentinel, so
			// after the first run every project already pins it — skipping on that basis would skip everything
			// and never deliver a new build. The pin edit is a no-op from then on; the reinstall is the point.
			//
			// What CAN be skipped is a project whose installed copy is already byte-identical to what the
			// registry is serving right now. That keys on content, not on the version string, so it stays
            // correct under a republished sentinel: the moment `beam web publish` changes the tarball, the
			// integrity changes, every project misses, and every project reinstalls exactly as before.
			// Without this the step reinstalls ~70 projects on every single `beam local up` to deliver a
			// build that is, in the overwhelmingly common case, already there.
			if (registryIntegrity != null
				&& registryIntegrity == WebLocalRegistryService.InstalledIntegrity(directory, WebLocalRegistryService.ToolkitPackage))
			{
				Log.Verbose($"[{name}] already has {WebLocalRegistryService.ToolkitPackage}@{version} " +
					"with the integrity the registry is serving - no reinstall needed");
				alreadyCurrent++;
				continue;
			}

			directoriesToInstall.Add((name, directory));
		}

		var installFailures = new List<string>();
		if (!args.SkipInstall)
		{
			installFailures = RunInstalls(directoriesToInstall, version, registry);
		}

		if (installFailures.Count > 0)
		{
			// Previously this was only a per-project warning while the project was still reported as "updated",
			// so a run whose installs ALL failed looked successful — and the extension then failed to start with
			// no visible cause. Say it plainly instead.
			results.failed = installFailures;

			// Exit NON-ZERO. `beam local up` runs this step on every start (it is in the forced-web set, not
			// gated on --build) and treats a non-zero build step as fatal — which is exactly the signal wanted
			// here. Exiting 0 with a warning is what made a broken install invisible: the step "succeeded", the
			// stack came up, and the extensions silently never started with no error anywhere to point at.
			// Every other build step (mvn, npm install) already aborts the stack this way; this was the anomaly.
			throw new CliException(
				$"Repointed the pins, but the install FAILED in {installFailures.Count} project(s): " +
				$"{string.Join(", ", installFailures)}.\n" +
				"Those extensions cannot build or start. Re-run with --logs v to see npm's output, or use " +
				"--skip-install to rewrite the pins without installing.");
		}

		Log.Information($"Updated {results.updated.Count} project(s); skipped {results.skipped.Count}" +
			$"; already current {alreadyCurrent}" +
			(installFailures.Count > 0 ? $"; install failed in {installFailures.Count}." : "."));
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
	/// The <c>dist.integrity</c> the registry currently serves for <paramref name="version"/>.
	///
	/// <para>
	/// Returns null on any failure rather than throwing. This drives an optimisation, not correctness: a
	/// null just means every project reinstalls, which is what happened before this existed.
	/// </para>
	/// </summary>
	private static async Task<string> ResolveRegistryIntegrity(WebUseCommandArgs args, string registry, string version)
	{
		try
		{
			var versionService = args.Provider.GetService<VersionService>();
			var packument = await versionService.GetNpmPackument(
				WebLocalRegistryService.ToolkitPackage, registry, throwOnError: false);

			if (packument?.Versions == null || !packument.Versions.TryGetValue(version, out var entry))
			{
				return null;
			}

			return (entry as JObject)?["dist"]?["integrity"]?.ToString();
		}
		catch (Exception e)
		{
			Log.Verbose($"Could not read the registry integrity for {WebLocalRegistryService.ToolkitPackage}@{version}: {e.Message}");
			return null;
		}
	}

	/// <summary>
	/// Installs in each updated project, bounded by processor count. Each has its own node_modules so the
	/// installs are independent; only npm's cache is shared, and npm locks that itself.
	/// </summary>
	private static List<string> RunInstalls(List<(string name, string directory)> directories, string version, string registry)
	{
		var failures = new System.Collections.Concurrent.ConcurrentBag<string>();
		if (directories.Count == 0)
		{
			return new List<string>();
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
						failures.Add(target.name);
						Log.Warning($"Repointed [{target.name}], but installing {WebLocalRegistryService.ToolkitPackage}@{version} " +
							$"failed there. Run it manually to resolve packages. Errors: \n{error}");
					}
				}
				catch (Exception e)
				{
					failures.Add(target.name);
					Log.Warning($"Repointed [{target.name}], but the install could not be run: {e.Message}");
				}
			});

		return failures.ToList();
	}
}
