using Beamable.Common.BeamCli.Contracts;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;

namespace cli.UnityCommands;

public class GetUnityVersionInfoCommandArgs : CommandArgs
{
	public string unityProjectPath;
}

public class GetUnityVersionInfoCommandOutput
{
	public string beamableNugetVersion;
	public string sdkVersion;
	public string packageFolder;
}

public class GetUnityVersionInfoCommand : AtomicCommand<GetUnityVersionInfoCommandArgs, GetUnityVersionInfoCommandOutput>, IStandaloneCommand
{
	public GetUnityVersionInfoCommand() : base("get-version-info", "get information about a beamable unity sdk project's version dependencies")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("unityPath", "the path to the root of the unity project"), (args, unityPath) =>
		{
			if (Path.HasExtension(unityPath))
			{
				throw new CliException($"given unityPath must be a directory, unityPath=[${unityPath}]");
			}
			args.unityProjectPath = unityPath;
		});
	}

	public override Task<GetUnityVersionInfoCommandOutput> GetResult(GetUnityVersionInfoCommandArgs args)
	{
		return Task.FromResult(GetUnityInfo(args.unityProjectPath, "com.beamable"));
	}

	public static GetUnityVersionInfoCommandOutput GetUnityInfo(string unityPath, string packageId)
	{
		if (!TryGetPackageFolder(unityPath, out var packagePath, packageId))
		{
			throw new CliException("No beamable package is installed at that unity location");
		}
		if (!TryGetPackageFolder(unityPath, out var corePackagePath, "com.beamable"))
		{
			throw new CliException("No beamable package is installed at that unity location");
		}
		if (!TryGetUnityProjectNugetVersion(corePackagePath, out var versionData))
		{
			Log.Warning("Given beamable unity project does not have version file");
			versionData = new EnvironmentVersionData
			{
				nugetPackageVersion = "0.0.0"
			};
		}

		if (!TryGetUnityProjectPackageVersion(packagePath, out var packageData))
		{
			Log.Warning("Given beamable package does not have package.json file");
			packageData = new UnityPackage()
			{
				version = "0.0.0"
			};
		}
		
		return new GetUnityVersionInfoCommandOutput
		{
			packageFolder = packagePath,
			beamableNugetVersion = versionData.nugetPackageVersion,
			sdkVersion = packageData.version
		};
	}

	public static bool TryGetPackageFolder(string unityPath, out string packagePath, string packageId = "com.beamable")
	{
		packagePath = null;
		var localPackagePath = Path.Combine(unityPath, "Packages", packageId);
		if (Directory.Exists(localPackagePath))
		{
			packagePath = localPackagePath;
			return true;
		}
		
		var packageCache = Path.Combine(unityPath, "Library", "PackageCache");
		var cachedPackages = Directory.GetDirectories(packageCache);
		foreach (var cachedPackage in cachedPackages)
		{
			var cachedPackageName = Path.GetFileName(cachedPackage);
			if (cachedPackageName.StartsWith(packageId + "@"))
			{
				packagePath = cachedPackage;
				return true;
			}
		}

		return false;
	}
	
	public static bool TryGetUnityProjectNugetVersion(string packagePath, out EnvironmentVersionData versionData)
	{
		versionData = null;
		var versionPath = Path.Combine(packagePath, "Runtime/Environment/Resources/versions-default.json");
		if (!File.Exists(versionPath))
		{
			return false;
		}
		var json = File.ReadAllText(versionPath);
		versionData = JsonConvert.DeserializeObject<EnvironmentVersionData>(json);
		return true;
	}

	public static bool TryGetUnityProjectPackageVersion(string packagePath, out UnityPackage data)
	{
		data = null;
		var packageJsonPath = Path.Combine(packagePath, "package.json");
		if (!File.Exists(packageJsonPath))
		{
			return false;
		}
		var json = File.ReadAllText(packageJsonPath);
		data = JsonConvert.DeserializeObject<UnityPackage>(json);
		return true;
	}

	[Serializable]
	public class UnityPackage
	{
		public string version;
	}
}
