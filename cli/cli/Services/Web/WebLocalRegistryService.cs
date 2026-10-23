using Beamable.Common;
using Beamable.Server;
using cli.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace cli.Services.Web;

/// <summary>
/// Shared plumbing for the local web packages: the Verdaccio registry (:4873) that holds local builds of
/// <c>@beamable/sdk</c> and <c>@beamable/portal-toolkit</c> (local-unpkg serves their files to the portal on :4874),
/// the product checkout they are built from, and the extensions that use them. The loop itself is
/// <see cref="WebPackagesService"/>.
/// </summary>
public static class WebLocalRegistryService
{
	public const string DefaultRegistry = "http://localhost:4873";

	/// <summary>The public npm registry, where released <c>@beamable</c> versions come from.</summary>
	public const string PublicRegistry = "https://registry.npmjs.org/";

	public const string SdkPackage = "@beamable/sdk";
	public const string ToolkitPackage = "@beamable/portal-toolkit";

	/// <summary>The dist-tag local builds are published under, so a publish never moves <c>latest</c>.</summary>
	public const string LocalDistTag = "local";

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
	/// The npm flag binding the local registry to the <c>@beamable</c> scope only. A global <c>--registry</c> would send
	/// every package of the project through Verdaccio's proxy of npmjs, and a few truncated entries in its cache once
	/// turned a seconds-long install into ten minutes across the workspace.
	/// </summary>
	public static string ScopedRegistryFlag(string registryUrl) =>
		$"--@beamable:registry={registryUrl}";

	/// <summary>
	/// npm refuses to publish without a token even when the registry allows anonymous writes, so every
	/// write passes one as a CLI flag. Doing it this way keeps the user's global npmrc untouched: changing it
	/// broke installs machine-wide once the registry went away.
	/// </summary>
	public static string AuthTokenFlag(string registryUrl)
	{
		// npm keys auth config by the registry URL with the scheme stripped, e.g. //localhost:4873/:_authToken
		var withoutScheme = registryUrl.Replace("https://", string.Empty).Replace("http://", string.Empty).TrimEnd('/');
		return $"\"--//{withoutScheme}/:_authToken={LocalDistTag}\"";
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
	/// Preflight for <c>beam local run</c> and any direct <c>beam web publish</c>: when the recorded
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
	/// backend connection, unlike the manifest-driven discovery in <c>beam portal extension update-toolkit</c>.
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
					// markers as a real extension, but aren't projects of the workspace.
					|| string.Equals(name, "beamable.templates", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				pending.Push(directory);
			}
		}
	}
}
