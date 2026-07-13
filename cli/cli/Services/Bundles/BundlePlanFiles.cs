using Beamable.Common.Dependencies;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server;
using cli.Deployment.Services;
using cli.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace cli.Services.Bundles;

/// <summary>
/// A saved <c>bundles plan</c> result, the bundle analog of a <c>deploy plan</c> file. Wraps the
/// full built <see cref="DeployablePlan"/> (component refs, image/asset upload lists) with the
/// bundle it was planned for and the catalog state it was diffed against, so <c>bundles publish
/// --from-plan</c> can skip the rebuild and detect staleness.
/// </summary>
public class BundlePlanFile : JsonSerializable.ISerializable
{
	/// <summary>The short bundle name this plan was generated for.</summary>
	public string bundleName;

	/// <summary>
	/// The published @latest bundle checksum at plan time ("" if never published). If the catalog's
	/// @latest differs at publish time, the plan is out of date.
	/// </summary>
	public string publishedChecksum;

	public DeployablePlan plan;
	public BundleDiffResult diff;

	public void Serialize(JsonSerializable.IStreamSerializer s)
	{
		s.Serialize(nameof(bundleName), ref bundleName);
		s.Serialize(nameof(publishedChecksum), ref publishedChecksum);
		s.Serialize(nameof(plan), ref plan);
		s.Serialize(nameof(diff), ref diff);
	}
}

/// <summary>
/// Save/load helpers for <see cref="BundlePlanFile"/>s, mirroring <see cref="DeployUtil"/>'s
/// plan-file handling but in a separate <c>temp/bundles-plans</c> folder (so bundle plans never get
/// picked up by <c>deploy release --from-latest-plan</c>) and with the bundle name in the file name.
/// </summary>
public static class BundlePlanUtil
{
	public static string GetBundlePlanTempFolder(IDependencyProvider provider)
	{
		var config = provider.GetService<ConfigService>();
		return Path.Combine(config.ConfigDirectoryPath, "temp", "bundles-plans");
	}

	public static string GetLatestBundlePlanFilePath(IDependencyProvider provider, string bundleName)
	{
		var logDir = GetBundlePlanTempFolder(provider);
		if (!Directory.Exists(logDir)) return null;
		var info = new DirectoryInfo(logDir);
		var file = info.GetFiles($"plan-{bundleName}-*").MaxBy(p => p.CreationTime);
		return file?.FullName;
	}

	public static async Task<string> SaveBundlePlanToTempFolder(IDependencyProvider provider, BundlePlanFile planFile)
	{
		var logDir = GetBundlePlanTempFolder(provider);
		Directory.CreateDirectory(logDir);

		{ // delete files so that only the most recent 10 exist.
			var info = new DirectoryInfo(logDir);
			var files = info.GetFiles().OrderByDescending(p => p.CreationTime).ToList();
			for (var i = 10; i < files.Count; i++)
			{
				Log.Verbose($"Deleting old bundle plan file=[{files[i].Name}]");
				files[i].Delete();
			}
		}

		var planJson = JsonSerializable.ToJson(planFile);
		var planPath = Path.Combine(logDir, $"plan-{planFile.bundleName}-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.plan.json").LocalizeSlashes();
		Log.Verbose($"Saving bundle plan: {planPath}");
		await File.WriteAllTextAsync(planPath, planJson);

		return planPath;
	}

	public static bool IsJsonABundlePlan(IDictionary<string, object> data)
	{
		return data != null
		       && data.ContainsKey(nameof(BundlePlanFile.bundleName))
		       && data.ContainsKey(nameof(BundlePlanFile.publishedChecksum))
		       && data.ContainsKey(nameof(BundlePlanFile.plan))
		       && data.ContainsKey(nameof(BundlePlanFile.diff))
			;
	}

	public static async Task<BundlePlanFile> LoadBundlePlanFile(string path, string expectedBundleName)
	{
		if (!File.Exists(path))
			throw new CliException($"The bundle plan file=[{path}] does not exist.");

		var json = await File.ReadAllTextAsync(path);
		var data = Json.Deserialize(json) as IDictionary<string, object>;
		if (!IsJsonABundlePlan(data))
		{
			throw new CliException(
				$"The file {path} does not contain a valid bundle plan. Use the `dotnet beam bundles plan` command to create one.");
		}

		var planFile = JsonSerializable.FromJson<BundlePlanFile>(json);
		if (planFile.bundleName != expectedBundleName)
		{
			throw new CliException(
				$"The plan file {path} was generated for bundle=[{planFile.bundleName}], not [{expectedBundleName}].");
		}

		return planFile;
	}

	public static void PrintBundlePlanNextSteps(string planFile, string bundleName, bool hasChanges)
	{
		if (hasChanges)
		{
			Log.Information($"To publish, use `dotnet beam bundles publish {bundleName} --plan {planFile}`");
		}
		else
		{
			Log.Information("There is nothing to publish; the catalog already matches this bundle. ");
		}
	}
}
