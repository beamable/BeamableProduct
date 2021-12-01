using Beamable.Tests.Content.Serialization.Support;
using NUnit.Framework;

namespace Beamable.Tests.Content.Serialization.ClientContentSerializationTests
{
	public class DeserializeEmptyFields
	{
		[Test]
		public void DeserializingEmptyValueDoesntBreak()
		{
			var json = @"{
               ""id"": ""test.nothing"",
               ""version"": ""123"",
               ""properties"": {
                  ""number"": { ""data"": """" },
               },
            }";

			var s = new TestSerializer();
			var o = s.Deserialize<SimpleContent>(json);

			Assert.AreEqual(0, o.number);
		}

		[Test]
		public void DeserializeEmptyValueIntoSerializedObject_YieldsDefault()
		{
			var json = @"{
               ""id"": ""test.nothing"",
               ""version"": ""123"",
               ""properties"": {
                  ""code"": { ""data"": """" },
               },
            }";

			var s = new TestSerializer();
			var o = s.Deserialize<ContentWithNestedType>(json);

			Assert.AreEqual("fish", o.code.tuna);
			Assert.AreEqual(1, o.code.x);
		}

		[Test]
		public void DeserializeEmptyValueIntoSerializedObject_SetToNull_DoesntCreate()
		{
			var json = @"{
               ""id"": ""test.nothing"",
               ""version"": ""123"",
               ""properties"": {
                  ""code"": { ""data"": """" },
               },
            }";

			var s = new TestSerializer();
			var o = s.Deserialize<ContentWithNestedTypeNull>(json);

			Assert.IsNull(o.code);
		}

#pragma warning disable CS0649

		class SimpleContent : TestContentObject
		{
			public int number;
		}

		class ContentWithNestedType : TestContentObject
		{
			public PromoCode code = new PromoCode();
		}

		class ContentWithNestedTypeNull : TestContentObject
		{
			public PromoCode code;
		}


		[System.Serializable]
		class PromoCode
		{
			public int x = 1;
			public string tuna = "fish";
		}
	}
}
