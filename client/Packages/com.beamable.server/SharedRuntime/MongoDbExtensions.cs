using Beamable.Server;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beamable.Microservices
{
	public static class MongoDbExtensions
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

		public static async Task CreateSingleIndex<T>(this IMongoCollection<T> collection,
		                                        IndexType type,
		                                        string fieldName,
		                                        string indexName = "") where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(type, fieldName);
			await collection.CreateIndex(indexKeysDefinition,
			                             string.IsNullOrEmpty(indexName) ? type.ToString().ToLower() : indexName);
		}
		
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

		public static async Task CreateAscendingIndex<T>(this IMongoCollection<T> collection,
		                                                 string fieldName,
		                                                 string indexName = "ascending")
			where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(IndexType.Ascending, fieldName);
			await collection.CreateIndex(indexKeysDefinition, indexName);
		}

		public static async Task CreateDescendingIndex<T>(this IMongoCollection<T> collection,
		                                                  string fieldName,
		                                                  string indexName = "descending")
			where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(IndexType.Descending, fieldName);
			await collection.CreateIndex(indexKeysDefinition, indexName);
		}

		public static async Task CreateTextIndex<T>(this IMongoCollection<T> collection,
		                                            string fieldName,
		                                            string indexName = "text")
			where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(IndexType.Text, fieldName);
			await collection.CreateIndex(indexKeysDefinition, indexName);
		}

		public static async Task CreateGeo2DIndex<T>(this IMongoCollection<T> collection,
		                                             string fieldName,
		                                             string indexName = "geo2D")
			where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(IndexType.Geo2D, fieldName);
			await collection.CreateIndex(indexKeysDefinition, indexName);
		}

		public static async Task CreateGeo2DSphereIndex<T>(this IMongoCollection<T> collection,
		                                                   string fieldName,
		                                                   string indexName = "geo2Dsphere")
			where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(IndexType.Geo2DSphere, fieldName);
			await collection.CreateIndex(indexKeysDefinition, indexName);
		}

		public static async Task CreateHashedIndex<T>(this IMongoCollection<T> collection,
		                                              string fieldName,
		                                              string indexName = "hashed")
			where T : StorageDocument
		{
			var indexKeysDefinition = BuildDefinition<T>(IndexType.Hashed, fieldName);
			await collection.CreateIndex(indexKeysDefinition, indexName);
		}

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
	}
}
