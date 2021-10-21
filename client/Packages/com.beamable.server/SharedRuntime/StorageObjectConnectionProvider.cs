using System;
using Beamable.Common;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Beamable.Server
{
    public interface IStorageObjectConnectionProvider
    {
        IMongoDatabase GetDatabase<TStorage>() where TStorage : MongoStorageObject;
        IMongoDatabase this[string name] { get; }

        IMongoCollection<TCollection> GetCollection<TStorage, TCollection>(string name)
            where TStorage : MongoStorageObject;

        IMongoCollection<BsonDocument> GetBsonCollection<TStorage>(string collectionName)
            where TStorage : MongoStorageObject;
    }

    public class StorageObjectConnectionProvider : IStorageObjectConnectionProvider
    {
        private readonly IRealmInfo _realmInfo;
        private const string CONNSTR_VAR_NAME_FORMAT = "STORAGE_CONNSTR_{0}";

        public StorageObjectConnectionProvider(IRealmInfo realmInfo)
        {
            _realmInfo = realmInfo;
        }

        public IMongoDatabase GetDatabase<TStorage>() where TStorage : MongoStorageObject
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

            return GetDatabaseByStorageName(storageName);
        }

        public IMongoCollection<TCollection> GetCollection<TStorage, TCollection>(string collectionName)
            where TStorage : MongoStorageObject
        {
            return GetDatabase<TStorage>().GetCollection<TCollection>(collectionName);
        }

        public IMongoCollection<BsonDocument> GetBsonCollection<TStorage>(string collectionName)
            where TStorage : MongoStorageObject
        {
            return GetDatabase<TStorage>().GetCollection<BsonDocument>(collectionName);
        }

        public IMongoDatabase this[string name] => GetDatabaseByStorageName(name);

        private IMongoDatabase GetDatabaseByStorageName(string storageName)
        {
            var client = new MongoClient(GetConnectionString(storageName));
            var db = client.GetDatabase($"{_realmInfo.CustomerID}_{_realmInfo.ProjectName}");
            return db;
        }

        private string GetConnectionString(string storageName)
        {
            string connStringName = string.Format(CONNSTR_VAR_NAME_FORMAT, storageName);
            string connectionString = Environment.GetEnvironmentVariable(connStringName);

            if (string.IsNullOrEmpty(connectionString))
            {
                BeamableLogger.LogError(
                    $"Connection string to storage '{storageName}' is empty or not present in environment variables.");
            }

            return connectionString;
        }
    }
}