// this file was copied from nuget package Beamable.Server.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Server.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using UnityEngine;

namespace Beamable.Server
{
	[Serializable]
	public class StorageDocument
	{
		[SerializeField]
		[BsonIgnore]
		private string _id = null;

		/// <summary>
		/// The unique Mongo ID of the instance. This is automatically created when the object is first sent to the database.
		/// </summary>
		[BsonId]
		[BsonElement(nameof(_id))]
		[BsonRepresentation(BsonType.ObjectId)]
		[BsonIgnoreIfDefault]
		[BsonIgnoreIfNull]
		public string Id
		{
			get => _id;
			set => _id = value;
		}
	}
}
