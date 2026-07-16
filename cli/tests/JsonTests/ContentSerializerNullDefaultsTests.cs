using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Reflection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace tests.JsonTests;

// Regression coverage for ContentSerializer<TContentBase> (cli/beamable.common/Runtime/Content/ContentSerializer.cs),
// the actual deserializer behind Beamable.Common.Content.ContentObject and its subclasses (ItemContent,
// CurrencyContent, ListingContent, etc.) - a completely separate code path from the SmallerJSON.ObjectMapper
// fix in commit 80d3f959974a1292b9cfb3c193b4565f9a14dbd0 (see SmallerJsonDefaultsTests.cs).
//
// Two distinct scenarios, with two different intended outcomes:
//  - A field whose JSON key is entirely ABSENT (schema drift: a field added to the class after content was
//    published, or omitted because it happens to equal its zero value) now gets the same non-null default
//    Unity's JsonUtility/ObjectMapper.CreateJsonUtilityDefault would produce, so old content never leaves a
//    reference-type field null.
//  - An explicit JSON `null` on a PRESENT key is intentionally preserved as C# `null` - this is pre-existing,
//    tested behavior (see client/Packages/com.beamable/Tests/.../ClientContentSerializer/DeserializeTests.cs:
//    Primitives_WithNullString, ListStringWithNull) that must not regress.
public class ContentSerializerNullDefaultsTests
{
	[SetUp]
	public void Setup()
	{
		var reflectionCache = new ReflectionCache();
		var cache = new ContentTypeReflectionCache();
		reflectionCache.RegisterTypeProvider(cache);
		reflectionCache.RegisterReflectionSystem(cache);

		var assembliesToSweep = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetName().Name).ToList();
		reflectionCache.GenerateReflectionCache(assembliesToSweep);
	}

	// Mirrors the TestSerializer/TestContentObject support classes used by the Unity content test suite
	// (client/Packages/com.beamable/Tests/Runtime/Beamable/Content/Serialization/TestContentRef.cs), which
	// implement IContentObject directly rather than via ScriptableObject so they run in a plain NUnit host.
	private class TestSerializer : ContentSerializer<TestContentObject>
	{
		protected override TContent CreateInstance<TContent>() => new TContent();
		protected override TestContentObject CreateInstanceWithType(Type type) => new TestContentObject();
	}

	private class TestContentObject : IContentObject
	{
		public string Id { get; set; }
		public string Version { get; set; }
		public string[] Tags { get; set; }
		public string ManifestID { get; }
		public long LastChanged { get; set; }
		public ContentCorruptedException ContentException { get; set; }

		public void SetIdAndVersion(string id, string version)
		{
			Id = id;
			Version = version;
		}

		public string ToJson() => throw new NotImplementedException();
	}

	[System.Serializable]
	private class Inner
	{
		public string tag;
	}

	[System.Serializable]
	private class Nested
	{
		public string label;
		public int count;
		public Inner inner;
	}

	private class TestContentRef : AbsContentRef<TestContentObject>
	{
		public override Promise<TestContentObject> Resolve(string manifestID = "") => throw new NotImplementedException();
	}

	private class Model : TestContentObject
	{
		public string name;
		public string[] ids;
		public List<string> tags;
		public SerializableDictionaryStringToString lookup;
		public Nested child;
		public Optional<int> maybeCount;
		public TestContentRef reference;
		public int number;
	}

	[Test]
	public void MissingTopLevelFields_DefaultToNonNull()
	{
		// Only `number`'s key is present; every reference-type field's key is entirely absent.
		var json = @"{
			""id"": ""test.missing-fields"",
			""version"": ""1"",
			""properties"": {
				""number"": { ""data"": 5 }
			}
		}";

		var result = new TestSerializer().Deserialize<Model>(json);

		Assert.That(result.number, Is.EqualTo(5));
		Assert.That(result.name, Is.EqualTo(""));                 // string => ""
		Assert.That(result.ids, Is.Not.Null.And.Empty);           // array => empty
		Assert.That(result.tags, Is.Not.Null.And.Empty);          // List<T> => empty
		Assert.That(result.lookup, Is.Not.Null.And.Empty);        // SerializableDictionary => empty
		Assert.That(result.child, Is.Not.Null);                   // nested [Serializable] class => instance
		Assert.That(result.child.label, Is.EqualTo(""));          // ...with its own fields defaulted
		Assert.That(result.child.count, Is.EqualTo(0));
		Assert.That(result.child.inner, Is.Not.Null);             // ...recursively, even two levels deep
		Assert.That(result.child.inner.tag, Is.EqualTo(""));
		Assert.That(result.maybeCount.HasValue, Is.False);        // Optional<T>: pre-existing, must not change
		Assert.That(result.reference, Is.Not.Null);               // IContentRef => instance...
		Assert.That(result.reference.GetId(), Is.EqualTo(""));    // ...with an empty id, like a present ref would get
	}

	[Test]
	public void MissingNestedFieldInsideNestedObject_DefaultsToNonNull()
	{
		// `child` is present, but its own `count`/`inner` keys are absent - exercises the nested-object
		// field loop inside ContentSerializer.DeserializeResult, not the top-level BaseConvertType loop.
		var json = @"{
			""id"": ""test.missing-nested"",
			""version"": ""1"",
			""properties"": {
				""child"": { ""data"": { ""label"": ""hi"" } }
			}
		}";

		var result = new TestSerializer().Deserialize<Model>(json);

		Assert.That(result.child.label, Is.EqualTo("hi"));
		Assert.That(result.child.count, Is.EqualTo(0));
		Assert.That(result.child.inner, Is.Not.Null);
		Assert.That(result.child.inner.tag, Is.EqualTo(""));
	}

	[Test]
	public void ExplicitNullTopLevelStringField_StaysNull()
	{
		// Regression lock matching DeserializeTests.Primitives_WithNullString: an explicit JSON `null` on
		// a *present* key must stay C# null, unlike an absent key. Do not "fix" this to return "".
		var json = @"{
			""id"": ""test.explicit-null"",
			""version"": ""1"",
			""properties"": {
				""name"": { ""data"": null }
			}
		}";

		var result = new TestSerializer().Deserialize<Model>(json);

		Assert.That(result.name, Is.Null);
	}

	[Test]
	public void ExplicitNullArrayElement_StaysNull()
	{
		// Regression lock matching DeserializeTests.ListStringWithNull: individual null array/list elements
		// are intentionally preserved as null, not defaulted to "".
		var json = @"{
			""id"": ""test.explicit-null-elem"",
			""version"": ""1"",
			""properties"": {
				""tags"": { ""data"": [""a"", null] }
			}
		}";

		var result = new TestSerializer().Deserialize<Model>(json);

		Assert.That(result.tags.Count, Is.EqualTo(2));
		Assert.That(result.tags[0], Is.EqualTo("a"));
		Assert.That(result.tags[1], Is.Null);
	}

	[Test]
	public void PresentValues_ArePreserved()
	{
		var json = @"{
			""id"": ""test.present"",
			""version"": ""1"",
			""properties"": {
				""name"": { ""data"": ""hi"" },
				""ids"": { ""data"": [1, 2] },
				""tags"": { ""data"": [""a""] },
				""child"": { ""data"": { ""label"": ""L"", ""count"": 3, ""inner"": { ""tag"": ""t"" } } },
				""number"": { ""data"": 9 }
			}
		}";

		var result = new TestSerializer().Deserialize<Model>(json);

		Assert.That(result.name, Is.EqualTo("hi"));
		Assert.That(result.ids, Is.EqualTo(new[] { "1", "2" }));
		Assert.That(result.tags, Is.EqualTo(new List<string> { "a" }));
		Assert.That(result.child.label, Is.EqualTo("L"));
		Assert.That(result.child.count, Is.EqualTo(3));
		Assert.That(result.child.inner.tag, Is.EqualTo("t"));
		Assert.That(result.number, Is.EqualTo(9));
	}

	[Test]
	public void PartialSchemaPayload_MixOfMissingAndPresentFields_IsFullyUsable()
	{
		// Simulates old content published before several fields existed on the class: only `number` and
		// `name` are present; everything else must still come back non-null and usable.
		var json = @"{
			""id"": ""test.partial-schema"",
			""version"": ""1"",
			""properties"": {
				""name"": { ""data"": ""legacy"" },
				""number"": { ""data"": 1 }
			}
		}";

		var result = new TestSerializer().Deserialize<Model>(json);

		Assert.That(result.name, Is.EqualTo("legacy"));
		Assert.That(result.number, Is.EqualTo(1));
		Assert.DoesNotThrow(() =>
		{
			_ = result.ids.Length;
			_ = result.tags.Count;
			_ = result.lookup.Count;
			_ = result.child.label.Length;
			_ = result.child.inner.tag.Length;
			_ = result.reference.GetId().Length;
		});
	}
}
