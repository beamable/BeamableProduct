using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using cli.Services;
using cli.Services.Bundles;

namespace cli.BundleCommands;

/// <summary>
/// CLI-owned projection of a bundle authored in this workspace (<c>*.beam.bundle.json</c>), with each
/// component classified from its project.
/// </summary>
[Serializable]
public class BundleInfo
{
	public string name;

	/// <summary>Absolute path of the authored config file.</summary>
	public string filePath = "";

	public BundleComponentInfo[] components = Array.Empty<BundleComponentInfo>();

	/// <summary>Component group names, each with its components.</summary>
	public Dictionary<string, string[]> componentGroups = new Dictionary<string, string[]>();

	/// <summary>Bundle name to "hard 1+" or "soft 1-5".</summary>
	public Dictionary<string, string> dependencies = new Dictionary<string, string>();

	public static BundleInfo FromLocalBundle(BundleConfigFile bundle, BeamoLocalManifest manifest, string fullName)
	{
		return new BundleInfo
		{
			name = fullName,
			filePath = bundle.filePath ?? "",
			components = bundle.components.Select(c =>
			{
				var definition = manifest?.ServiceDefinitions?.FirstOrDefault(d => d.BeamoId == c.Key);
				return new BundleComponentInfo
				{
					name = c.Key,
					kind = definition?.Protocol switch
					{
						BeamoProtocolType.HttpMicroservice => "service",
						BeamoProtocolType.EmbeddedMongoDb => "storage",
						BeamoProtocolType.PortalExtension => "extension",
						_ => "unknown",
					},
					site = BundleWorkspace.SiteOf(manifest, bundle, c.Key),
					source = c.Value.source,
				};
			}).ToArray(),
			componentGroups = bundle.componentGroups.ToDictionary(g => g.Key, g => g.Value.components.ToArray()),
			dependencies = bundle.dependencies.ToDictionary(d => d.Key, d => d.Value.Format()),
		};
	}

	/// <summary>
	/// Convert a Unix epoch (seconds) timestamp into a human-readable date for terminal display.
	/// Returns "-" for zero.
	/// </summary>
	public static string FormatPublishedAt(long publishedAt)
	{
		if (publishedAt <= 0)
		{
			return "-";
		}

		return DateTimeOffset.FromUnixTimeSeconds(publishedAt)
			.ToLocalTime()
			.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
	}
}

[Serializable]
public class BundleComponentInfo
{
	public string name;

	/// <summary>"service", "storage" or "extension".</summary>
	public string kind;

	/// <summary>"realm" or "zone".</summary>
	public string site;

	/// <summary>Whether orgs that acquire the bundle may get this component's source.</summary>
	public bool source;
}
