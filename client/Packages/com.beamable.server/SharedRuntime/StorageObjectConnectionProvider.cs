using System;
using Beamable.Common;
using Beamable.Common.Api;
using MongoDB.Driver;
using System.Threading.Tasks;
using UnityEngine;

namespace Beamable.Server
{
    public interface IStorageObjectConnectionProvider
    {
        Promise<IMongoDatabase> GetDatabase<TStorage>() where TStorage : MongoStorageObject;
        Promise<IMongoDatabase> this[string name] { get; }

        Promise<IMongoCollection<TCollection>> GetCollection<TStorage, TCollection>(string name)
            where TStorage : MongoStorageObject;
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

        public async Promise<IMongoCollection<TCollection>> GetCollection<TStorage, TCollection>(string collectionName)
            where TStorage : MongoStorageObject
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
