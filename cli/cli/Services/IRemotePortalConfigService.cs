using System.Text.Json;
using System.Text.RegularExpressions;
using cli.Services;
using Beamable.Server;

namespace cli.Portal;

public interface IRemotePortalConfigService
{
	Task<RemotePortalConfiguration> GetRemotePortalConfig(CommandArgs args);
}

public class RemotePortalConfigService : IRemotePortalConfigService
{
	/// <summary>
	/// Selector <c>type</c> used for mount sites that come from a local extension's
	/// <c>BeamExtensionSite</c> declarations. These are neither remote "page" nor "component"
	/// slots: an extension's site lives wherever that extension is mounted, so a child extension
	/// targets it by the host extension's <b>name</b> (the host's package.json <c>mount.page</c>),
	/// not by a Portal route. The distinct type lets callers list and validate them separately.
	/// </summary>
	public const string ExtensionMountType = "extension";

	public async Task<RemotePortalConfiguration> GetRemotePortalConfig(CommandArgs args)
	{
		var config = await FetchRemotePortalConfig(args);
		AppendLocalExtensionMountSites(args, config);
		return config;
	}

	/// <summary>
	/// Fetch the remote mount-site catalog (extension-pages.json) and strip the common realm path
	/// prefix. This is best-effort: if the catalog is unavailable — e.g. an environment that
	/// doesn't publish it (404) or no network — we warn and return an empty config so that
	/// locally-discovered extension mount sites still surface instead of failing the whole command.
	/// </summary>
	private static async Task<RemotePortalConfiguration> FetchRemotePortalConfig(CommandArgs args)
	{
		var url = PortalCommand.GetPortalBaseUrl(args, true) + "/extension-pages.json";
		try
		{
			var client = new HttpClient();
			var json = await client.GetStringAsync(url);
			var config = JsonSerializer.Deserialize<RemotePortalConfiguration>(json, new JsonSerializerOptions
			{
				IncludeFields = true
			}) ?? new RemotePortalConfiguration();

			var commonPref = "/:customerId/games/:gameId/realms/:realmId/";
			foreach (var mountSite in config.mountSites)
			{
				if (mountSite.path.StartsWith(commonPref))
				{
					mountSite.path = mountSite.path.Substring(commonPref.Length);
				}
			}

			return config;
		}
		catch (Exception e)
		{
			Log.Warning(
				"Could not fetch remote portal config from {url}: {message}. Continuing with local extension mount sites only.",
				url, e.Message);
			return new RemotePortalConfiguration();
		}
	}

	/// <summary>
	/// Local Portal Extensions can declare slots for *other* extensions to mount into via the
	/// <c>&lt;BeamExtensionSite selector="top|bottom" /&gt;</c> React component. The remote
	/// extension-pages.json knows nothing about those locally-declared slots, so we read the
	/// already-loaded local manifest and append a mount site per extension that declares any.
	/// The manifest is assumed to be initialized — every command that reaches here does so without
	/// <c>ISkipManifest</c>, so the framework has already loaded it. Best-effort: failures (e.g.
	/// while scanning extension sources) must not break the remote-config path.
	/// </summary>
	private static void AppendLocalExtensionMountSites(CommandArgs args, RemotePortalConfiguration config)
	{
		try
		{
			var extensions = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
				.Where(x => x.PortalExtensionDefinition?.Properties?.IsPortalExtension ?? false)
				.Select(x => x.PortalExtensionDefinition);

			MergeMountSites(config.mountSites, BuildLocalExtensionMountSites(extensions));
		}
		catch (Exception e)
		{
			Log.Warning("Could not append local extension mount sites: {message}", e.Message);
		}
	}

	/// <summary>
	/// Merge generated sites into the existing list, keyed by <c>path</c>, de-duplicating selectors
	/// by selector string. Extension-hosted sites are keyed by extension name, which is normally
	/// distinct from any remote route, so each is simply appended. Merging (rather than a blind add)
	/// guards the rare path clash — duplicate paths would otherwise collide downstream in the
	/// path-keyed dictionary in <c>NewPortalExtensionCommand.BuildMountSiteIndex</c> (last-writer-wins).
	/// </summary>
	public static void MergeMountSites(
		List<RemotePortalConfiguration.MountSiteConfig> target,
		IEnumerable<RemotePortalConfiguration.MountSiteConfig> additions)
	{
		foreach (var addition in additions)
		{
			var existing = target.FirstOrDefault(s => s.path == addition.path);
			if (existing == null)
			{
				target.Add(addition);
				continue;
			}

			foreach (var selector in addition.selectors)
			{
				if (existing.selectors.All(s => s.selector != selector.selector))
					existing.selectors.Add(selector);
			}
		}
	}

	/// <summary>
	/// Pure builder: turns local extensions into the extra mount sites they expose. The site
	/// <c>path</c> is the extension's <b>name</b> — a child extension mounts into it by setting its
	/// own <c>mount.page</c> to that name (e.g. a child sets <c>"page": "Temaki"</c> to land inside
	/// the "Temaki" extension). Selectors come from the <c>BeamExtensionSite</c> declarations found
	/// in the extension's source, tagged with <see cref="ExtensionMountType"/>; navContext carries
	/// the host extension's navGroup + navLabel for display.
	/// </summary>
	public static List<RemotePortalConfiguration.MountSiteConfig> BuildLocalExtensionMountSites(
		IEnumerable<PortalExtensionDef> extensions)
	{
		var sites = new List<RemotePortalConfiguration.MountSiteConfig>();

		foreach (var ext in extensions)
		{
			var name = ext?.Name;
			if (string.IsNullOrEmpty(name)) continue;

			var selectors = ScanExtensionSiteSelectors(ext.AbsolutePath);
			if (selectors.Count == 0) continue;

			var navContext = new List<string>();
			var navGroup = ext.Properties?.Mount?.NavGroup;
			var navLabel = ext.Properties?.Mount?.NavLabel;
			if (navGroup != null && navGroup.HasValue && !string.IsNullOrEmpty(navGroup.Value))
				navContext.Add(navGroup.Value);
			if (navLabel != null && navLabel.HasValue && !string.IsNullOrEmpty(navLabel.Value))
				navContext.Add(navLabel.Value);

			sites.Add(new RemotePortalConfiguration.MountSiteConfig
			{
				path = name,
				selectors = selectors,
				navContext = navContext
			});
		}

		return sites;
	}

	// Only `top` / `bottom` BeamExtensionSite slots are surfaced, emitted (in this order) as the
	// `#top` / `#bottom` selectors a child extension references.
	private static readonly string[] SupportedExtensionSiteSelectors = { "top", "bottom" };

	private static readonly Regex BeamExtensionSiteRegex = new Regex(
		"<BeamExtensionSite\\b[^>]*\\bselector\\s*=\\s*[\"']([^\"']+)[\"']",
		RegexOptions.Compiled);

	/// <summary>
	/// Scan an extension's source tree for <c>BeamExtensionSite</c> usages and return the resulting
	/// selectors (tagged with <see cref="ExtensionMountType"/>). I/O wrapper around
	/// <see cref="ParseExtensionSiteSelectors"/>.
	/// </summary>
	public static List<RemotePortalConfiguration.MountSiteSelector> ScanExtensionSiteSelectors(string extAbsolutePath)
	{
		var found = new HashSet<string>();

		if (!string.IsNullOrEmpty(extAbsolutePath) && Directory.Exists(extAbsolutePath))
		{
			var excludedDirs = new[] { "node_modules", "dist", "assets", "build", ".git" };
			var files = Directory.EnumerateFiles(extAbsolutePath, "*.*", SearchOption.AllDirectories)
				.Where(f => Path.GetExtension(f) is ".tsx" or ".jsx" or ".ts")
				.Where(f =>
				{
					var rel = Path.GetRelativePath(extAbsolutePath, f);
					var segments = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
					return !segments.Any(s => excludedDirs.Contains(s));
				});

			foreach (var file in files)
			{
				string text;
				try { text = File.ReadAllText(file); }
				catch { continue; }

				foreach (var selector in ParseExtensionSiteSelectors(text))
					found.Add(selector);
			}
		}

		// Preserve the canonical top-then-bottom order regardless of discovery order.
		return SupportedExtensionSiteSelectors
			.Where(found.Contains)
			.Select(s => new RemotePortalConfiguration.MountSiteSelector
			{
				selector = "#" + s,
				type = ExtensionMountType
			})
			.ToList();
	}

	/// <summary>
	/// Pure parse: extract the supported (`top` / `bottom`) BeamExtensionSite selector values from a
	/// single source file's text. Handles <c>selector="top"</c> and <c>selector='top'</c>; the JSX
	/// expression form <c>selector={'top'}</c> is out of scope.
	/// </summary>
	public static IEnumerable<string> ParseExtensionSiteSelectors(string fileText)
	{
		if (string.IsNullOrEmpty(fileText)) yield break;

		foreach (Match match in BeamExtensionSiteRegex.Matches(fileText))
		{
			var value = match.Groups[1].Value;
			if (SupportedExtensionSiteSelectors.Contains(value))
				yield return value;
		}
	}
}
