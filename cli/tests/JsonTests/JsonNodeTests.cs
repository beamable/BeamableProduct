using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using Moq;
using NUnit.Framework;
using System;

namespace tests.JsonTests;

public class JsonNodeTests
{
	public class Tuna : JsonSerializable.ISerializable
	{
		public int x;
		public JsonString str;
		public int y;
		
		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.Serialize("x", ref x);
			s.SerializeNestedJson("p", ref str);
			s.Serialize("y", ref y);
		}
	}

	public class MockSettings : JsonSerializable.ISerializable
	{
		public string contentId;
		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.Serialize(nameof(contentId), ref contentId);
		}
	}

	[Test]
	public void Simple()
	{
		const string fakeJson = "[1,2,3]";
		var t = new Tuna { x = 3, y= 1, str = JsonString.FromJson(fakeJson)};
		var json = JsonSerializable.ToJson(t);
		
		Assert.That(json, Is.EqualTo($@"{{""x"":3,""p"":{fakeJson},""y"":1}}"));
		var clone = JsonSerializable.FromJson<Tuna>(json);
		Assert.That(clone.str.Json, Is.EqualTo(fakeJson));
		
		Assert.That(clone.str.ValueObject, Is.Not.Null);
	}
	
	
	[Test]
	public void ComplexType()
	{
		const string contentId = "foozle";
		
		// convert a strong type into a json string
		var jsonString = JsonString.FromSerializable(new MockSettings { contentId = contentId });
		
		var t = new Tuna { x = 3, y= 1, str = jsonString};
		
		var json = JsonSerializable.ToJson(t);
		Assert.That(json, Is.EqualTo($@"{{""x"":3,""p"":{{""contentId"":""foozle""}},""y"":1}}"));
		var clone = JsonSerializable.FromJson<Tuna>(json);

		var outputSettings = clone.str.Deserialize<MockSettings>();
		Assert.That(outputSettings.contentId, Is.EqualTo(contentId));
	}
}
