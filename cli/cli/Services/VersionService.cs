using Newtonsoft.Json;
using Serilog;
using System.Reflection;

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

		var asm = Assembly.GetEntryAssembly();
		var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(asm.Location);
		info.version = versionInfo.FileVersion;
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
