using Beamable.Common;
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
        public static Promise<IMongoDatabase> GetAliveStorage(this IStorageObjectConnectionProvider provider)
            => provider.GetDatabase<AliveStorage>();

        public static Promise<IMongoCollection<TCollection>> CollectionFromAliveStorage<TCollection>(
            this IStorageObjectConnectionProvider provider, string name)
            => provider.GetCollection<AliveStorage, TCollection>(name);
    }

    public class Data
    {
	    public ObjectId id;
	    public int a;
    }
}
