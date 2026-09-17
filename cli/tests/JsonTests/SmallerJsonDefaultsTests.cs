using Beamable.Serialization.SmallerJSON;
using NUnit.Framework;
using System.Collections.Generic;

namespace tests.JsonTests;

// Regression coverage for the JsonUtility -> SmallerJSON deserializer swap (commit 74dcc190e3).
// Unity's JsonUtility.FromJson<T> never leaves reference fields null: missing strings become "",
// missing arrays/lists become empty, and missing [Serializable] classes become default-constructed
// instances. Json.Deserialize<T> (ObjectMapper.ConvertToType) must preserve those same non-null
// guarantees so existing SDK/game code does not start throwing NullReferenceException.
public class SmallerJsonDefaultsTests
{
	public class Nested
	{
		public string label;
		public int count;
	}

	public class Model
	{
		public string name;
		public int[] ids;
		public List<string> tags;
		public Dictionary<string, string> lookup;
		public Nested child;
		public int number;
	}

	[Test]
	public void MissingReferenceFields_DefaultToNonNull()
	{
		// Only the value-type field is present; every reference field is absent from the JSON.
		var result = Json.Deserialize<Model>("{\"number\":5}");

		Assert.That(result, Is.Not.Null);
		Assert.That(result.number, Is.EqualTo(5));
		Assert.That(result.name, Is.EqualTo(""));                 // string => ""
		Assert.That(result.ids, Is.Not.Null.And.Empty);           // array => empty
		Assert.That(result.tags, Is.Not.Null.And.Empty);          // List<T> => empty
		Assert.That(result.lookup, Is.Not.Null.And.Empty);        // Dictionary<,> => empty
		Assert.That(result.child, Is.Not.Null);                   // nested class => instance
		Assert.That(result.child.label, Is.EqualTo(""));          // ...with its own fields defaulted
		Assert.That(result.child.count, Is.EqualTo(0));
	}

	[Test]
	public void EmptyObject_DefaultsEveryReferenceField()
	{
		var result = Json.Deserialize<Model>("{}");

		Assert.That(result.name, Is.EqualTo(""));
		Assert.That(result.ids, Is.Not.Null.And.Empty);
		Assert.That(result.tags, Is.Not.Null.And.Empty);
		Assert.That(result.child, Is.Not.Null);
		Assert.That(result.number, Is.EqualTo(0));
	}

	[Test]
	public void ExplicitNullString_DefaultsToEmptyString()
	{
		var result = Json.Deserialize<Model>("{\"name\":null}");
		Assert.That(result.name, Is.EqualTo(""));
	}

	[Test]
	public void ExplicitNullCollections_DefaultToEmpty()
	{
		var result = Json.Deserialize<Model>("{\"ids\":null,\"tags\":null,\"lookup\":null}");
		Assert.That(result.ids, Is.Not.Null.And.Empty);
		Assert.That(result.tags, Is.Not.Null.And.Empty);
		Assert.That(result.lookup, Is.Not.Null.And.Empty);
	}

	[Test]
	public void ExplicitNullNestedObject_DefaultsToInstance()
	{
		var result = Json.Deserialize<Model>("{\"child\":null}");
		Assert.That(result.child, Is.Not.Null);
		Assert.That(result.child.label, Is.EqualTo(""));
	}

	[Test]
	public void PresentValues_ArePreserved()
	{
		var result = Json.Deserialize<Model>(
			"{\"name\":\"hi\",\"ids\":[1,2],\"tags\":[\"a\"],\"child\":{\"label\":\"L\",\"count\":3},\"number\":9}");

		Assert.That(result.name, Is.EqualTo("hi"));
		Assert.That(result.ids, Is.EqualTo(new[] { 1, 2 }));
		Assert.That(result.tags, Is.EqualTo(new List<string> { "a" }));
		Assert.That(result.child.label, Is.EqualTo("L"));
		Assert.That(result.child.count, Is.EqualTo(3));
		Assert.That(result.number, Is.EqualTo(9));
	}

	[Test]
	public void EmptyStringValue_IsPreservedAndNotNull()
	{
		var result = Json.Deserialize<Model>("{\"name\":\"\"}");
		Assert.That(result.name, Is.EqualTo(""));
	}

	public class SelfReferential
	{
		public string value;
		public SelfReferential next;
	}

	[Test]
	public void SelfReferentialType_DoesNotStackOverflow()
	{
		// 'next' is absent, so it is defaulted via a recursive nested-instance chain that must be
		// bounded by MaxDefaultDepth rather than recursing forever.
		var result = Json.Deserialize<SelfReferential>("{\"value\":\"root\"}");
		Assert.That(result, Is.Not.Null);
		Assert.That(result.value, Is.EqualTo("root"));
	}
}
