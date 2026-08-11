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
		if (result.exit != 0)
		{
			error = string.IsNullOrWhiteSpace(result.stderr) ? result.stdout : result.stderr;
			return false;
		}

		return true;
	}

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
	/// The npm arguments needed to install a project whose <c>@beamable/portal-toolkit</c> pin is a local
	/// developer build — i.e. <c>--registry &lt;local&gt;</c> plus its auth token. Returns an empty string
	/// for every other project, so a normal install is completely untouched.
	///
	/// <para>
	/// Required, not an optimisation: a local-dev version exists only on the local registry, so a plain
	/// <c>npm install</c> resolves it against npmjs, 404s, and fails the build. Routing the *whole* install
	/// at the local registry is correct because it proxies everything else to npmjs (see
	/// <c>portal-localdev/verdaccio/config.yml</c>).
	/// </para>
	/// </summary>
	public static string InstallArgsFor(string projectDir, string registryUrl = DefaultRegistry)
	{
		var pinned = ReadPinnedVersion(Path.Combine(projectDir, "package.json"), ToolkitPackage);
		if (!IsLocalDevVersion(pinned))
		{
			return string.Empty;
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
