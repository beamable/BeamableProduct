using Beamable.Server;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Beamable.Server
{
    [StorageObject("AliveStorage")]
    public class AliveStorage : MongoStorageObject
    {
    }

    public static class AliveStorageExtension
    {
        public static IMongoDatabase GetAliveStorage(this IStorageObjectConnectionProvider provider)
            => provider.GetDatabase<AliveStorage>();

        public static IMongoCollection<TCollection> CollectionFromAliveStorage<TCollection>(
            this IStorageObjectConnectionProvider provider, string name)
            => provider.GetCollection<AliveStorage, TCollection>(name);
    }

    public class Data
    {
	    public ObjectId id;
	    public int a;
    }
}
