using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Beamable.Editor.Tests
{
	public class JsonTestHelp
	{
		[Test]
		public void JsonReadPoly()
		{
			var original = new Wrapper()
			{
				data = new BaseType[] { new SubA() { type = "a", x = 1 }, new SubB() { type = "b", y = 2 }, }
			};
			var json = JsonSerializable.ToJson(original);

			var factory = new JsonSerializable.TypeLookupFactory<BaseType>()
				.Add<SubA>("a")
				.Add<SubB>("b");
			var instance = JsonSerializable.FromJson<Wrapper>(json, new List<JsonSerializable.ISerializableFactory> { factory });

			Assert.AreEqual(instance.data.Length, original.data.Length);
			Assert.AreEqual(instance.data[0].type, original.data[0].type);
			Assert.AreEqual(instance.data[1].type, original.data[1].type);
			Assert.AreEqual(((SubA)instance.data[0]).x, ((SubA)original.data[0]).x);
			Assert.AreEqual(((SubB)instance.data[1]).y, ((SubB)original.data[1]).y);
		}


		public class DedicatedFactory : JsonSerializable.TypeLookupFactory<BaseType>
		{
			public DedicatedFactory() : base()
			{
				this.Add<SubA>("a");
			}
		}

		public class Wrapper : JsonSerializable.ISerializable
		{
			public BaseType[] data;
			public void Serialize(JsonSerializable.IStreamSerializer s)
			{
				s.SerializeArray("data", ref data);
			}
		}

		public class BaseType : JsonSerializable.ISerializable
		{
			public string type;
			public virtual void Serialize(JsonSerializable.IStreamSerializer s)
			{
				s.Serialize("type", ref type);
			}
		}

		public class SubA : BaseType
		{
			public int x;
			public override void Serialize(JsonSerializable.IStreamSerializer s)
			{
				base.Serialize(s);
				s.Serialize("x", ref x);
			}
		}

		public class SubB : BaseType
		{
			public int y;
			public override void Serialize(JsonSerializable.IStreamSerializer s)
			{
				base.Serialize(s);
				s.Serialize("y", ref y);
			}
		}
	}


}
