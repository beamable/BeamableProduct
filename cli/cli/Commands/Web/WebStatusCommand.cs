using Beamable.Server;
using cli.Services;
using cli.Services.Web;
using System.CommandLine;

namespace cli.Web;

public class WebStatusCommandArgs : CommandArgs
{
	public string Registry;
	public string Cdn;
}

public class WebPackageStatus
{
	public string package;
	/// <summary>Versions published to the local registry. Empty when none are.</summary>
	public List<string> localVersions = new List<string>();
	/// <summary>
	/// The version the registry's <c>local</c> dist-tag points at. Null when nothing is published.
	/// </summary>
	public string localTag;
	/// <summary>
	/// When the local build was published (ISO). Because the version never changes, this is the only way to
	/// tell one build from the next.
	/// </summary>
	public string publishedAt;
}

public class WebStatusCommandResults
{
	public string registry;
	public string cdn;
	public bool registryReachable;
	public bool cdnReachable;
	public List<WebPackageStatus> packages = new List<WebPackageStatus>();
}

/// <summary>
/// Reports whether the local registry and CDN are up and which web package versions are published
/// locally — the first thing worth checking when an extension unexpectedly loads a published SDK.
/// </summary>
public class WebStatusCommand : AtomicCommand<WebStatusCommandArgs, WebStatusCommandResults>, IStandaloneCommand, ISkipManifest
{
	public WebStatusCommand() : base("status",
		"Show whether the local Beamable web package registry and CDN are running, and which versions are published locally")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--registry", () => WebLocalRegistryService.DefaultRegistry, "The local npm registry to query"),
			(args, i) => args.Registry = i);
		AddOption(new Option<string>("--cdn", () => WebLocalRegistryService.DefaultCdn, "The local unpkg-style CDN to probe"),
			(args, i) => args.Cdn = i);
	}

	public override async Task<WebStatusCommandResults> GetResult(WebStatusCommandArgs args)
	{
		var registry = string.IsNullOrEmpty(args.Registry) ? WebLocalRegistryService.DefaultRegistry : args.Registry;
		var cdn = string.IsNullOrEmpty(args.Cdn) ? WebLocalRegistryService.DefaultCdn : args.Cdn;
		var service = args.Provider.GetService<WebLocalRegistryService>();

		var results = new WebStatusCommandResults { registry = registry, cdn = cdn };

		results.registryReachable = await service.IsRegistryReachable(registry);
		if (!results.registryReachable)
		{
			Log.Information($"registry [{registry}]  NOT RUNNING - every project resolves from npm");
			return results;
		}

		var versionService = args.Provider.GetService<VersionService>();
		foreach (var package in new[] { WebLocalRegistryService.SdkPackage, WebLocalRegistryService.ToolkitPackage })
		{
			// Only local builds count: the registry proxies npmjs, so its packument also lists every
			// published version, which is noise here.
			var versions = (await service.GetLocallyPublishedVersions(package, registry))
				.Where(WebLocalRegistryService.IsLocalDevVersion)
				.OrderBy(v => v, StringComparer.Ordinal)
				.ToList();

			var packument = await versionService.GetNpmPackument(package, registry, throwOnError: false);
			var localTag = packument?.DistTags != null
				&& packument.DistTags.TryGetValue(WebLocalRegistryService.LocalDistTag, out var tag)
					? tag
					: null;

			// The publish time of the local-dev version, since the version string itself can't distinguish
			// two builds. Prefer the tagged version, else the standard one.
			var timeKey = localTag ?? WebLocalRegistryService.LocalDevVersion;
			var publishedAt = packument?.Time != null && packument.Time.TryGetValue(timeKey, out var t) ? t : null;

			results.packages.Add(new WebPackageStatus
			{
				package = package,
				localVersions = versions,
				localTag = localTag,
				publishedAt = publishedAt
			});
		}

		results.cdnReachable = await IsCdnReachable(cdn);

		Log.Information($"registry [{registry}]  running");
		Log.Information($"cdn      [{cdn}]  {(results.cdnReachable ? "running" : "NOT RUNNING - extensions on a local build will fail to load")}");
		foreach (var package in results.packages)
		{
			if (package.localVersions.Count == 0)
			{
				Log.Information($"  {package.package}: no local build published");
				continue;
			}

			// The version is fixed, so the publish time is what tells builds apart.
			Log.Information($"  {package.package}: {string.Join(", ", package.localVersions)}" +
				(string.IsNullOrEmpty(package.publishedAt) ? "" : $"   published {package.publishedAt}"));
		}

		if (results.packages.Any(p => p.localVersions.Count > 0))
		{
			Log.Information("Refresh the projects that use these with 'beam web use'.");
		}

		return results;
	}

	private static async Task<bool> IsCdnReachable(string cdn)
	{
		try
		{
			using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
			// Any path works as a liveness probe: the server answers 400 for a malformed package path,
			// which still proves it is listening.
			await client.GetAsync($"{cdn.TrimEnd('/')}/");
			return true;
		}
		catch
		{
			return false;
		}
	}
}
