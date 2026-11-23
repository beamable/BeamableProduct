namespace cli.Services.PortalExtension;

/// <summary>
/// The build-time checks on an extension's site declarations (<c>beamable.sites</c>) and the site ids its
/// mounts list (<c>mounts[].siteIds</c>). Each finding is an error that fails the build: a site without an
/// id, two sites with one id, and a <c>#</c> or <c>@</c> in a site id. A missing description is never
/// reported.
/// <para>
/// The presentation and the site's data schemas (<c>static</c>, <c>input</c>, <c>output</c>, <c>events</c>,
/// <c>definitions</c>) are not checked here: they pass through to metadata.json and the portal checks them
/// when it loads the extension, so a new presentation or schema keyword needs no CLI release.
/// </para>
/// </summary>
public static class PortalExtensionSiteDeclarationChecks
{
	private static readonly char[] MalformedIdChars = { '#', '@' };

	public static List<string> GetErrors(PortalExtensionPackageProperties properties)
	{
		var errors = new List<string>();
		if (properties == null) return errors;

		var seenIds = new HashSet<string>();
		var sites = properties.Sites ?? new List<PortalExtensionSiteDeclaration>();
		for (var i = 0; i < sites.Count; i++)
		{
			var id = sites[i]?.Id;

			if (string.IsNullOrWhiteSpace(id))
				errors.Add($"sites[{i}] has no id.");
			else
			{
				if (id.IndexOfAny(MalformedIdChars) >= 0)
					errors.Add($"site '{id}': a site id is a bare name, with no '#' and no '@'.");
				if (!seenIds.Add(id))
					errors.Add($"site '{id}' is declared more than once in sites.");
			}
		}

		var mounts = properties.Mounts ?? new List<PortalExtensionMountProperties>();
		for (var i = 0; i < mounts.Count; i++)
		{
			foreach (var siteId in mounts[i]?.SiteIds ?? new List<string>())
			{
				if (string.IsNullOrWhiteSpace(siteId))
					errors.Add($"mounts[{i}].siteIds has an empty site id.");
				else if (siteId.IndexOfAny(MalformedIdChars) >= 0)
					errors.Add($"mounts[{i}].siteIds has '{siteId}': list bare site ids, with no '#' and no '@' (e.g. \"{siteId.TrimStart('#').Split('@')[0]}\").");
			}
		}

		return errors;
	}

	/// <summary>The errors as one message, or null when the declarations are valid.</summary>
	public static string GetErrorMessage(string extensionName, PortalExtensionPackageProperties properties)
	{
		var errors = GetErrors(properties);
		if (errors.Count == 0) return null;
		return $"Portal extension [{extensionName}] has invalid site declarations in its package.json:\n - " +
		       string.Join("\n - ", errors);
	}
}
