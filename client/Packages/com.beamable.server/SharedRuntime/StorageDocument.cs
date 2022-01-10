
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using System;
using UnityEngine;

namespace Beamable.Server
{
	/// <summary>
	/// This should be the base class of the types you wish to store in MongoDB.
	/// It will automatically assign IDs to your documents with the <see cref="Id"/> property.
	/// </summary>
	[Serializable]
	public class StorageDocument
	{
		[SerializeField]
		[BsonRepresentation(BsonType.ObjectId)]
		[BsonId]
		private string _id = null; // MongoDb driver will auto-set this.

		/// <summary>
		/// The unique Mongo ID of the instance. This is automatically created when the object is first sent to the database.
		/// </summary>
		[BsonIgnore]
		public string Id => _id;
	}

}
