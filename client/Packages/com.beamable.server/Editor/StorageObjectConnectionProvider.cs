using System.Threading.Tasks;
using MongoDB.Driver;
using UnityEngine;

namespace Beamable.Server.Editor
{
    public class StorageObjectConnectionProvider
    {
        public async Task<MongoClient> GetClient<TStorage, TMicroservice>() where TStorage : MongoStorageObject where TMicroservice : Microservice
        {
            var storageDescriptor = Microservices.StorageDescriptors.Find(descriptor => descriptor.Type == typeof(TStorage));
            var microserviceDescriptor = Microservices.Descriptors.Find(descriptor => descriptor.Type == typeof(TMicroservice));

            if (storageDescriptor == null || microserviceDescriptor == null)
            {
                Debug.LogError($"Cannot find storage descriptor for type {typeof(TStorage)} " +
                               $"or microservice descriptor for type {typeof(TMicroservice)}");
                return null;
            }
            
            var connectionString = await Microservices.GetConnectionString(storageDescriptor, microserviceDescriptor);

            return new MongoClient(connectionString);
        }
    }
}