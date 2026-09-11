using Beamable.Common;
using Beamable.Server;
using cli.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace cli.Services.Web;

/// <summary>
/// Shared plumbing for the local web-package dev loop: a Verdaccio registry (default :4873) that holds
/// locally-built copies of <c>@beamable/sdk</c> and <c>@beamable/portal-toolkit</c>, fronted by a
/// local-unpkg file server (default :4874) so the Portal can fetch their IIFEs by path.
///
/// <para>
/// The loop works by <b>shadowing</b>: a local build is published under the SAME version consumers
/// already pin (e.g. <c>0.4.0</c>), so no package.json, lockfile or extension manifest is ever edited.
/// Two consequences drive the code here:
/// </para>
/// <list type="number">
/// <item>
/// Verdaccio refuses to publish a version it already has, so a republish must unpublish first. This is
/// also why <c>@beamable/*</c> is deliberately not proxied to npmjs in
/// <c>portal-localdev/verdaccio/config.yml</c> — with a proxy, the upstream version is already in the
/// packument, so the first publish conflicts and unpublishing it tombstones the upstream copy too.
/// </item>
/// <item>
/// A committed lockfile pins <c>resolved</c> to registry.npmjs.org, and npm honours that URL. So merely
/// pointing a registry at Verdaccio silently installs the REMOTE package. The only reliable way in is
/// an explicit <c>npm install &lt;name&gt;@&lt;version&gt;</c> with <c>--no-save</c>, which is what
/// <see cref="TryShadowInstall"/> does.
/// </item>
/// </list>
/// </summary>
public class WebLocalRegistryService
{
	public const string DefaultRegistry = "http://localhost:4873";
	public const string DefaultCdn = "http://localhost:4874";

	/// <summary>The public npm registry a released <c>@beamable</c> pin must always resolve against.</summary>
	public const string PublicRegistry = "https://registry.npmjs.org/";

	/// <summary>Directory inside the product repo holding the registry's docker-compose file.</summary>
	public const string LocaldevDirName = "portal-localdev";

	/// <summary>
	/// The version prefix marking a local developer build, matching the sentinel <c>dev.sh</c> uses for the
	/// .NET packages. The Portal string-matches this prefix to route a package at the local CDN instead of
	/// unpkg.com (see <c>extensionSdkRegistry.ts</c>), so it must stay in sync with that constant.
	/// </summary>
	public const string LocalDevPrefix = "0.0.123";

	/// <summary>
	/// The single version every local build is published as.
	///
	/// <para>
	/// Deliberately constant rather than a counter: consumers pin it, so a moving version would mean
	/// rewriting every extension's <c>package.json</c> on every publish. Holding it still means the pin is
	/// written once, and "is this the latest build?" becomes a cache-invalidation problem instead — see
	/// <see cref="ForceReinstall"/> and the CDN flush in <c>beam web publish</c>.
	/// </para>
	/// <para>
	/// Safe to reuse because it exists nowhere upstream (verified 404 on npmjs for both packages), so it can
	/// never collide with the registry's npmjs uplink, and removing it can't tombstone a published version.
	/// </para>
	/// </summary>
	public const string LocalDevVersion = LocalDevPrefix;

	/// <summary>
	/// Whether a dependency spec points at a local developer build.
	///
	/// <para>
	/// Leading semver range operators are trimmed first, so <c>^0.0.123</c> and <c>~0.0.123</c> count as
	/// well as a bare <c>0.0.123</c>. That matters in practice: npm's default save-prefix rewrites an exact
	/// install to a caret range, and people hand-write ranges too — without this the pin would silently stop
	/// being recognised, and installs would no longer be routed at the local registry.
	/// </para>
	/// </summary>
	public static bool IsLocalDevVersion(string version) =>
		!string.IsNullOrEmpty(version)
		&& version.TrimStart('^', '~', '>', '<', '=', 'v', ' ').StartsWith(LocalDevPrefix, StringComparison.Ordinal);

	/// <summary>
	/// Installs <paramref name="package"/>@<paramref name="version"/> from the local registry, forcing a
	/// fresh fetch even when that version is already installed.
	///
	/// <para>
	/// The existing copy is deleted first, and this is the crux of the fixed-version model: npm sees the
	/// pinned version already present, considers the tree satisfied, and no-ops — so a republished build
	/// would never arrive. Removing the directory and asking for the explicit spec makes npm re-resolve.
	/// </para>
	/// <para>
	/// Deliberately NOT <c>--no-save</c>: the lock file records an integrity hash for the old tarball, and
	/// letting the install rewrite it keeps the lock consistent with what is on disk.
	/// </para>
	/// </summary>
	public static bool ForceReinstall(string directory, string package, string version, string registryUrl, out string error)
	{
		error = null;

		var installedPath = Path.Combine(directory, "node_modules", package.Replace("/", Path.DirectorySeparatorChar.ToString()));
		try
		{
			if (Directory.Exists(installedPath))
			{
				Directory.Delete(installedPath, recursive: true);
			}
		}
		catch (Exception e)
		{
			error = $"Could not remove [{installedPath}]: {e.Message}";
			return false;
		}

		// --save-exact so the manifest keeps the literal version rather than npm's default caret range.
		// (IsLocalDevVersion tolerates a range anyway, but an exact pin is what we asked for.)
		var args = $"install {package}@{version} --registry {registryUrl} --save-exact --no-audit --no-fund {AuthTokenFlag(registryUrl)}";
		var result = StartProcessUtil.Run("npm", args, useShell: true, workingDirectoryPath: directory).WaitForResult();
		if (result.exit == 0)
		{
			return true;
		}

		var output = string.IsNullOrWhiteSpace(result.stderr) ? result.stdout : result.stderr;

		// EINTEGRITY: the lockfile pins a sha512 for this version that no longer matches what the registry
		// serves. That is GUARANTEED to happen in this flow — the local-dev version is a fixed sentinel that
		// every `beam web publish` republishes with different content, so any committed lockfile goes stale the
		// moment the packages are rebuilt. npm will not resolve past it, and deleting node_modules does not help
		// because the bad hash lives in the lockfile.
		//
		// Drop the lockfile and retry once. It is regenerated by this install, and `web use` already warns that
		// package.json/package-lock.json are tracked files to restore before committing.
		if (!IsIntegrityFailure(output))
		{
			error = output;
			return false;
		}

		var lockFile = Path.Combine(directory, "package-lock.json");
		try
		{
			if (File.Exists(lockFile))
			{
				Log.Verbose($"[{directory}] npm reported EINTEGRITY against the local registry; " +
					"removing the stale package-lock.json and retrying.");
				File.Delete(lockFile);
			}
			else
			{
				// No lockfile to blame — the mismatch is with something else, so report the original failure.
				error = output;
				return false;
			}
		}
		catch (Exception e)
		{
			error = $"{output}\n(could not remove the stale lockfile at {lockFile}: {e.Message})";
			return false;
		}

		var retry = StartProcessUtil.Run("npm", args, useShell: true, workingDirectoryPath: directory).WaitForResult();
		if (retry.exit != 0)
		{
			error = string.IsNullOrWhiteSpace(retry.stderr) ? retry.stdout : retry.stderr;
			return false;
		}

		return true;
	}

	/// <summary>
	/// True when npm failed because a lockfile integrity hash did not match what the registry served.
	/// </summary>
	private static bool IsIntegrityFailure(string output) =>
		!string.IsNullOrEmpty(output)
		&& (output.Contains("EINTEGRITY", StringComparison.OrdinalIgnoreCase)
		    || output.Contains("integrity checksum failed", StringComparison.OrdinalIgnoreCase));

	public const string SdkPackage = "@beamable/sdk";
	public const string ToolkitPackage = "@beamable/portal-toolkit";

	/// <summary>The dist-tag local builds are published under. `beam portal extension update-toolkit --local` reads it.</summary>
	public const string LocalDistTag = "local";

	/// <summary>Dependency blocks searched when reading the version a project pins.</summary>
	private static readonly string[] DependencyBlocks = { "dependencies", "devDependencies", "peerDependencies" };

	private readonly VersionService _versionService;

	public WebLocalRegistryService(VersionService versionService)
	{
		_versionService = versionService;
	}

	/// <summary>
	/// The versions of <paramref name="package"/> published to the local registry; empty when none are.
	/// Because <c>@beamable/*</c> is not proxied, everything the packument reports for these packages was
	/// published locally — there is no upstream to filter out.
	/// <para>
	/// Callers must check <see cref="IsRegistryReachable"/> first: an un-proxied package that was never
	/// published locally answers 404, which is indistinguishable here from an unreachable registry — and
	/// on an empty registry the 404 is the normal case, not an error.
	/// </para>
	/// </summary>
	public async Task<HashSet<string>> GetLocallyPublishedVersions(string package, string registryUrl)
	{
		var packument = await _versionService.GetNpmPackument(package, registryUrl, throwOnError: false);
		return packument?.Versions == null
			? new HashSet<string>(StringComparer.Ordinal)
			: new HashSet<string>(packument.Versions.Keys, StringComparer.Ordinal);
	}

	/// <summary>
	/// Whether the local registry is listening. Any HTTP response counts, including a 4xx — the point is
	/// to separate "the registry isn't running" (fall back to npm) from "the registry is running but has
	/// nothing published yet" (the local flow is available, there is just nothing to link).
	/// </summary>
	public async Task<bool> IsRegistryReachable(string registryUrl)
	{
		try
		{
			using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
			await client.GetAsync(registryUrl);
			return true;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Reads the version a package.json pins for <paramref name="package"/>, searching dependencies,
	/// devDependencies then peerDependencies. Returns null when the file doesn't reference it.
	/// Kept pure so it can be unit-tested without npm or a registry.
	/// </summary>
	public static string ReadPinnedVersion(string packageJsonPath, string package)
	{
		if (!File.Exists(packageJsonPath))
		{
			return null;
		}

		var root = JObject.Parse(File.ReadAllText(packageJsonPath));
		foreach (var block in DependencyBlocks)
		{
			if (root[block] is JObject dependencies && dependencies[package] != null)
			{
				return dependencies[package].ToString();
			}
		}

		return null;
	}

	/// <summary>
	/// The npm arguments needed to install a project's <c>@beamable/portal-toolkit</c> pin, chosen from the
	/// pinned version:
	/// <list type="bullet">
	/// <item>
	/// A local developer build (<c>0.0.123-*</c>) exists only on the local registry, so the whole install is
	/// routed there with <c>--registry &lt;local&gt;</c> plus its auth token. Required, not an optimisation:
	/// a plain <c>npm install</c> would resolve it against npmjs, 404, and fail the build. Routing everything
	/// at the local registry is fine because it proxies the rest to npmjs (see
	/// <c>portal-localdev/verdaccio/config.yml</c>).
	/// </item>
	/// <item>
	/// A released build pins the <c>@beamable</c> scope at the public npm registry with
	/// <c>--@beamable:registry=&lt;public&gt;</c>. A user's machine may have a corporate proxy or a private
	/// registry configured for <c>@beamable</c> in their npmrc that has never heard of the package, so pinning
	/// the scope to npmjs keeps a normal install from failing there.
	/// </item>
	/// </list>
	/// </summary>
	public static string InstallArgsFor(string projectDir, string registryUrl = DefaultRegistry)
	{
		var pinned = ReadPinnedVersion(Path.Combine(projectDir, "package.json"), ToolkitPackage);
		if (!IsLocalDevVersion(pinned))
		{
			Log.Verbose($"[{projectDir}] pins the released {ToolkitPackage}@{pinned}; forcing the @beamable scope at [{PublicRegistry}]");
			return $" --@beamable:registry={PublicRegistry}";
		}

		Log.Verbose($"[{projectDir}] pins the local build {ToolkitPackage}@{pinned}; installing from [{registryUrl}]");
		return $" --registry {registryUrl} {AuthTokenFlag(registryUrl)}";
	}

	/// <summary>Reads the <c>version</c> field of a package.json. Throws when the file is missing or has none.</summary>
	public static string ReadOwnVersion(string packageJsonPath)
	{
		if (!File.Exists(packageJsonPath))
		{
			throw new CliException($"No package.json at [{packageJsonPath}]");
		}

		var version = JObject.Parse(File.ReadAllText(packageJsonPath))["version"]?.ToString();
		if (string.IsNullOrEmpty(version))
		{
			throw new CliException($"package.json at [{packageJsonPath}] has no version field");
		}

		return version;
	}

	/// <summary>
	/// Clears the local-unpkg in-memory file cache. Rarely needed now that every publish uses a fresh
	/// version (a new version is a new cache key), but it still matters when republishing over a version
	/// with <c>--version</c>. Best-effort: a missing CDN is not an error, it just means nothing is serving
	/// files yet.
	/// </summary>
	public async Task FlushCdnCache(string cdnUrl)
	{
		try
		{
			using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
			var response = await client.PostAsync($"{cdnUrl.TrimEnd('/')}/__flush", null);
			if (response.IsSuccessStatusCode)
			{
				Log.Verbose($"Flushed the local CDN cache at [{cdnUrl}]");
				return;
			}

			Log.Warning($"The local CDN at [{cdnUrl}] rejected the cache flush (status {(int)response.StatusCode}). " +
				"If it is running an older image, rebuild it with 'docker compose up -d --build' in portal-localdev.");
		}
		catch (Exception e)
		{
			Log.Warning($"Could not flush the local CDN cache at [{cdnUrl}]: {e.Message}. " +
				"Files already fetched may be stale until it restarts.");
		}
	}

	/// <summary>
	/// npm refuses to publish without a token even when the registry allows anonymous writes, so every
	/// write passes one as a CLI flag. Doing it this way keeps the user's global npmrc untouched — the
	/// old setup-web.sh/teardown-web.sh pair mutated it, which is what broke installs machine-wide when
	/// the registry went away.
	/// </summary>
	public static string AuthTokenFlag(string registryUrl)
	{
		// npm keys auth config by the registry URL with the scheme stripped, e.g. //localhost:4873/:_authToken
		var withoutScheme = registryUrl.Replace("https://", string.Empty).Replace("http://", string.Empty).TrimEnd('/');
		return $"\"--//{withoutScheme}/:_authToken={LocalDistTag}\"";
	}

	/// <summary>
	/// Finds the BeamableProduct checkout that holds the web packages, by walking up from
	/// <paramref name="startDir"/> looking for the <c>web</c> + <c>beam-portal-toolkit</c> pair.
	/// Returns null when not found.
	/// </summary>
	public static string FindProductDir(string startDir, int maxLevels = 4)
	{
		var dir = new DirectoryInfo(startDir);
		for (var i = 0; i <= maxLevels && dir != null; i++, dir = dir.Parent)
		{
			if (LooksLikeProductDir(dir.FullName))
			{
				return dir.FullName;
			}

			// Also check siblings one level down, so running from a game workspace next to the product repo works.
			foreach (var child in dir.GetDirectories())
			{
				if (LooksLikeProductDir(child.FullName))
				{
					return child.FullName;
				}
			}
		}

		return null;
	}

	private static bool LooksLikeProductDir(string dir)
	{
		return File.Exists(Path.Combine(dir, "web", "package.json"))
			&& File.Exists(Path.Combine(dir, "beam-portal-toolkit", "package.json"));
	}

	/// <summary>
	/// The <c>web/</c> and <c>beam-portal-toolkit/</c> directories every web-registry step depends on. A missing
	/// entry here is what <see cref="MissingProductDirMarkers"/> reports and what
	/// <see cref="EnsureProductDirIntact"/> tries to restore.
	/// </summary>
	private static readonly string[] ProductDirMarkerDirs = { "web", "beam-portal-toolkit" };

	/// <summary>
	/// Names any <see cref="ProductDirMarkerDirs"/> whose <c>package.json</c> is missing under
	/// <paramref name="productDir"/>. Empty when the checkout is intact.
	/// </summary>
	public static IReadOnlyList<string> MissingProductDirMarkers(string productDir)
	{
		var missing = new List<string>();
		if (string.IsNullOrEmpty(productDir) || !Directory.Exists(productDir))
		{
			return missing;
		}
		foreach (var name in ProductDirMarkerDirs)
		{
			if (!File.Exists(Path.Combine(productDir, name, "package.json")))
			{
				missing.Add(name);
			}
		}
		return missing;
	}

	/// <summary>
	/// Preflight for <c>beam local up</c> and any direct <c>beam web publish</c>: when the recorded
	/// <paramref name="productDir"/> is a git checkout that has had <c>web/</c> or <c>beam-portal-toolkit/</c>
	/// wiped from the working tree, restore them from HEAD instead of letting <c>beam web publish</c> fail
	/// minutes into the run — the stack has spun up 20+ services by then and cascades a full shutdown.
	///
	/// Silent no-op on an intact checkout, so the healthy path pays only a couple of <c>File.Exists</c> calls.
	///
	/// Restore, not report, because every file under the missing directory is tracked-in-index with the working
	/// copy deleted, so <c>git restore</c> is a pure re-materialisation — nothing local can be overwritten. If
	/// the directory is not a git repo (a zip download), or git is not on <c>PATH</c>, or the restore returns
	/// non-zero, throw a <see cref="CliException"/> naming the exact command to run by hand. Never fail silently:
	/// a missing marker guarantees a downstream failure, and the point of this preflight is to name the fix.
	/// </summary>
	public static void EnsureProductDirIntact(string productDir)
	{
		if (string.IsNullOrWhiteSpace(productDir)
		    || productDir.Contains(cli.Services.LocalStack.LocalStackConfigIO.EditPlaceholder, StringComparison.Ordinal)
		    || !Directory.Exists(productDir))
		{
			return;
		}

		var missing = MissingProductDirMarkers(productDir);
		if (missing.Count == 0)
		{
			return;
		}

		var isGitRepo = Directory.Exists(Path.Combine(productDir, ".git"))
		                || File.Exists(Path.Combine(productDir, ".git"));
		if (!isGitRepo)
		{
			throw new CliException(BuildIncompleteCheckoutMessage(productDir, missing,
				gitAvailable: false, gitMessage: null));
		}

		string gitError = null;
		foreach (var name in missing)
		{
			// `git restore` succeeds silently on a tracked path that was deleted only in the working tree; that
			// is the exact state the bug leaves the repo in.
			var (exit, _, stderr) = TryRunGit(productDir, "restore", "--", name + "/");
			if (exit != 0)
			{
				gitError = string.IsNullOrWhiteSpace(stderr)
					? $"git restore -- {name}/ exited with {exit}"
					: stderr.Trim();
				break;
			}
		}

		var stillMissing = MissingProductDirMarkers(productDir);
		if (stillMissing.Count == 0)
		{
			Log.Warning($"Restored {string.Join(", ", missing.Select(m => m + "/"))} in [{productDir}] from git — they were deleted from the working tree.");
			return;
		}

		throw new CliException(BuildIncompleteCheckoutMessage(productDir, stillMissing,
			gitAvailable: true, gitMessage: gitError));
	}

	private static string BuildIncompleteCheckoutMessage(string productDir, IReadOnlyList<string> missing,
		bool gitAvailable, string gitMessage)
	{
		var missingList = string.Join(", ", missing.Select(m => m + "/"));
		var head = $"BeamableProduct checkout at [{productDir}] is missing {missingList} — `beam web publish` cannot run without them.";
		if (!gitAvailable)
		{
			return head + " That directory is not a git checkout, so nothing here can restore the missing files. " +
			       "Re-clone BeamableProduct, or pass --product-dir at a complete checkout.";
		}
		var why = string.IsNullOrEmpty(gitMessage) ? string.Empty : $" git said: {gitMessage}.";
		var restoreArgs = string.Join(" ", missing.Select(m => m + "/"));
		return head + $" Tried `git restore` but the working tree is still missing them.{why} " +
		       $"Fix by hand from the repo root: `git -C \"{productDir}\" restore {restoreArgs}`.";
	}

	private static (int exit, string stdout, string stderr) TryRunGit(string workingDir, params string[] args)
	{
		try
		{
			var psi = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "git",
				WorkingDirectory = workingDir,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
			foreach (var a in args)
			{
				psi.ArgumentList.Add(a);
			}

			var proc = System.Diagnostics.Process.Start(psi);
			if (proc == null)
			{
				return (-1, string.Empty, "git could not be started");
			}
			var stdout = proc.StandardOutput.ReadToEnd();
			var stderr = proc.StandardError.ReadToEnd();
			if (!proc.WaitForExit(30_000))
			{
				try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
				return (-1, stdout, "git did not exit within 30s");
			}
			return (proc.ExitCode, stdout, stderr);
		}
		catch (Exception e)
		{
			return (-1, string.Empty, e.Message);
		}
	}

	/// <summary>
	/// Every portal extension and extension library under <paramref name="root"/>, found by the
	/// <c>beamable.portalExtension</c> / <c>beamable.portalExtensionLib</c> markers in their package.json.
	///
	/// <para>
	/// A plain filesystem scan on purpose: it needs no Beamable workspace, no service manifest and no
	/// backend connection, so repointing a local package version works entirely offline — unlike the
	/// manifest-driven discovery in <c>beam portal extension update-toolkit</c>.
	/// </para>
	/// <para>
	/// node_modules is skipped, and not just for speed: a <c>file:</c> library is symlinked into every
	/// consumer, and .NET's recursive enumeration follows those symlinks, so walking it would return the
	/// same project many times under different paths.
	/// </para>
	/// </summary>
	public static List<(string name, string packageJsonPath)> FindExtensionProjects(string root)
	{
		var results = new List<(string, string)>();
		if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
		{
			return results;
		}

		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var packagePath in EnumeratePackageJsonExcludingNodeModules(root))
		{
			try
			{
				var info = JsonConvert.DeserializeObject<BeamoLocalSystem.PortalExtensionPackageInfo>(File.ReadAllText(packagePath));
				var isTarget = info?.BeamableProperties?.IsPortalExtension == true
					|| info?.BeamableProperties?.IsPortalExtensionLib == true;
				if (!isTarget)
				{
					continue;
				}

				var full = Path.GetFullPath(packagePath);
				if (seen.Add(full))
				{
					results.Add((info.Name ?? Path.GetFileName(Path.GetDirectoryName(full)), full));
				}
			}
			catch
			{
				// not a valid extension package.json - ignore
			}
		}

		return results;
	}

	private static IEnumerable<string> EnumeratePackageJsonExcludingNodeModules(string root)
	{
		var pending = new Stack<string>();
		pending.Push(root);

		while (pending.Count > 0)
		{
			var current = pending.Pop();

			string[] files;
			string[] directories;
			try
			{
				files = Directory.GetFiles(current, "package.json");
				directories = Directory.GetDirectories(current);
			}
			catch
			{
				continue; // unreadable directory
			}

			foreach (var file in files)
			{
				yield return file;
			}

			foreach (var directory in directories)
			{
				var name = Path.GetFileName(directory);
				if (string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase)
					// The `dotnet new` scaffolding sources carry the same portalExtension/portalExtensionLib
					// markers as a real extension, so a scan rooted at the product repo would repin them to the
					// local dev version — and every extension created afterwards would inherit that pin.
					|| string.Equals(name, "beamable.templates", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				pending.Push(directory);
			}
		}
	}

	/// <summary>
	/// Resolves the <c>portal-localdev</c> directory, from an explicit product dir or by searching upwards.
	/// Throws a <see cref="CliException"/> when it can't be found or has no compose file.
	/// </summary>
	public static string ResolveLocaldevDir(string productDirOrNull)
	{
		var productDir = productDirOrNull;
		if (string.IsNullOrEmpty(productDir))
		{
			productDir = FindProductDir(Directory.GetCurrentDirectory());
			if (string.IsNullOrEmpty(productDir))
			{
				throw new CliException(
					$"Could not find a BeamableProduct checkout containing {LocaldevDirName}/. Run this from inside that repository, or pass --product-dir");
			}
		}

		var localdevDir = Path.Combine(productDir, LocaldevDirName);
		if (!File.Exists(Path.Combine(localdevDir, "docker-compose.yml")))
		{
			throw new CliException($"No docker-compose.yml in [{localdevDir}]");
		}

		return localdevDir;
	}

	/// <summary>Runs a <c>docker compose</c> invocation in the registry's directory, throwing on failure.</summary>
	public static void RunCompose(string localdevDir, string arguments)
	{
		Log.Verbose($"  [cmd] docker {arguments}");
		var result = StartProcessUtil.Run("docker", arguments, useShell: true, workingDirectoryPath: localdevDir).WaitForResult();
		if (result.exit != 0)
		{
			throw new CliException($"'docker {arguments}' failed in [{localdevDir}]. Is Docker running? " +
				$"Errors: \n{result.stderr}");
		}
	}

	/// <summary>
	/// Best-effort, targeted eviction of the cached @beamable tarballs. A wiped registry republishes the
	/// same versions with different tarball hashes, and pnpm keys its content-addressable store by
	/// integrity, so a stale entry surfaces as ERR_PNPM_TARBALL_INTEGRITY on the next install.
	/// <para>
	/// Deliberately scoped to these two packages: a blanket <c>npm cache clean --force</c> or
	/// <c>pnpm store prune</c> would slow down every unrelated install on the machine to fix a problem only
	/// these packages have. npm needs no equivalent — it treats a changed integrity as a cache miss.
	/// </para>
	/// </summary>
	public static void EvictPackageCaches()
	{
		foreach (var package in new[] { SdkPackage, ToolkitPackage })
		{
			foreach (var arguments in new[] { $"store delete {package}", $"cache delete {package}" })
			{
				try
				{
					var result = StartProcessUtil.Run("pnpm", arguments, useShell: true).WaitForResult();
					if (result.exit != 0)
					{
						Log.Verbose($"'pnpm {arguments}' exited {result.exit}; continuing");
						continue;
					}

					Log.Verbose($"Evicted cached packages via 'pnpm {arguments}'");
				}
				catch (Exception e)
				{
					Log.Verbose($"Could not run 'pnpm {arguments}': {e.Message}");
				}
			}
		}
	}
}
