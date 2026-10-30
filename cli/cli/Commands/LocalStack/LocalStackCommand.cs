using cli.Services;
using cli.Services.LocalStack;

namespace cli.Commands.LocalStack;

/// <summary>
/// Parent group for the local-stack orchestrator (<c>beam local ...</c>). Brings up a local Beamable loop —
/// backend, portal, microservices and portal extensions — by stack name, like <c>npm run</c>:
/// <c>setup</c>, <c>run</c>, <c>create</c>, <c>edit</c>, <c>delete</c>, <c>ps</c>, <c>logs</c>, <c>restart</c>, <c>start</c> and <c>stop</c>.
/// </summary>
public class LocalStackCommand : CommandGroup, IStandaloneCommand, ISkipManifest
{
	public override bool IsForInternalUse { get; } = true;

	public LocalStackCommand() : base("local", "Run a local Beamable stack by name: `beam local run` lists the stacks, `beam local run <stack>[:variant...]` starts one")
	{
	}

	/// <summary>
	/// Resolves the manifest path: the explicit <paramref name="overridePath"/> if given, otherwise
	/// <c>&lt;.beamable&gt;/local/stack-settings.json</c> when a workspace exists, otherwise the same file under a
	/// <c>.beamable</c> folder in the current directory.
	/// </summary>
	public static string ResolveManifestPath(ConfigService configService, string overridePath)
	{
		if (!string.IsNullOrWhiteSpace(overridePath))
			return Path.GetFullPath(overridePath);

		if (configService?.DirectoryExists == true
		    && !string.IsNullOrEmpty(configService.ConfigDirectoryPath)
		    && !IsHomeWorkspace(configService.ConfigDirectoryPath))
			return SettingsPath(configService.ConfigDirectoryPath);

		// No workspace was registered at startup. Use the `.beamable` folder in the current directory — that is where
		// `init` writes one, and the folder can have been created AFTER the ConfigService was built (by an `init` in
		// this same shell), so DirectoryExists above would not have seen it.
		return SettingsPath(Path.GetFullPath(ConfigService.CFG_FOLDER));
	}

	/// <summary>
	/// Where <c>beam local setup</c> should WRITE a manifest. Same as <see cref="ResolveManifestPath"/> except that,
	/// with no workspace, the <c>.beamable</c> folder is taken in the working directory the CLI was started in.
	///
	/// The directory is not created here; <see cref="EnsureManifestDirectory"/> does that, so the caller can
	/// confirm first.
	/// </summary>
	public static string ResolveManifestPathForInit(ConfigService configService, string overridePath)
	{
		if (!string.IsNullOrWhiteSpace(overridePath))
			return Path.GetFullPath(overridePath);

		if (configService?.DirectoryExists == true
		    && !string.IsNullOrEmpty(configService.ConfigDirectoryPath)
		    && !IsHomeWorkspace(configService.ConfigDirectoryPath))
			return SettingsPath(configService.ConfigDirectoryPath);

		var workingDirectory = configService?.WorkingDirectory ?? Directory.GetCurrentDirectory();
		return SettingsPath(Path.GetFullPath(Path.Combine(workingDirectory, ConfigService.CFG_FOLDER)));
	}

	/// <summary>The manifest path inside a <c>.beamable</c> folder: <c>&lt;.beamable&gt;/local/stack-settings.json</c>.</summary>
	public static string SettingsPath(string beamableFolder) =>
		Path.Combine(beamableFolder, LocalStackConfigIO.SettingsFolder, LocalStackConfigIO.SettingsFileName);


	/// <summary>
	/// Makes sure the directory a manifest is about to be written into exists, creating it when the caller allows.
	/// Returns false when it is missing and creation was declined.
	///
	/// <paramref name="confirm"/> is invoked only when the directory does not exist yet, so an existing workspace
	/// is never questioned. Creating a <c>.beamable</c> folder is a visible side effect in someone's project, so
	/// interactive callers ask first; quiet ones create it.
	/// </summary>
	public static bool EnsureManifestDirectory(string manifestPath, Func<string, bool> confirm, out string directory)
	{
		directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
		if (string.IsNullOrEmpty(directory) || Directory.Exists(directory))
			return true;

		if (confirm != null && !confirm(directory))
			return false;

		Directory.CreateDirectory(directory);
		return true;
	}

	/// <summary>
	/// True when a resolved workspace is <c>~/.beamable</c> — i.e. the HOME directory itself.
	///
	/// Workspace lookup walks up from the working directory, so running a command outside any workspace lands on
	/// the home directory whenever a <c>~/.beamable</c> exists. That is never what someone means: it silently
	/// writes a <c>local-stack.json</c> (and config, and logs) into their home folder, and every later invocation
	/// from an unrelated directory then picks that phantom workspace up instead of reporting that there is none.
	///
	/// Treating it as "no workspace" makes the local-stack commands use a manifest in the CURRENT directory
	/// instead, which is both predictable and what the fallback below already did before a <c>~/.beamable</c>
	/// happened to exist.
	/// </summary>
	public static bool IsHomeWorkspace(string configDirectoryPath)
	{
		try
		{
			var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (string.IsNullOrEmpty(home)) return false;

			var resolved = Path.GetFullPath(configDirectoryPath).TrimEnd(Path.DirectorySeparatorChar);
			var homeWorkspace = Path.Combine(home, ConfigService.CFG_FOLDER).TrimEnd(Path.DirectorySeparatorChar);

			return string.Equals(resolved, homeWorkspace,
				OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
					? StringComparison.OrdinalIgnoreCase
					: StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
	}

	/// <summary>Resolves the run-state path that sits alongside the resolved manifest.</summary>
	public static string ResolveRunStatePath(ConfigService configService, string overridePath) =>
		LocalStackRunStateIO.ResolveRunStatePath(ResolveManifestPath(configService, overridePath));

	/// <summary>The well-known checkout folder names the local stack spans, in <c>(name, kind)</c> form.</summary>
	public const string ApiRepoName = "BeamableAPI";

	/// <inheritdoc cref="ApiRepoName"/>
	public const string ScalaRepoName = "BeamableBackend";

	/// <inheritdoc cref="ApiRepoName"/>
	public const string PortalRepoName = "agentic-portal";

	/// <inheritdoc cref="ApiRepoName"/>
	public const string ProductRepoName = "BeamableProduct";

	/// <summary>
	/// Looks for a folder named <paramref name="name"/> in <paramref name="startDir"/> and its ancestors (up to
	/// <paramref name="maxLevels"/> levels up), returning the first match — used to auto-fill the repo paths
	/// (BeamableAPI / BeamableBackend / agentic-portal) that typically sit as siblings a level or two up.
	///
	/// Shared by <c>init</c> and <c>setup</c>: setup has to find the checkouts on its own, because it is meant to
	/// run BEFORE a manifest exists and therefore cannot read the paths from one.
	///
	/// Repo folders may carry a name suffix, after <c>_</c> or <c>-</c> (<c>&lt;Repo&gt;_x</c> for parallel checkouts,
	/// <c>&lt;Repo&gt;-&lt;branch&gt;</c> for a branch downloaded from GitHub; see <see cref="FindRepoSuffix"/>).
	/// From inside one, the sibling with the same suffix wins, so <c>agentic-portal_x</c> gets <c>BeamableAPI_x</c>;
	/// a repo with no such sibling falls back to its plain name, then to the only suffixed folder of that repo.
	/// </summary>
	public static string FindRepoDir(string startDir, string name, int maxLevels = 3)
	{
		try
		{
			var start = new DirectoryInfo(string.IsNullOrEmpty(startDir) ? Directory.GetCurrentDirectory() : startDir);
			var suffix = FindRepoSuffix(start.FullName);
			return (suffix == null ? null : FindDir(start, n => n.Equals($"{name}_{suffix}", StringComparison.OrdinalIgnoreCase)
			                                                    || n.Equals($"{name}-{suffix}", StringComparison.OrdinalIgnoreCase), maxLevels))
			       ?? FindDir(start, n => n.Equals(name, StringComparison.OrdinalIgnoreCase), maxLevels)
			       ?? FindDir(start, n => IsSuffixed(n, name), maxLevels, onlyOne: true);
		}
		catch { /* best-effort discovery */ }

		return null;
	}

	/// <summary>
	/// The name suffix of the repo folder <paramref name="path"/> is in: the <c>x</c> of a folder named
	/// <c>&lt;Repo&gt;_x</c> or <c>&lt;Repo&gt;-x</c> for one of the well-known repo names, or null when the folder
	/// has the plain name.
	/// </summary>
	public static string FindRepoSuffix(string path)
	{
		string[] repos = { ApiRepoName, ScalaRepoName, PortalRepoName, ProductRepoName };
		for (var dir = new DirectoryInfo(path); dir != null; dir = dir.Parent)
		{
			foreach (var repo in repos)
			{
				if (IsSuffixed(dir.Name, repo))
					return dir.Name.Substring(repo.Length + 1);
			}
		}

		return null;
	}

	/// <summary>True for <c>&lt;repo&gt;_x</c> and <c>&lt;repo&gt;-x</c>.</summary>
	private static bool IsSuffixed(string folder, string repo) =>
		folder.Length > repo.Length + 1 && folder.StartsWith(repo, StringComparison.OrdinalIgnoreCase) && folder[repo.Length] is '_' or '-';

	/// <summary>
	/// The first folder <paramref name="matches"/> accepts: <paramref name="start"/> or an ancestor itself, or a folder
	/// in one. With <paramref name="onlyOne"/>, a level with several matching folders is skipped as ambiguous.
	/// </summary>
	private static string FindDir(DirectoryInfo start, Func<string, bool> matches, int maxLevels, bool onlyOne = false)
	{
		var dir = start;
		for (var i = 0; i <= maxLevels && dir != null; i++)
		{
			// The checkout itself counts: from inside agentic-portal_x, the portal is agentic-portal_x.
			if (matches(dir.Name)) return dir.FullName;
			var found = dir.GetDirectories().Where(d => matches(d.Name)).ToList();
			if (found.Count == 1 || (!onlyOne && found.Count > 1)) return found[0].FullName;
			dir = dir.Parent;
		}

		return null;
	}
}
