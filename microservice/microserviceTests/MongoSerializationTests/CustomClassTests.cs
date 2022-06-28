using Beamable.Server;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

			Assert.AreEqual(data.X, output.X);
			Assert.AreEqual(data.Vec, output.Vec);
			Assert.AreEqual(data.Nested.X, output.Nested.X);
			Assert.AreEqual(data.Nested.Vec, output.Nested.Vec);
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

			Assert.AreEqual(data.X, output.X);
			Assert.AreEqual(data.Vec, output.Vec);
			Assert.AreEqual(data.Color, output.Color);
			Assert.AreEqual(data.Vectors.Count, output.Vectors.Count);
		}
		[Serializable]
		public class DataDooSupport : StorageDocument
		{
			public int X;

			public Vector2 Vec;

			public Color Color;

			public List<Vector2> Vectors;
		}

		[MongoSerializable]
		public class UnityClassSupport : StorageDocument
		{
			[FormerlySerializedAsAttribute("text")]
			public string message;

			public string property { get; set; }

			public int X;

			[FormerlySerializedAs("info")]
			[SerializeField]
			private string field1;

			private string field2;

			public void SetFields(string str1, string str2)
			{
				field1 = str1;
				field2 = str2;
			}

			public string GetField1()
			{
				return this.field1;
			}

			public string GetField2()
			{
				return this.field2;
			}
		}

		[Test]
		public void DataUnity()
		{
			var data = new UnityClassSupport
			{
				message = "Msg",
				property = "Test",
				X = 1,
			};

			data.SetFields("fieldRaw1", "fieldRaw2");

			var bson = data.ToBson();
			var output = BsonSerializer.Deserialize<UnityClassSupport>(bson);

			Assert.AreEqual(data.X, output.X);
			Assert.AreNotEqual(data.property, output.property);
			Assert.AreEqual(data.message, output.message);
			Assert.AreEqual(data.GetField1(), output.GetField1());
			Assert.AreNotEqual(data.GetField2(), output.GetField2());
		}
	}
}
