using Beamable.Server;

namespace cli.Services;

/// <summary>
/// Validates that local storage names fit in a MongoDB database name. A storage maps to the database
/// <c>{cid}{scope}_{storageName}</c> (see <see cref="StorageDatabaseName"/>), where the scope is the pid for a realm
/// storage and the zid for a zone storage, so how long a storage name may be depends on the target cid and scope.
/// MongoDB rejects the name locally and remotely alike, so this guards both local runs and deploys.
/// </summary>
public static class StorageNameValidator
{
	// Worst-case ids are snowflake longs of up to 19 digits: cid (19), pid "DE_" + 19, zid "ZONE_" + 19.
	private const int WorstCaseCidLength = 19;
	public const int WorstCaseRealmStorageNameLength = StorageDatabaseName.MaxLength - WorstCaseCidLength - 22 - 1;
	public const int WorstCaseZoneStorageNameLength = StorageDatabaseName.MaxLength - WorstCaseCidLength - 24 - 1;

	/// <summary>
	/// Validates the name of a storage about to be created. With a known cid and scope (pid, or zid for a zone
	/// storage) a name that doesn't fit throws; without them, the name is only compared against the worst-case
	/// budget and a warning is logged, since the target isn't known yet.
	/// </summary>
	public static void ValidateNewStorage(string cid, string scope, string storageName, bool isZone)
	{
		if (IsNumericCid(cid) && !string.IsNullOrEmpty(scope))
		{
			if (StorageDatabaseName.IsTooLong(cid, scope, storageName))
			{
				ThrowIfAny(new List<string> { StorageDatabaseName.DescribeTooLong(cid, scope, storageName) });
			}

			return;
		}

		var worstCase = isZone ? WorstCaseZoneStorageNameLength : WorstCaseRealmStorageNameLength;
		if (storageName.Length > worstCase)
		{
			Log.Warning($"Storage [{storageName}] is {storageName.Length} characters long. Its MongoDB database name " +
			            $"is {{cid}}{{{(isZone ? "zid" : "pid")}}}_{{storageName}} (at most {StorageDatabaseName.MaxLength} " +
			            $"characters), and the {(isZone ? "zone" : "realm")} isn't known yet, so it can't be checked. " +
			            $"Names up to {worstCase} characters always fit; a longer name may fail once you run or deploy it.");
		}
	}

	/// <summary>
	/// Returns one message per local storage whose database name would be too long. Realm storages are checked
	/// against <paramref name="pid"/> and zone storages against <paramref name="zid"/>; a storage whose scope id is
	/// unknown (null/empty) is skipped, as is everything when <paramref name="cid"/> isn't a numeric customer id.
	/// </summary>
	/// <param name="definitions">The manifest's definitions; only local storages are checked.</param>
	/// <param name="includeOnlyBeamoIds">When set, only these storages are checked.</param>
	public static List<string> FindTooLong(
		IEnumerable<BeamoServiceDefinition> definitions,
		string cid,
		string pid,
		string zid,
		ICollection<string> includeOnlyBeamoIds = null)
	{
		var errors = new List<string>();
		if (!IsNumericCid(cid))
		{
			return errors;
		}

		foreach (var definition in definitions)
		{
			if (!definition.IsLocal || definition.Protocol != BeamoProtocolType.EmbeddedMongoDb)
				continue;
			if (includeOnlyBeamoIds != null && !includeOnlyBeamoIds.Contains(definition.BeamoId))
				continue;

			var scope = definition.IsZoneScoped ? zid : pid;
			if (string.IsNullOrEmpty(scope))
			{
				Log.Trace($"Skipping storage name length check for storage=[{definition.BeamoId}]; no " +
				            $"{(definition.IsZoneScoped ? "zid" : "pid")} is known.");
				continue;
			}

			if (StorageDatabaseName.IsTooLong(cid, scope, definition.BeamoId))
			{
				errors.Add(StorageDatabaseName.DescribeTooLong(cid, scope, definition.BeamoId));
			}
		}

		return errors;
	}

	/// <summary>
	/// Throws a single <see cref="CliException"/> listing every storage in <paramref name="errors"/>, if any.
	/// </summary>
	public static void ThrowIfAny(List<string> errors)
	{
		if (errors.Count == 0)
		{
			return;
		}

		var lines = string.Join(Environment.NewLine, errors.Select(e => "  - " + e));
		throw new CliException(
			$"Some storage names are too long for MongoDB:{Environment.NewLine}{lines}{Environment.NewLine}" +
			"Rename these storages to fit: change the <BeamId> property (or the .csproj name when <BeamId> isn't set) " +
			"and the matching [StorageObject] attribute name.");
	}

	private static bool IsNumericCid(string cid) => !string.IsNullOrEmpty(cid) && cid.All(char.IsDigit);
}
