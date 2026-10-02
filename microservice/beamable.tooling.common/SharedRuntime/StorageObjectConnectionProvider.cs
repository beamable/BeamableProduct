using Beamable.Common;
using Beamable.Common.Api;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace Beamable.Server
{
	public interface IStorageObjectConnectionProvider
	{
		/// <summary>
		/// Get a MongoDB connection for TStorage.
		/// <para>
		/// You can save time by using the <see cref="GetCollection{TStorage,TCollection}()"/> or <see cref="GetCollection{TStorage,TCollection}(string)"/> methods.
		/// </para>
		/// </summary>
		/// <param name="useCache">By default, the database connection is cached. If you pass `false` here, the database connection will be forced to reconnect.</param>
		/// <typeparam name="TStorage"></typeparam>
		/// <returns></returns>
		Promise<IMongoDatabase> GetDatabase<TStorage>(bool useCache = true) where TStorage : MongoStorageObject;

		/// <summary>
		/// Get a MongoDB connection by the storageName from <see cref="StorageObjectAttribute"/> that decorates a <see cref="StorageObject"/> class.
		/// This will never use the cached version.
		/// <b> This method is deprecated. You should be using the <see cref="GetDatabase{TStorage}"/> method instead </b>
		/// </summary>
		/// <param name="name"></param>
		[Obsolete("please use " + nameof(GetDatabase) + " instead")]
		Promise<IMongoDatabase> this[string name] { get; }

		/// <summary>
		/// Gets a MongoDB collection by the requested name, and uses the given mapping class.
		/// If you don't want to pass in a name, consider using <see cref="GetCollection{TStorage,TCollection}()"/>
		/// </summary>
		/// <param name="name">The name of the collection</param>
		/// <typeparam name="TStorage">The <see cref="StorageObject"/> type to get the collection from</typeparam>
		/// <typeparam name="TCollection">The type of the mapping class</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		Promise<IMongoCollection<TCollection>> GetCollection<TStorage, TCollection>(string name)
			where TStorage : MongoStorageObject
			where TCollection : StorageDocument;

		/// <summary>
		/// Gets a MongoDB collection for the mapping class given in TCollection. The collection will share the name of the mapping class.
		/// If you need to control the collection name separate from the mapping class name, consider using <see cref="GetCollection{TStorage,TCollection}(string)"/>
		/// </summary>
		/// <typeparam name="TStorage">The <see cref="StorageObject"/> type to get the collection from</typeparam>
		/// <typeparam name="TCollection">The type of collection to fetch</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		Promise<IMongoCollection<TCollection>> GetCollection<TStorage, TCollection>()
			where TStorage : MongoStorageObject
			where TCollection : StorageDocument;
	}

	public class StorageObjectConnectionProvider : IStorageObjectConnectionProvider
	{
		private readonly IRealmInfo _realmInfo;
		private readonly IBeamableRequester _requester;
		private const string CONNSTR_VAR_NAME_FORMAT = "STORAGE_CONNSTR_{0}";

		private ConcurrentDictionary<Type, Promise<IMongoDatabase>> _databaseCache =
			new ConcurrentDictionary<Type, Promise<IMongoDatabase>>();

		public StorageObjectConnectionProvider(IRealmInfo realmInfo, IBeamableRequester requester)
		{
			_realmInfo = realmInfo;
			_requester = requester;
		}

		public async Promise<IMongoDatabase> GetDatabase<TStorage>(bool useCache = true) where TStorage : MongoStorageObject
		{
			if (!useCache)
			{
				_databaseCache.TryRemove(typeof(TStorage), out _);
			}

			try
			{
				var db = await _databaseCache.GetOrAdd(typeof(TStorage), (type) =>
				{
					string storageName = string.Empty;
					var attributes = type.GetCustomAttributes(true);
					foreach (var attribute in attributes)
					{
						if (attribute is StorageObjectAttribute storageAttr)
						{
							storageName = storageAttr.StorageName;
							break;
						}
					}

					if (string.IsNullOrEmpty(storageName))
					{
						throw new StorageNotFoundException(storageName);
					}

					return GetDatabaseByStorageName(storageName);
				});

				return db;
			}
			catch (Exception ex)
			{
				BeamableLogger.LogError($"Failed to get IMongoDatabase instance for type {typeof(TStorage)} with error: {ex.Message}");
				_databaseCache.TryRemove(typeof(TStorage), out _);
				throw;
			}
		}

		public Promise<IMongoCollection<TCollection>> GetCollection<TStorage, TCollection>()
			where TStorage : MongoStorageObject
			where TCollection : StorageDocument
		{
			return GetCollection<TStorage, TCollection>(typeof(TCollection).Name);
		}

		public async Promise<IMongoCollection<TCollection>> GetCollection<TStorage, TCollection>(string collectionName)
			where TStorage : MongoStorageObject
			where TCollection : StorageDocument
		{
			var db = await GetDatabase<TStorage>();
			return db.GetCollection<TCollection>(collectionName);
		}

		public Promise<IMongoDatabase> this[string name] => GetDatabaseByStorageName(name);

		private async Promise<IMongoDatabase> GetDatabaseByStorageName(string storageName)
		{
			// MongoDB would reject the name on the first call with a bare InvalidNamespace; fail early and explain why.
			if (StorageDatabaseName.IsTooLong(_realmInfo.CustomerID, _realmInfo.ProjectName, storageName))
			{
				throw new StorageNameTooLongException(_realmInfo.CustomerID, _realmInfo.ProjectName, storageName);
			}

			var connStr = await GetConnectionString(storageName);

			var clientSettings = MongoClientSettings.FromConnectionString(connStr);
			clientSettings.ClusterConfigurator = cb =>
			{
				cb.Subscribe(new DiagnosticsActivityEventSubscriber(new InstrumentationOptions
				{
					// TODO allow for configuration?
				}));
			};
			var client = new MongoClient(clientSettings);
			
			var db = client.GetDatabase(StorageDatabaseName.Compose(_realmInfo.CustomerID, _realmInfo.ProjectName, storageName));
			return db;
		}

		private async Task<string> GetConnectionString(string storageName)
		{
			string connStringName = string.Format(CONNSTR_VAR_NAME_FORMAT, storageName);
			string connectionString = Environment.GetEnvironmentVariable(connStringName);
			if (string.IsNullOrEmpty(connectionString))
			{
				var res = await _requester.Request<ConnectionString>(Method.GET, "basic/beamo/storage/connection");
				connectionString = res.connectionString;
			}

			if (string.IsNullOrEmpty(connectionString))
			{
				throw new ConnectionStringException(storageName);
			}
			return connectionString;
		}

		[Serializable]
		public class ConnectionString
		{
			public string connectionString;
		}

		[Serializable]
		public class StorageNotFoundException : MicroserviceException
		{
			public StorageNotFoundException(string storageName) : base(500, "StorageNotFound", $"Database storage name '{storageName}' not found.") { }
		}

		[Serializable]
		public class ConnectionStringException : MicroserviceException
		{
			public ConnectionStringException(string storageName) : base(500, "InvalidConnectionString", $"Connection string for storage name '{storageName}' is null or empty.") { }
		}

		[Serializable]
		public class StorageNameTooLongException : MicroserviceException
		{
			public StorageNameTooLongException(string cid, string scope, string storageName) : base(500, "StorageNameTooLong", StorageDatabaseName.DescribeTooLong(cid, scope, storageName)) { }
		}
	}

	/// <summary>
	/// The MongoDB database name a storage object maps to: <c>{cid}{scope}_{storageName}</c>, where the scope is the
	/// pid for a realm-scoped storage and the zid for a zone-scoped one. MongoDB rejects database names longer than
	/// <see cref="MaxLength"/> characters (locally and on Atlas), so a storage name only fits when the whole composed
	/// name does. Shared by the microservice runtime and the CLI so both apply the same rule.
	/// </summary>
	public static class StorageDatabaseName
	{
		/// <summary>
		/// The longest database name MongoDB accepts.
		/// </summary>
		public const int MaxLength = 63;

		public static string Compose(string cid, string scope, string storageName) => $"{cid}{scope}_{storageName}";

		public static bool IsTooLong(string cid, string scope, string storageName) =>
			Compose(cid, scope, storageName).Length > MaxLength;

		/// <summary>
		/// The longest storage name that still fits for the given cid and scope (pid or zid).
		/// </summary>
		public static int MaxStorageNameLength(string cid, string scope) =>
			Math.Max(0, MaxLength - (cid?.Length ?? 0) - (scope?.Length ?? 0) - 1);

		/// <summary>
		/// A human-readable explanation of why the storage name doesn't fit, naming the composed database name,
		/// its length, and the longest storage name allowed for this cid and scope.
		/// </summary>
		public static string DescribeTooLong(string cid, string scope, string storageName)
		{
			var databaseName = Compose(cid, scope, storageName);
			var scopeKind = IsZoneId(scope) ? "zone" : "realm";
			return $"Storage [{storageName}] is too long for {scopeKind} [{scope}]: its MongoDB database name " +
			       $"[{databaseName}] would be {databaseName.Length} characters, but MongoDB allows at most {MaxLength}. " +
			       $"Storage names in this {scopeKind} can be at most {MaxStorageNameLength(cid, scope)} characters.";
		}

		private static bool IsZoneId(string scope) =>
			scope != null && scope.StartsWith("ZONE_", StringComparison.OrdinalIgnoreCase);
	}
}
