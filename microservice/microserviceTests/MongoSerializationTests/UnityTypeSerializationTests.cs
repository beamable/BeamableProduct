using Beamable.Server;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace microserviceTests.MongoSerializationTests
{
	public class UnityTypeSerializationTests
	{
		private MongoSerializationService _srvc;
		[SetUp]
		public void Setup()
		{
			_srvc = new MongoSerializationService();
			_srvc.Init();
		}

		[Test]
		public void Vector2()
		{
			var v = new Vector2(1, 2);
			var bson = v.ToBson();
			var output = BsonSerializer.Deserialize<Vector2>(bson);
			Assert.AreEqual(v.x, output.x);
			Assert.AreEqual(v.y, output.y);
		}

		[Test]
		public void ListOfVector2()
		{
			var v = new ListOfVector2Support { Vectors = new List<Vector2> { new Vector2(1, 2), new Vector2(3, 4) } };
			var bson = v.ToBson();
			var output = BsonSerializer.Deserialize<ListOfVector2Support>(bson);
			Assert.AreEqual(v.Vectors.Count, output.Vectors.Count);
			Assert.AreEqual(v.Vectors[0].x, output.Vectors[0].x);
			Assert.AreEqual(v.Vectors[0].y, output.Vectors[0].y);
			Assert.AreEqual(v.Vectors[1].x, output.Vectors[1].x);
			Assert.AreEqual(v.Vectors[1].y, output.Vectors[1].y);
		}

		[Test]
		public void ArrayOfVector2()
		{
			var v = new ArrayOfVector2Support { Vectors = new Vector2[] { new Vector2(1, 2), new Vector2(3, 4) } };
			var bson = v.ToBson();
			var output = BsonSerializer.Deserialize<ArrayOfVector2Support>(bson);
			Assert.AreEqual(v.Vectors.Length, output.Vectors.Length);
			Assert.AreEqual(v.Vectors[0].x, output.Vectors[0].x);
			Assert.AreEqual(v.Vectors[0].y, output.Vectors[0].y);
			Assert.AreEqual(v.Vectors[1].x, output.Vectors[1].x);
			Assert.AreEqual(v.Vectors[1].y, output.Vectors[1].y);
		}


		[Test]
		public void ListListOfVector2()
		{
			var v = new ListOfListOfVector2Support
			{
				VectorList = new List<ListOfVector2Support>
				{
					new ListOfVector2Support {Vectors = new List<Vector2> {new Vector2(1, 2), new Vector2(3, 4)}},
					new ListOfVector2Support {Vectors = new List<Vector2> {new Vector2(5, 7), new Vector2(8, 9)}}
				}
			};
			var bson = v.ToBson();
			var output = BsonSerializer.Deserialize<ListOfListOfVector2Support>(bson);
			Assert.AreEqual(v.VectorList.Count, output.VectorList.Count);
			for (var i = 0; i < v.VectorList.Count; i++)
			{
				Assert.AreEqual(v.VectorList[i].Vectors.Count, output.VectorList[i].Vectors.Count);
			}
		}

		public class ListOfVector2Support
		{
			public List<Vector2> Vectors = new List<Vector2>();
		}

		public class ArrayOfVector2Support
		{
			public Vector2[] Vectors;
		}


		public class ListOfListOfVector2Support
		{
			public List<ListOfVector2Support> VectorList = new List<ListOfVector2Support>();
		}
	}
}
