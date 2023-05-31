using Beamable.Common;
using Beamable.Server;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beamable.Microservices
{
	public static class MongoDbExtensions
	{
		#region Indexes

		public enum IndexType
		{
			Ascending,
			Descending,
			Text,
			Geo2D,
			Geo2DSphere,
			Hashed,
			Wildcard,
		}

		/// <summary>
		/// Method to create single mongo index for a single field 
		/// </summary>
		/// <param name="collection">Collection that index should be created for</param>
		/// <param name="type">Type of index to create</param>
		/// <param name="fieldName">Field name that index will be created for</param>
		/// <param name="indexName">(Optional) Custom index name, if left empty index will be named after <see cref="IndexType"/> passed formatted to lower</param>
		/// <typeparam name="T">Constrained to a class that derives from <see cref="StorageDocument"/></typeparam>
		public static async Task CreateSingleIndex<T>(this IMongoCollection<T> collection,
		                                              IndexType type,
		                                              string fieldName,
		                                              string indexName = "") where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(type, fieldName);
			await collection.CreateIndex(indexKeysDefinition,
			                             string.IsNullOrEmpty(indexName) ? type.ToString().ToLower() : indexName);
		}
		
		/// <summary>
		/// Method to create compound index containing set of supported mongo indexes
		/// </summary>
		/// <param name="collection">Collection that index should be created for</param>
		/// <param name="indexes">Dictionary containing pairs of <see cref="IndexType"/> for index type and <see cref="string"/> for indexed field</param>
		/// <param name="indexName">(Optional) Custom index name, if left empty index will be named as default "compound" name</param>
		/// <typeparam name="T">Constrained to a class that derives from <see cref="StorageDocument"/></typeparam>
		public static async Task CreateCompoundIndex<T>(this IMongoCollection<T> collection,
		                                                Dictionary<IndexType, string> indexes,
		                                                string indexName = "compound") where T : StorageDocument
		{
			var definitions = new List<IndexKeysDefinition<T>>();

			foreach (var index in indexes)
			{
				var indexKeysDefinition = BuildDefinition<T>(index.Key, index.Value);
				definitions.Add(indexKeysDefinition);
			}

			var keysDefinition = Builders<T>.IndexKeys.Combine(definitions);
			await collection.CreateIndex(keysDefinition, indexName);
		}

		/// <summary>
		/// Method to create single ascending index for a field
		/// </summary>
		/// <param name="collection">Collection that index should be created for</param>
		/// <param name="fieldName">Field name that index will be created for</param>
		/// <param name="indexName">(Optional) Custom index name, if left empty index will be named as default "ascending" name</param>
		/// <typeparam name="T">Constrained to a class that derives from <see cref="StorageDocument"/></typeparam>
		public static async Task CreateAscendingIndex<T>(this IMongoCollection<T> collection,
		                                                 string fieldName,
		                                                 string indexName = "ascending")
			where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(IndexType.Ascending, fieldName);
			await collection.CreateIndex(indexKeysDefinition, indexName);
		}

		/// <summary>
		/// Method to create single descending index for a field
		/// </summary>
		/// <param name="collection">Collection that index should be created for</param>
		/// <param name="fieldName">Field name that index will be created for</param>
		/// <param name="indexName">(Optional) Custom index name, if left empty index will be named as default "descending" name</param>
		/// <typeparam name="T">Constrained to a class that derives from <see cref="StorageDocument"/></typeparam>
		public static async Task CreateDescendingIndex<T>(this IMongoCollection<T> collection,
		                                                  string fieldName,
		                                                  string indexName = "descending")
			where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(IndexType.Descending, fieldName);
			await collection.CreateIndex(indexKeysDefinition, indexName);
		}

		/// <summary>
		/// Method to create single ascending text for a field
		/// </summary>
		/// <param name="collection">Collection that index should be created for</param>
		/// <param name="fieldName">Field name that index will be created for</param>
		/// <param name="indexName">(Optional) Custom index name, if left empty index will be named as default "text" name</param>
		/// <typeparam name="T">Constrained to a class that derives from <see cref="StorageDocument"/></typeparam>
		public static async Task CreateTextIndex<T>(this IMongoCollection<T> collection,
		                                            string fieldName,
		                                            string indexName = "text")
			where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(IndexType.Text, fieldName);
			await collection.CreateIndex(indexKeysDefinition, indexName);
		}

		/// <summary>
		/// Method to create single geo2D index for a field
		/// </summary>
		/// <param name="collection">Collection that index should be created for</param>
		/// <param name="fieldName">Field name that index will be created for</param>
		/// <param name="indexName">(Optional) Custom index name, if left empty index will be named as default "geo2D" name</param>
		/// <typeparam name="T">Constrained to a class that derives from <see cref="StorageDocument"/></typeparam>
		public static async Task CreateGeo2DIndex<T>(this IMongoCollection<T> collection,
		                                             string fieldName,
		                                             string indexName = "geo2D")
			where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(IndexType.Geo2D, fieldName);
			await collection.CreateIndex(indexKeysDefinition, indexName);
		}

		/// <summary>
		/// Method to create single geo2DSphere index for a field
		/// </summary>
		/// <param name="collection">Collection that index should be created for</param>
		/// <param name="fieldName">Field name that index will be created for</param>
		/// <param name="indexName">(Optional) Custom index name, if left empty index will be named as default "geo2DSphere" name</param>
		/// <typeparam name="T">Constrained to a class that derives from <see cref="StorageDocument"/></typeparam>
		public static async Task CreateGeo2DSphereIndex<T>(this IMongoCollection<T> collection,
		                                                   string fieldName,
		                                                   string indexName = "geo2Dsphere")
			where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(IndexType.Geo2DSphere, fieldName);
			await collection.CreateIndex(indexKeysDefinition, indexName);
		}

		/// <summary>
		/// Method to create single hashed index for a field
		/// </summary>
		/// <param name="collection">Collection that index should be created for</param>
		/// <param name="fieldName">Field name that index will be created for</param>
		/// <param name="indexName">(Optional) Custom index name, if left empty index will be named as default "hashed" name</param>
		/// <typeparam name="T">Constrained to a class that derives from <see cref="StorageDocument"/></typeparam>
		public static async Task CreateHashedIndex<T>(this IMongoCollection<T> collection,
		                                              string fieldName,
		                                              string indexName = "hashed")
			where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(IndexType.Hashed, fieldName);
			await collection.CreateIndex(indexKeysDefinition, indexName);
		}

		/// <summary>
		/// Method to create single wildcard index for a field
		/// </summary>
		/// <param name="collection">Collection that index should be created for</param>
		/// <param name="fieldName">Field name that index will be created for</param>
		/// <param name="indexName">(Optional) Custom index name, if left empty index will be named as default "wildcard" name</param>
		/// <typeparam name="T">Constrained to a class that derives from <see cref="StorageDocument"/></typeparam>
		public static async Task CreateWildcardIndex<T>(this IMongoCollection<T> collection,
		                                                string fieldName,
		                                                string indexName = "wildcard")
			where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(IndexType.Wildcard, fieldName);
			await collection.CreateIndex(indexKeysDefinition, indexName);
		}
		
		private static IndexKeysDefinition<T> BuildDefinition<T>(IndexType type, string fieldName)
		{
			switch (type)
			{
				case IndexType.Ascending:
					return Builders<T>.IndexKeys.Ascending(fieldName);
				case IndexType.Descending:
					return Builders<T>.IndexKeys.Descending(fieldName);
				case IndexType.Text:
					return Builders<T>.IndexKeys.Text(fieldName);
				case IndexType.Geo2D:
					return Builders<T>.IndexKeys.Geo2D(fieldName);
				case IndexType.Geo2DSphere:
					return Builders<T>.IndexKeys.Geo2DSphere(fieldName);
				case IndexType.Hashed:
					return Builders<T>.IndexKeys.Hashed(fieldName);
				case IndexType.Wildcard:
					return Builders<T>.IndexKeys.Wildcard(fieldName);
				default:
					return null;
			}
		}

		private static async Task CreateIndex<T>(this IMongoCollection<T> collection,
		                                         IndexKeysDefinition<T> indexKeysDefinition,
		                                         string indexName) where T : StorageDocument
		{
			var indexManager = collection.Indexes;
			var model =
				new CreateIndexModel<T>(indexKeysDefinition, new CreateIndexOptions {Name = indexName});
			await indexManager.CreateOneAsync(model);
		}

		#endregion
		
		#region CRUD
		public static async Promise<TCollection> Get<TCollection>(this IMongoCollection<TCollection> collection,
		                                                          string id)
			where TCollection : StorageDocument
		{
			var filter = Builders<TCollection>.Filter.Eq("_id", new ObjectId(id));
			var search = await collection.FindAsync(filter);
			return search.FirstOrDefault();
		}

		public static async Promise<TCollection> Get<TStorage, TCollection>(
			this IStorageObjectConnectionProvider provider,
			string id)
			where TStorage : MongoStorageObject
			where TCollection : StorageDocument
		{
			var collection = await provider.GetCollection<TStorage, TCollection>();
			var filter = Builders<TCollection>.Filter.Eq("_id", new ObjectId(id));
			var search = await collection.FindAsync(filter);
			return search.FirstOrDefault();
		}
		
		public static async void Create<TStorage, TCollection>(this IStorageObjectConnectionProvider provider,
		                                                       TCollection data) where TStorage : MongoStorageObject
			where TCollection : StorageDocument
		{
			var collection = await provider.GetCollection<TStorage, TCollection>();
			await collection.Create(data);
		}
		
		public static async Task Create<TCollection>(this IMongoCollection<TCollection> collection,
		                                             TCollection data) where TCollection : StorageDocument
		{
			await collection.InsertOneAsync(data);
		}
		
		public static async Promise<bool> Update<TStorage, TCollection>(this IStorageObjectConnectionProvider provider,
		                                                                string id,
		                                                                TCollection updatedData)
			where TStorage : MongoStorageObject
			where TCollection : StorageDocument
		{
			var collection = await provider.GetCollection<TStorage, TCollection>();
			return await collection.Update(id, updatedData);
		}
		
		public static async Promise<bool> Update<TCollection>(this IMongoCollection<TCollection> collection,
		                                                      string id,
		                                                      TCollection updatedData)
			where TCollection : StorageDocument
		{
			var filter = Builders<TCollection>.Filter.Eq("_id", new ObjectId(id));
			var result = await collection.ReplaceOneAsync(filter, updatedData);
			return result.ModifiedCount > 0;
		}
		
		public static async Promise<bool> Delete<TStorage, TCollection>(this IStorageObjectConnectionProvider provider,
		                                                                string id)
			where TStorage : MongoStorageObject
			where TCollection : StorageDocument
		{
			var collection = await provider.GetCollection<TStorage, TCollection>();
			return await collection.Delete(id);
		}

		public static async Promise<bool> Delete<TCollection>(this IMongoCollection<TCollection> collection, string id)
			where TCollection : StorageDocument
		{
			var filter = Builders<TCollection>.Filter.Eq("_id", new ObjectId(id));
			var result = await collection.DeleteOneAsync(filter);
			return result.DeletedCount > 0;
		}
		
		#endregion
		
		public static async Promise<bool> UpdateMany<TCollection>(this IMongoCollection<TCollection> collection,
		                                                          string id,
		                                                          Dictionary<string, object> updatedData)
			where TCollection : StorageDocument
		{
			FilterDefinition<TCollection> filter = Builders<TCollection>.Filter.Eq("_id", new ObjectId(id));

			var update = Builders<TCollection>.Update;
			var updates = new List<UpdateDefinition<TCollection>>();

			foreach (var data in updatedData)
			{
				updates.Add(update.Set(data.Key, data.Value));
			}

			var result = await collection.UpdateOneAsync(filter, update.Combine(updates), new UpdateOptions
			{
				IsUpsert = true,
				
			});

			return result.ModifiedCount == updatedData.Count;
		}
	}
}
