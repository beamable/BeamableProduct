using System;
using System.Threading.Tasks;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice
{
	/// <summary>
	/// A storage's MongoDB database is <c>{cid}{pid}_{storageName}</c> (pid is the zid for zone services). MongoDB
	/// rejects names over 63 characters with a bare InvalidNamespace on the first call, so the provider must fail
	/// first with an error that names the storage.
	/// </summary>
	[TestFixture]
	public class StorageDatabaseNameTests
	{
		const string Cid = "1706624984549280";    // 16
		const string Pid = "DE_1706624984549283"; // 19 -> storage names up to 27 characters fit

		class RealmInfo : IRealmInfo
		{
			public string CustomerID => Cid;
			public string ProjectName => Pid;
		}

#pragma warning disable CS0618 // the by-name indexer is the direct path to the database lookup
		static async Task<MongoDB.Driver.IMongoDatabase> GetDatabase(string storageName) =>
			await new StorageObjectConnectionProvider(new RealmInfo(), null)[storageName];
#pragma warning restore CS0618

		[Test]
		public void GetDatabase_WhenNameIsTooLong_ThrowsStorageNameTooLong()
		{
			var storageName = new string('a', 28);

			var ex = Assert.ThrowsAsync<StorageObjectConnectionProvider.StorageNameTooLongException>(
				() => GetDatabase(storageName));
			Assert.That(ex.Message, Does.Contain($"[{storageName}]"));
			Assert.That(ex.Message, Does.Contain("at most 27 characters"));
		}

		[Test]
		public async Task GetDatabase_AtTheLimit_UsesTheComposedName()
		{
			var storageName = new string('b', 27);
			var connStrVar = $"STORAGE_CONNSTR_{storageName}";
			Environment.SetEnvironmentVariable(connStrVar, "mongodb://localhost:27017");
			try
			{
				var db = await GetDatabase(storageName);
				Assert.That(db.DatabaseNamespace.DatabaseName, Is.EqualTo($"{Cid}{Pid}_{storageName}"));
				Assert.That(db.DatabaseNamespace.DatabaseName.Length, Is.EqualTo(StorageDatabaseName.MaxLength));
			}
			finally
			{
				Environment.SetEnvironmentVariable(connStrVar, null);
			}
		}
	}
}
