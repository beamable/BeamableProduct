namespace cli.Services.PortalExtension;

/// <summary>
/// Validates that portal extension names are unique. A portal extension name must not:
///  - duplicate another portal extension's name, and
///  - collide with a microservice or microstorage name.
///
/// Portal extensions, microservices and microstorages all share a single <see cref="BeamoServiceDefinition.BeamoId"/>
/// namespace, so any collision between them is a conflict. The same name is checked against both
/// local definitions and (during deploy) deployed ones.
/// </summary>
public static class PortalExtensionNameValidator
{
	private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

	/// <summary>
	/// Core conflict detector. Returns one human-readable message per conflict found.
	/// </summary>
	/// <param name="localPortalExtensions">Names of portal extensions that live in the local workspace.</param>
	/// <param name="deployedPortalExtensions">Names of portal extensions already deployed to the realm. A deployed
	/// extension that shares a local extension's name is the SAME extension (an update), not a conflict.</param>
	/// <param name="serviceAndStorageNames">All microservice and microstorage names that will exist (local and/or deployed).</param>
	public static List<string> FindConflicts(
		IReadOnlyCollection<string> localPortalExtensions,
		IReadOnlyCollection<string> deployedPortalExtensions,
		IReadOnlyCollection<string> serviceAndStorageNames)
	{
		var errors = new List<string>();

		// Two local portal extensions cannot share a name.
		foreach (var group in localPortalExtensions
			         .Where(n => !string.IsNullOrEmpty(n))
			         .GroupBy(n => n, NameComparer)
			         .Where(g => g.Count() > 1))
		{
			errors.Add(
				$"Duplicate portal extension name '{group.Key}'. Portal extension names must be unique.");
		}

		// No portal extension (local or deployed) may share a name with a microservice or microstorage.
		var serviceStorageSet = new HashSet<string>(
			serviceAndStorageNames.Where(n => !string.IsNullOrEmpty(n)), NameComparer);

		foreach (var peName in localPortalExtensions
			         .Concat(deployedPortalExtensions)
			         .Where(n => !string.IsNullOrEmpty(n))
			         .Distinct(NameComparer))
		{
			if (serviceStorageSet.Contains(peName))
			{
				errors.Add(
					$"Portal extension name '{peName}' conflicts with a microservice or storage of the same name. " +
					$"Names must be unique across microservices, storages, and portal extensions.");
			}
		}

		return errors;
	}

	/// <summary>
	/// Detects portal extension name conflicts purely from the LOCAL manifest (no network):
	/// two local portal extensions sharing a name, or a local portal extension colliding with a
	/// local microservice/microstorage. Used as a non-fatal warning when the manifest is loaded.
	/// </summary>
	public static List<string> FindLocalConflicts(BeamoLocalManifest manifest)
	{
		var localPortalExtensions = manifest.ServiceDefinitions
			.Where(d => d.Protocol == BeamoProtocolType.PortalExtension && d.IsLocal)
			.Select(d => d.BeamoId)
			.ToList();

		var serviceAndStorageNames = manifest.ServiceDefinitions
			.Where(d => d.IsLocal &&
			            (d.Protocol == BeamoProtocolType.HttpMicroservice ||
			             d.Protocol == BeamoProtocolType.EmbeddedMongoDb))
			.Select(d => d.BeamoId)
			.ToList();

		return FindConflicts(localPortalExtensions, Array.Empty<string>(), serviceAndStorageNames);
	}

	/// <summary>
	/// Checks whether a brand new portal extension <paramref name="name"/> would collide with any
	/// local service definition already known to the manifest. Used to fail fast before scaffolding.
	/// Deployed-only definitions are intentionally ignored here; those are validated at deploy time.
	/// </summary>
	public static bool TryGetConflictForNewName(BeamoLocalManifest manifest, string name, out string error)
	{
		var existing = manifest.ServiceDefinitions
			.FirstOrDefault(d => d.IsLocal && NameComparer.Equals(d.BeamoId, name));

		if (existing != null)
		{
			var kind = existing.Protocol switch
			{
				BeamoProtocolType.PortalExtension => "portal extension",
				BeamoProtocolType.EmbeddedMongoDb => "storage",
				_ => "microservice"
			};
			error =
				$"Cannot create portal extension '{name}': a {kind} with that name already exists. " +
				$"Names must be unique across microservices, storages, and portal extensions.";
			return true;
		}

		error = null;
		return false;
	}
}
