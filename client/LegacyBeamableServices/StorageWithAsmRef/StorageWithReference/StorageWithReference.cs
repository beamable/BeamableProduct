using Beamable.Common;
using MongoDB.Driver;

namespace Beamable.Server
{
	[StorageObject("StorageWithReference")]
	public class StorageWithReference : MongoStorageObject
	{
		public StorageReferenceFun x;
	}

	public static class StorageWithReferenceExtension
	{
		/// <summary>
		/// Get an authenticated MongoDB instance for StorageWithReference
		/// </summary>
		/// <returns></returns>
		public static Promise<IMongoDatabase> StorageWithReferenceDatabase(this IStorageObjectConnectionProvider provider)
			=> provider.GetDatabase<StorageWithReference>();

		/// <summary>
		/// Gets a MongoDB collection from StorageWithReference by the requested name, and uses the given mapping class.
		/// If you don't want to pass in a name, consider using <see cref="StorageWithReferenceCollection{TCollection}()"/>
		/// </summary>
		/// <param name="name">The name of the collection</param>
		/// <typeparam name="TCollection">The type of the mapping class</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		public static Promise<IMongoCollection<TCollection>> StorageWithReferenceCollection<TCollection>(
			this IStorageObjectConnectionProvider provider, string name)
			where TCollection : StorageDocument
			=> provider.GetCollection<StorageWithReference, TCollection>(name);

		/// <summary>
		/// Gets a MongoDB collection from StorageWithReference by the requested name, and uses the given mapping class.
		/// If you want to control the collection name separate from the class name, consider using <see cref="StorageWithReferenceCollection{TCollection}(string)"/>
		/// </summary>
		/// <param name="name">The name of the collection</param>
		/// <typeparam name="TCollection">The type of the mapping class</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		public static Promise<IMongoCollection<TCollection>> StorageWithReferenceCollection<TCollection>(
			this IStorageObjectConnectionProvider provider)
			where TCollection : StorageDocument
			=> provider.GetCollection<StorageWithReference, TCollection>();
	}
}
