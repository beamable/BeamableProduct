using Beamable.Common;
using Beamable.Common.Util;
using Newtonsoft.Json;
using Beamable.Server;

namespace cli.Services;

public class VersionService
{
	private readonly HttpClient _httpClient;

	public VersionService()
	{
		_httpClient = new HttpClient();
	}

	public class NugetVersionList
	{
		public string[] versions;
	}

	public class NugetPackages
	{
		public string originalVersion;
		public string packageVersion;
	}

	/// <summary>
	/// The subset of an npm "packument" (the JSON returned by GET {registry}/{package}) that we care
	/// about: the dist-tags (e.g. "latest", "local") and the set of published versions.
	/// </summary>
	public class NpmPackument
	{
		[JsonProperty("dist-tags")] public Dictionary<string, string> DistTags;
		[JsonProperty("versions")] public Dictionary<string, object> Versions;
	}

	/// <summary>
	/// Fetches the npm packument for a package from an npm-compatible registry (npmjs.org or a local
	/// verdaccio). The package name's "/" is URL-encoded as required by the registry API.
	/// When <paramref name="throwOnError"/> is false, an unreachable registry or non-success response
	/// returns null instead of throwing — used to probe verdaccio as a fallback.
	/// </summary>
	public async Task<NpmPackument> GetNpmPackument(string packageName, string registryBaseUrl, bool throwOnError = true)
	{
		var url = $"{registryBaseUrl.TrimEnd('/')}/{packageName.Replace("/", "%2F")}";
		try
		{
			var message = await _httpClient.GetAsync(url);
			var rawBody = await message.Content.ReadAsStringAsync();

			if (!message.IsSuccessStatusCode)
			{
				if (!throwOnError)
				{
					return null;
				}

				var errorMessage = $"Unable to query registry for package [{packageName}]. url=[{url}] status=[{message.StatusCode}] message=[{rawBody}]";
				Log.Error(errorMessage);
				throw new CliException(errorMessage);
			}

			return JsonConvert.DeserializeObject<NpmPackument>(rawBody);
		}
		catch (Exception e) when (e is not CliException)
		{
			if (!throwOnError)
			{
				return null;
			}

			throw new CliException($"Unable to query registry for package [{packageName}]. url=[{url}] message=[{e.Message}]");
		}
	}

	public async Task<NugetPackages[]> GetBeamableToolPackageVersions(bool replaceDashWithDot = true,
		string packageName = "beamable.tools")
	{
		var url = $"https://api.nuget.org/v3-flatcontainer/{packageName}/index.json";
		var message = await _httpClient.GetAsync(url);
		var rawBody = await message.Content.ReadAsStringAsync();

		if (!message.IsSuccessStatusCode)
		{
			var errorMessage = $"Unable to list packages from nuget.org. url=[{url}] status=[{message.StatusCode}] message=[{rawBody}]";
			Log.Error(errorMessage);
			throw new CliException(errorMessage);
		}

		var response = JsonConvert.DeserializeObject<NugetVersionList>(rawBody);
		var packageVersions = new NugetPackages[response.versions.Length];
		for (var i = 0; i < packageVersions.Length; i++)
		{
			packageVersions[i] = new NugetPackages
			{
				originalVersion = response.versions[i],
				packageVersion = replaceDashWithDot
					? response.versions[i].Replace("-", ".")
					: response.versions[i]
			};
		}

		return packageVersions;

	}

	public async Task<VersionInfo> GetInformationData(ProjectService projectService)
	{
		var info = new VersionInfo();

		info.version = BeamAssemblyVersionUtil.GetVersion<BeamoService>();
		info.location = Environment.ProcessPath;

		var templateInfo = await projectService.GetTemplateInfo();
		info.templateVersion = templateInfo.HasTemplates ? templateInfo.templateVersion : "<no templates installed>";

		var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var globalToolsDir = Path.Combine(homeDir, ".dotnet", "tools");
		if (info.location?.StartsWith(globalToolsDir) ?? false)
		{
			info.installType = VersionInstallType.GlobalTool;
		}
		else
		{
			info.installType = VersionInstallType.LocalTool;
		}
		return info;
	}

	public static PackageVersion GetNugetPackagesForExecutingCliVersion()
	{
		var versionString = BeamAssemblyVersionUtil.GetVersion<BeamoService>();
		if (!PackageVersion.TryFromSemanticVersionString(versionString, out var currentVersion))
		{
			currentVersion = "0.0.0";
		}

		if ((currentVersion.Major == 0 || currentVersion == "1.0.0") && !currentVersion.IsNightly)
		{
			// if the major is 0, then its likely 0.0.0 or 0.0.123, 
			//  which means we want to use our local dev nuget package version, which is 0.0.123
			return "0.0.123";
		}

		return currentVersion;
	}

	public struct VersionInfo
	{
		public string version;
		public string location;
		public string templateVersion;
		public VersionInstallType installType;
	}

	public enum VersionInstallType
	{
		GlobalTool,
		LocalTool
	}
}
