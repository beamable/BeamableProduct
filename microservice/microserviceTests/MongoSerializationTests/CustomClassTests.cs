using System;
using System.Collections.Generic;
using Beamable.Server;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace microserviceTests.MongoSerializationTests
{
	public class CustomClassTests
	{
		[SetUp]
		public void Setup()
		{
			var srvc = new MongoSerializationService();
			srvc.Init();

			srvc.RegisterStruct<CustomStructTests.CustomXYZ>();
		}

		[Test]
		public void NestedStuff()
		{
			var data = new NestedStuffSupport
			{
				X = 1,
				Vec = new Vector2(2, 3),
				Nested = new NestedStuffSupport
				{
					X = 2,
					Vec = new Vector2(4, 5)
				}
			};

			var bson = data.ToBson();
			var output = BsonSerializer.Deserialize<NestedStuffSupport>(bson);

			Assert.AreEqual(data.X,output.X);
			Assert.AreEqual(data.Vec,output.Vec);
			Assert.AreEqual(data.Nested.X,output.Nested.X);
			Assert.AreEqual(data.Nested.Vec,output.Nested.Vec);
		}

		public class NestedStuffSupport
		{
			public int X;
			public Vector2 Vec;
			public NestedStuffSupport Nested;
		}


		[Test]
		public void DataDoo()
		{
			var data = new DataDooSupport
			{
				X = 1,
				Vec = new Vector2(2, 3),
				Color = new Color(1, 2, 3, 4),
				Vectors = new List<Vector2>
				{
					new Vector2(3, 4)
				}
			};

			var bson = data.ToBson();
			var output = BsonSerializer.Deserialize<DataDooSupport>(bson);

			Assert.AreEqual(data.X,output.X);
			Assert.AreEqual(data.Vec,output.Vec);
			Assert.AreEqual(data.Color,output.Color);
			Assert.AreEqual(data.Vectors.Count,output.Vectors.Count);
		}
		[Serializable]
		public class DataDooSupport : StorageDocument
		{
			public int X;

			public Vector2 Vec;

			public Color Color;

			public List<Vector2> Vectors;
		}
	}
}