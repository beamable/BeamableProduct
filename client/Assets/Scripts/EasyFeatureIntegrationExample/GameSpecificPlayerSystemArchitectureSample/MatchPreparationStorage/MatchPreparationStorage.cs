using Beamable.Common;
using Beamable.Server;
using MongoDB.Driver;

namespace Beamable.EasyFeature.GameSpecificPlayerSystemArchitecture
{
	[StorageObject("MatchPreparationStorage")]
	public class MatchPreparationStorage : MongoStorageObject
	{
	}

	public static class MatchPreparationStorageExtension
	{
		/// <summary>
		/// Get an authenticated MongoDB instance for MatchPreparationStorage
		/// </summary>
		/// <returns></returns>
		public static Promise<IMongoDatabase> MatchPreparationStorageDatabase(this IStorageObjectConnectionProvider provider)
			=> provider.GetDatabase<MatchPreparationStorage>();

		/// <summary>
		/// Gets a MongoDB collection from MatchPreparationStorage by the requested name, and uses the given mapping class.
		/// If you don't want to pass in a name, consider using <see cref="MatchPreparationStorageCollection{TCollection}()"/>
		/// </summary>
		/// <param name="name">The name of the collection</param>
		/// <typeparam name="TCollection">The type of the mapping class</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		public static Promise<IMongoCollection<TCollection>> MatchPreparationStorageCollection<TCollection>(
			this IStorageObjectConnectionProvider provider, string name)
			where TCollection : StorageDocument
			=> provider.GetCollection<MatchPreparationStorage, TCollection>(name);

		/// <summary>
		/// Gets a MongoDB collection from MatchPreparationStorage by the requested name, and uses the given mapping class.
		/// If you want to control the collection name separate from the class name, consider using <see cref="MatchPreparationStorageCollection{TCollection}(string)"/>
		/// </summary>
		/// <param name="name">The name of the collection</param>
		/// <typeparam name="TCollection">The type of the mapping class</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		public static Promise<IMongoCollection<TCollection>> MatchPreparationStorageCollection<TCollection>(
			this IStorageObjectConnectionProvider provider)
			where TCollection : StorageDocument
			=> provider.GetCollection<MatchPreparationStorage, TCollection>();
	}
}
