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
		public static IMongoDatabase GetXXXX(this IStorageObjectConnectionProvider provider) =>
			provider.GetDatabase<XXXX>();

		public static IMongoCollection<TCollection> CollectionFromXXXX<TCollection>(
			this IStorageObjectConnectionProvider provider,
			string name) =>
			provider.GetCollection<XXXX, TCollection>(name);
	}
}
