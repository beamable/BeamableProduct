using Beamable.Common;
using Beamable.Common.Api;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using UnityEngine;

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
		/// <typeparam name="TStorage"></typeparam>
		/// <returns></returns>
		Promise<IMongoDatabase> GetDatabase<TStorage>() where TStorage : MongoStorageObject;

		/// <summary>
		/// Get a MongoDB connection by the storageName from <see cref="StorageObjectAttribute"/> that decorates a <see cref="StorageObject"/> class
		/// </summary>
		/// <param name="name"></param>
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

		public StorageObjectConnectionProvider(IRealmInfo realmInfo, IBeamableRequester requester)
		{
			_realmInfo = realmInfo;
			_requester = requester;
		}

		public async Promise<IMongoDatabase> GetDatabase<TStorage>() where TStorage : MongoStorageObject
		{
			string storageName = string.Empty;
			var attributes = typeof(TStorage).GetCustomAttributes(true);
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
				BeamableLogger.LogError($"Cannot find storage name for type {typeof(TStorage)} ");
				return null;
			}

			return await GetDatabaseByStorageName(storageName);
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
			var connStr = await GetConnectionString(storageName);
			var client = new MongoClient(connStr);

			var db = client.GetDatabase($"{_realmInfo.CustomerID}{_realmInfo.ProjectName}_{storageName}");
			return db;
		}

		private async Task<string> GetConnectionString(string storageName)
		{
			string connStringName = string.Format(CONNSTR_VAR_NAME_FORMAT, storageName);
			string connectionString = Environment.GetEnvironmentVariable(connStringName);
			if (string.IsNullOrEmpty(connectionString))
			{
				// try to get the data from the running database.
				// TODO: cache this connection string for 30 minutes
				var res = await _requester.Request<ConnectionString>(Method.GET, "basic/beamo/storage/connection");
				connectionString = res.connectionString;
			}

			if (string.IsNullOrEmpty(connectionString))
			{
				BeamableLogger.LogError(
					$"Connection string to storage '{storageName}' is empty or not present in environment variables.");
			}
			return connectionString;
		}

		[Serializable]
		public class ConnectionString
		{
			public string connectionString;
		}
	}
}
