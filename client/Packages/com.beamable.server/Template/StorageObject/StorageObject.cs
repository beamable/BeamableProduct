using Beamable.Common;
using Beamable.Server;
using MongoDB.Driver;

namespace Beamable.Server
{
    [StorageObject("XXXX")]
    public class XXXX : MongoStorageObject
    {
    }

    public static class XXXXExtension
    {
        public static Promise<IMongoDatabase> GetXXXX(this IStorageObjectConnectionProvider provider)
            => provider.GetDatabase<XXXX>();

        public static Promise<IMongoCollection<TCollection>> CollectionFromXXXX<TCollection>(
            this IStorageObjectConnectionProvider provider, string name)
            => provider.GetCollection<XXXX, TCollection>(name);
    }
}
