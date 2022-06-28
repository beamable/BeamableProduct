using Beamable.Server;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using System;

namespace microserviceTests.MongoSerializationTests
{
	public class CustomStructTests
	{
		[SetUp]
		public void Setup()
		{
			var srvc = new MongoSerializationService();
			srvc.Init();

			srvc.RegisterStruct<CustomXYZ>();
		}

		[Serializable]
		public struct CustomXYZ
		{
			public int x, y;
		}

		[Test]
		public void CustomStructTest01()
		{
			var data = new CustomXYZ { x = 1, y = 2 };
			var bson = data.ToBson();
			var output = BsonSerializer.Deserialize<CustomXYZ>(bson);
			Assert.AreEqual(data.x, output.x);
			Assert.AreEqual(data.y, output.y);
		}
	}
}
