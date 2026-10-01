using System.Collections.Generic;
using Beamable.Server;
using cli;
using cli.Services;
using NUnit.Framework;

namespace tests;

/// <summary>
/// A storage maps to the MongoDB database <c>{cid}{scope}_{storageName}</c> (scope = pid for realm storages, zid for
/// zone storages), and MongoDB rejects names longer than 63 characters, locally and remotely. The CLI must catch a
/// name that doesn't fit before running or deploying it.
/// </summary>
[TestFixture]
public class StorageNameValidatorTests
{
	const string Cid = "1706624984549280";        // 16
	const string Pid = "DE_1706624984549283";     // 19 -> realm budget 63 - 16 - 19 - 1 = 27
	const string Zid = "ZONE_100000000000000000"; // 23 -> zone budget 63 - 16 - 23 - 1 = 23

	static BeamoServiceDefinition Storage(string beamoId, bool zone = false, bool local = true) => new()
	{
		BeamoId = beamoId,
		Protocol = BeamoProtocolType.EmbeddedMongoDb,
		ServiceScope = zone ? "zone" : null,
		ProjectPath = local ? $"services/{beamoId}/{beamoId}.csproj" : null,
	};

	static BeamoServiceDefinition Service(string beamoId) => new()
	{
		BeamoId = beamoId,
		Protocol = BeamoProtocolType.HttpMicroservice,
		ProjectPath = $"services/{beamoId}/{beamoId}.csproj",
	};

	// ---- StorageDatabaseName ----

	[Test]
	public void Compose_ConcatenatesCidScopeAndName()
	{
		Assert.That(StorageDatabaseName.Compose(Cid, Pid, "Inv"), Is.EqualTo($"{Cid}{Pid}_Inv"));
	}

	[Test]
	public void MaxStorageNameLength_IsWhatIsLeftOfTheDatabaseName()
	{
		Assert.That(StorageDatabaseName.MaxStorageNameLength(Cid, Pid), Is.EqualTo(27));
		Assert.That(StorageDatabaseName.MaxStorageNameLength(Cid, Zid), Is.EqualTo(23));
	}

	[Test]
	public void IsTooLong_At63Characters_Fits()
	{
		var name = new string('a', 27);
		Assert.That(StorageDatabaseName.Compose(Cid, Pid, name).Length, Is.EqualTo(63));
		Assert.That(StorageDatabaseName.IsTooLong(Cid, Pid, name), Is.False);
	}

	[Test]
	public void IsTooLong_At64Characters_DoesNotFit()
	{
		Assert.That(StorageDatabaseName.IsTooLong(Cid, Pid, new string('a', 28)), Is.True);
	}

	[Test]
	public void DescribeTooLong_NamesTheStorageDatabaseAndBudget()
	{
		var name = new string('a', 24);
		var message = StorageDatabaseName.DescribeTooLong(Cid, Zid, name);

		Assert.That(message, Does.Contain($"[{name}]"));
		Assert.That(message, Does.Contain($"zone [{Zid}]"));
		Assert.That(message, Does.Contain($"[{Cid}{Zid}_{name}]"));
		Assert.That(message, Does.Contain("64 characters"));
		Assert.That(message, Does.Contain("at most 23 characters"));
	}

	// ---- FindTooLong ----

	[Test]
	public void FindTooLong_ChecksRealmStoragesAgainstPidAndZoneStoragesAgainstZid()
	{
		// 25 characters fits the realm (27) but not the zone (23).
		var name = new string('a', 25);
		var errors = StorageNameValidator.FindTooLong(
			new[] { Storage(name), Storage("Z" + name, zone: true) }, Cid, Pid, Zid);

		Assert.That(errors, Has.Count.EqualTo(1));
		Assert.That(errors[0], Does.Contain($"[Z{name}]"));
	}

	[Test]
	public void FindTooLong_SkipsZoneStoragesWhenNoZidIsKnown()
	{
		var errors = StorageNameValidator.FindTooLong(
			new[] { Storage(new string('a', 40), zone: true) }, Cid, Pid, zid: null);

		Assert.That(errors, Is.Empty);
	}

	[Test]
	public void FindTooLong_SkipsEverythingWithoutANumericCid()
	{
		var definitions = new[] { Storage(new string('a', 40)) };

		Assert.That(StorageNameValidator.FindTooLong(definitions, null, Pid, Zid), Is.Empty);
		Assert.That(StorageNameValidator.FindTooLong(definitions, "my-alias", Pid, Zid), Is.Empty);
	}

	[Test]
	public void FindTooLong_IgnoresServicesAndRemoteOnlyStorages()
	{
		var longName = new string('a', 40);
		var errors = StorageNameValidator.FindTooLong(
			new[] { Service(longName), Storage("R" + longName, local: false) }, Cid, Pid, Zid);

		Assert.That(errors, Is.Empty);
	}

	[Test]
	public void FindTooLong_OnlyChecksIncludedStorages()
	{
		var tooLong = new string('a', 30);
		var definitions = new[] { Storage(tooLong), Storage("Other") };

		Assert.That(StorageNameValidator.FindTooLong(definitions, Cid, Pid, Zid, new HashSet<string> { "Other" }),
			Is.Empty);
	}

	[Test]
	public void ThrowIfAny_ListsEveryOffenderInOneError()
	{
		var errors = StorageNameValidator.FindTooLong(
			new[] { Storage(new string('a', 30)), Storage(new string('b', 30)) }, Cid, Pid, Zid);

		var ex = Assert.Throws<CliException>(() => StorageNameValidator.ThrowIfAny(errors));
		Assert.That(ex!.Message, Does.Contain(new string('a', 30)));
		Assert.That(ex.Message, Does.Contain(new string('b', 30)));
	}

	[Test]
	public void ThrowIfAny_WithNoErrors_DoesNothing()
	{
		Assert.DoesNotThrow(() => StorageNameValidator.ThrowIfAny(new List<string>()));
	}

	// ---- ValidateNewStorage ----

	[Test]
	public void ValidateNewStorage_WithKnownTarget_ThrowsWhenTooLong()
	{
		Assert.Throws<CliException>(() =>
			StorageNameValidator.ValidateNewStorage(Cid, Zid, new string('a', 24), isZone: true));
	}

	[Test]
	public void ValidateNewStorage_WithKnownTarget_PassesAtTheLimit()
	{
		Assert.DoesNotThrow(() =>
			StorageNameValidator.ValidateNewStorage(Cid, Zid, new string('a', 23), isZone: true));
	}

	[Test]
	public void ValidateNewStorage_WithoutTarget_OnlyWarns()
	{
		Assert.DoesNotThrow(() =>
			StorageNameValidator.ValidateNewStorage(null, null, new string('a', 40), isZone: true));
	}

	[Test]
	public void WorstCaseBudgets_MatchTheLongestPossibleIds()
	{
		// cid 19 digits, pid "DE_" + 19, zid "ZONE_" + 19.
		Assert.That(StorageNameValidator.WorstCaseRealmStorageNameLength, Is.EqualTo(21));
		Assert.That(StorageNameValidator.WorstCaseZoneStorageNameLength, Is.EqualTo(19));
	}
}
