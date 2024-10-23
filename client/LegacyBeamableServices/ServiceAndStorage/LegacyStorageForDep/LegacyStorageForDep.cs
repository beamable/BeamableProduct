using Beamable.Common;
using MongoDB.Driver;

namespace Beamable.Server
{
	[StorageObject("LegacyStorageForDep")]
	public class LegacyStorageForDep : MongoStorageObject
	{
	}

	public class LegacyDoc : StorageDocument
	{
		public int x;
	}

	public static class LegacyStorageForDepExtension
	{
		/// <summary>
		/// Get an authenticated MongoDB instance for LegacyStorageForDep
		/// </summary>
		/// <returns></returns>
		public static Promise<IMongoDatabase> LegacyStorageForDepDatabase(this IStorageObjectConnectionProvider provider)
			=> provider.GetDatabase<LegacyStorageForDep>();

		/// <summary>
		/// Gets a MongoDB collection from LegacyStorageForDep by the requested name, and uses the given mapping class.
		/// If you don't want to pass in a name, consider using <see cref="LegacyStorageForDepCollection{TCollection}()"/>
		/// </summary>
		/// <param name="name">The name of the collection</param>
		/// <typeparam name="TCollection">The type of the mapping class</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		public static Promise<IMongoCollection<TCollection>> LegacyStorageForDepCollection<TCollection>(
			this IStorageObjectConnectionProvider provider, string name)
			where TCollection : StorageDocument
			=> provider.GetCollection<LegacyStorageForDep, TCollection>(name);

		/// <summary>
		/// Gets a MongoDB collection from LegacyStorageForDep by the requested name, and uses the given mapping class.
		/// If you want to control the collection name separate from the class name, consider using <see cref="LegacyStorageForDepCollection{TCollection}(string)"/>
		/// </summary>
		/// <param name="name">The name of the collection</param>
		/// <typeparam name="TCollection">The type of the mapping class</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		public static Promise<IMongoCollection<TCollection>> LegacyStorageForDepCollection<TCollection>(
			this IStorageObjectConnectionProvider provider)
			where TCollection : StorageDocument
			=> provider.GetCollection<LegacyStorageForDep, TCollection>();
	}
}
