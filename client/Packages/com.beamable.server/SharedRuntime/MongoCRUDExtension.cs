// this file was copied from nuget package Beamable.Server.Common@0.0.0-PREVIEW.NIGHTLY-202407161549
// https://www.nuget.org/packages/Beamable.Server.Common/0.0.0-PREVIEW.NIGHTLY-202407161549

﻿using Beamable.Server;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beamable.Mongo
{
	public static class MongoCRUDExtensions
	{
		#region CRUD

		public static async Task<TCollection> Get<TCollection>(this IMongoCollection<TCollection> collection,
			string id)
			where TCollection : StorageDocument
		{
			var filter = Builders<TCollection>.Filter.Eq("_id", new ObjectId(id));
			var search = await collection.FindAsync(filter);
			return search.FirstOrDefault();
		}

		public static async Task<TCollection> Get<TStorage, TCollection>(
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

		public static async Task Create<TStorage, TCollection>(this IStorageObjectConnectionProvider provider,
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

		public static async Task<bool> Update<TStorage, TCollection>(this IStorageObjectConnectionProvider provider,
			string id,
			TCollection updatedData)
			where TStorage : MongoStorageObject
			where TCollection : StorageDocument
		{
			var collection = await provider.GetCollection<TStorage, TCollection>();
			return await collection.Update(id, updatedData);
		}

		public static async Task<bool> Update<TCollection>(this IMongoCollection<TCollection> collection,
			string id,
			TCollection updatedData)
			where TCollection : StorageDocument
		{
			var filter = Builders<TCollection>.Filter.Eq("_id", new ObjectId(id));
			var result = await collection.UpdateOneAsync(filter, new ObjectUpdateDefinition<TCollection>(updatedData));
			return result.ModifiedCount > 0;
		}

		public static async Task<bool> Delete<TStorage, TCollection>(this IStorageObjectConnectionProvider provider,
			string id)
			where TStorage : MongoStorageObject
			where TCollection : StorageDocument
		{
			var collection = await provider.GetCollection<TStorage, TCollection>();
			return await collection.Delete(id);
		}

		public static async Task<bool> Delete<TCollection>(this IMongoCollection<TCollection> collection, string id)
			where TCollection : StorageDocument
		{
			var filter = Builders<TCollection>.Filter.Eq("_id", new ObjectId(id));
			var result = await collection.DeleteOneAsync(filter);
			return result.DeletedCount > 0;
		}

		public static async Task<bool> UpdateMany<TCollection>(this IMongoCollection<TCollection> collection,
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

			var result = await collection.UpdateOneAsync(filter, update.Combine(updates),
				new UpdateOptions { IsUpsert = true, });

			return result.ModifiedCount == updatedData.Count;
		}

		#endregion
	}
}
