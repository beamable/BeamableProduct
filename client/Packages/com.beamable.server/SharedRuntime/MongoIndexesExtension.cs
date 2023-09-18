using Beamable.Server;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beamable.Mongo
{
	public static class MongoIndexesExtension
	{
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
			var model =
				new CreateIndexModel<T>(indexKeysDefinition, new CreateIndexOptions
				{
					Name = indexName,
					
				});
			await collection.Indexes.CreateOneAsync(model);
		}
	}
}
