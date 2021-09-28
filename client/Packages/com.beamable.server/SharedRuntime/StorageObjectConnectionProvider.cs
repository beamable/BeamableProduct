using System;
using Beamable.Common;
using MongoDB.Driver;

namespace Beamable.Server
{
    public class StorageObjectConnectionProvider
    {
        private const string CONNSTR_VAR_NAME_FORMAT = "STORAGE_CONNSTR_{0}";
        
        public MongoClient GetClient<TStorage>() where TStorage : MongoStorageObject
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
            
            return new MongoClient(GetConnectionString(storageName));
        }

        private string GetConnectionString(string storageName)
        {
            string connStringName = string.Format(CONNSTR_VAR_NAME_FORMAT, storageName);
            string connectionString = Environment.GetEnvironmentVariable(connStringName);
            
            if (string.IsNullOrEmpty(connectionString))
            {
                BeamableLogger.LogError($"Connection string to storage '{storageName}' is empty or not present in environment variables.");
            }
            
            return connectionString;
        }
    }
}