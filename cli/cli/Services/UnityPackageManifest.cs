using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace cli.Services;

[Serializable]
public class UnityPackageManifest
{
	public Dictionary<string, string> dependencies;

	public UnityScopedRegistry[] scopedRegistries;

	public static UnityPackageManifest FromFile(string path)
	{
		if (!File.Exists(path))
		{
			return null;
		}

		var fileContent = File.ReadAllText(path);

		JObject fullManifest = JObject.Parse(fileContent);

		// var properties = JsonConvert.DeserializeObject<UnityPackageManifest>(fileContent);
		var properties = fullManifest["dependencies"]!.ToObject<Dictionary<string, string>>();
		var scopedRegistries = fullManifest["scopedRegistries"]?.ToObject<UnityScopedRegistry[]>() ?? new UnityScopedRegistry[] { };

		return new UnityPackageManifest() { dependencies = properties, scopedRegistries = scopedRegistries };
	}

	public void SetBeamablePackagesVersion(string packageVersion, bool addServerPackageIfMissing = true)
	{
		AddOrUpdatePackage("com.beamable", packageVersion);
		if (addServerPackageIfMissing || dependencies.ContainsKey("com.beamable.server"))
		{
			AddOrUpdatePackage("com.beamable.server", packageVersion);
		}
	}

	public void AddOrUpdatePackage(string packageName, string packageVersion)
	{
		dependencies[packageName] = packageVersion;
	}

	public void AddOrUpdateScopedRegistryForBeam(BeamNexusRepository repository)
	{
		var scopedRegistry = repository.GetRegistryForRepository();
		for (int i = 0; i < scopedRegistries?.Length; i++)
		{
			if (scopedRegistries[i].name != scopedRegistry.name)
				continue;
			scopedRegistries[i] = scopedRegistry;
			return;
		}

		var registries = scopedRegistries != null ? scopedRegistries.ToList() : new List<UnityScopedRegistry>(1);
		registries.Add(scopedRegistry);
		scopedRegistries = registries.ToArray();
	}

	public async Task SaveToFile(string path)
	{
		var fileContent = await File.ReadAllTextAsync(path);
		JObject fullManifest = JObject.Parse(fileContent);

		// var properties = JsonConvert.DeserializeObject<UnityPackageManifest>(fileContent);
		fullManifest["dependencies"]!.Replace(JToken.FromObject(dependencies));
		fullManifest["scopedRegistries"] = JToken.FromObject(scopedRegistries);

		await File.WriteAllTextAsync(path, fullManifest.ToString());
	}

	[Serializable]
	public class UnityScopedRegistry
	{
		public string name;
		public string url;
		public string[] scopes;
	}
}

[Serializable]
public enum BeamNexusRepository
{
	All,
	Preview,
	Dev,
	Release
}

public static class BeamNexusRepositoryHelper
{
	public static UnityPackageManifest.UnityScopedRegistry GetRegistryForRepository(this BeamNexusRepository repository)
	{
		return new UnityPackageManifest.UnityScopedRegistry
		{
			name = "Beamable",
			url = repository.GetUrl(),
			scopes = new[] { "com.beamable" }
		};
	}

	public static string GetUrl(this BeamNexusRepository repository)
	{
		switch (repository)
		{
			case BeamNexusRepository.All:
				return "https://nexus.beamable.com/nexus/content/repositories/unity-all/";
			case BeamNexusRepository.Preview:
				return "https://nexus.beamable.com/nexus/content/repositories/unity-preview/";
			case BeamNexusRepository.Dev:
				return "https://nexus.beamable.com/nexus/content/repositories/unity-dev/";
			case BeamNexusRepository.Release:
				return "https://nexus.beamable.com/nexus/content/repositories/unity/";
			default:
				throw new ArgumentOutOfRangeException(nameof(repository), repository, null);
		}
	}
}
