using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Common.Reflection;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
	
	
	[ContentType("ConsumableContent")]
	[System.Serializable]
	public class ConsumableContent : ItemContent
	{
		[ContentField("initialProperties", FormerlySerializedAs = new[] { "InitialProperties" })]
		public Dictionary<string,string> initialProperties;
	}
	public class TestContentSerializer : ContentSerializer<IContentObject>
	{
		static readonly string[] excludedAssemblyPrefixes = new[]
		{
			"System.",
			"nunit.",
			"JetBrains.",
			"Microsoft.",
			"Serilog."
		};

		protected override TContent CreateInstance<TContent>()
		{
			return new TContent();
		}

		public static TestContentSerializer PopulateCacheAndGetSerializer()
		{
			var reflectionCache = new ReflectionCache();
			var contentTypeReflectionCache = new ContentTypeReflectionCache();
	        
			reflectionCache.RegisterTypeProvider(contentTypeReflectionCache);
			reflectionCache.RegisterReflectionSystem(contentTypeReflectionCache);


			var relevantAssemblyNames = AppDomain.CurrentDomain
				.GetAssemblies()
				.Select(asm => asm.GetName().Name)
				.Where(name => name != null && excludedAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
				.ToList();

			reflectionCache.GenerateReflectionCache(relevantAssemblyNames);
			return new TestContentSerializer();
		}
	}
	
	[Test]
	public void ContentTest()
	{
		var deserialized = TestContentSerializer.PopulateCacheAndGetSerializer().Deserialize<ConsumableContent>("""
			{
			  "id": "items.ConsumableContent.new_one",
			  "version": "some_id",
			  "properties": {
			    "InitialProperties": {
			      "data": {
			        "asdds": "asdf",
			        "dasa": "ddd",
			        "asaaa": "ddaa"
			      }
			    },
			    "icon": {
			      "data": {
			        "referenceKey": "",
			        "subObjectName": ""
			      }
			    },
			    "clientPermission": {
			      "data": {
			        "write_self": false
			      }
			    }
			  }
			}
			""");
		
		Assert.That(deserialized.initialProperties.Count, Is.EqualTo(3));
	}
}
