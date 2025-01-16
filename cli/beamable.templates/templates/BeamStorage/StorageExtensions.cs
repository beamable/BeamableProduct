using Beamable.Common;
using MongoDB.Driver;

namespace Beamable.Server
{
	public static class BeamStorageExtension
	{
		/// <summary>
		/// Get an authenticated MongoDB instance for BeamStorage
		/// </summary>
		/// <returns></returns>
		public static Promise<IMongoDatabase> BeamStorageDatabase(
			this IStorageObjectConnectionProvider provider)
			=> provider.GetDatabase<BeamStorage>();

		/// <summary>
		/// Gets a MongoDB collection from BeamStorage by the requested name, and uses the given mapping class.
		/// If you don't want to pass in a name, consider using <see cref="BeamStorageCollection{TCollection}()"/>
		/// </summary>
		/// <param name="name">The name of the collection</param>
		/// <typeparam name="TCollection">The type of the mapping class</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		public static Promise<IMongoCollection<TCollection>> BeamStorageCollection<TCollection>(
			this IStorageObjectConnectionProvider provider, string name)
			where TCollection : StorageDocument
			=> provider.GetCollection<BeamStorage, TCollection>(name);

		/// <summary>
		/// Gets a MongoDB collection from BeamStorage by the requested name, and uses the given mapping class.
		/// If you want to control the collection name separate from the class name, consider using <see cref="BeamStorageCollection{TCollection}(string)"/>
		/// </summary>
		/// <param name="name">The name of the collection</param>
		/// <typeparam name="TCollection">The type of the mapping class</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		public static Promise<IMongoCollection<TCollection>> BeamStorageCollection<TCollection>(
			this IStorageObjectConnectionProvider provider)
			where TCollection : StorageDocument
			=> provider.GetCollection<BeamStorage, TCollection>();
	}
}
