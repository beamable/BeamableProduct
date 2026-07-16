using Beamable.Common.Semantics;
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

	[Test]
	public void SelfReferentialType_DefaultChainDepthIsBoundedExactly()
	{
		// Walks the recursively-defaulted 'next' chain to lock in exactly where MaxDefaultDepth (10,
		// private to ObjectMapper in SmallerJSON.cs) cuts off to null, rather than only checking that
		// the top-level result isn't null. If MaxDefaultDepth changes, update the expected count below.
		var result = Json.Deserialize<SelfReferential>("{\"value\":\"root\"}");

		var depth = 0;
		var current = result;
		while (current.next != null && depth < 20)
		{
			current = current.next;
			depth++;
		}

		Assert.That(depth, Is.EqualTo(10));
	}

	public class MutualA
	{
		public string label;
		public MutualB b;
	}

	public class MutualB
	{
		public string label;
		public MutualA a;
	}

	[Test]
	public void MutualRecursion_DoesNotStackOverflow()
	{
		// A <-> B cycle (not just a single self-referential type) must also be bounded by
		// MaxDefaultDepth rather than recursing forever.
		var result = Json.Deserialize<MutualA>("{\"label\":\"root\"}");
		Assert.That(result, Is.Not.Null);
		Assert.That(result.label, Is.EqualTo("root"));
		Assert.That(result.b, Is.Not.Null);
		Assert.That(result.b.label, Is.EqualTo(""));
	}

	[Test]
	public void ArrayElementExplicitNull_DefaultsIndependently()
	{
		// Each array element is defaulted on its own; an explicit null element becomes "", not left null
		// and not collapsing the rest of the array.
		var result = Json.Deserialize<Model>("{\"tags\":[\"a\",null]}");
		Assert.That(result.tags.Count, Is.EqualTo(2));
		Assert.That(result.tags[0], Is.EqualTo("a"));
		Assert.That(result.tags[1], Is.EqualTo(""));
	}

	public class NestedLookupModel
	{
		public Dictionary<string, Nested> lookup;
	}

	[Test]
	public void DictionaryValueExplicitNull_DefaultsToInstanceNotNull()
	{
		var result = Json.Deserialize<NestedLookupModel>("{\"lookup\":{\"a\":null}}");
		Assert.That(result.lookup["a"], Is.Not.Null);
		Assert.That(result.lookup["a"].label, Is.EqualTo(""));
	}

	public class SemanticModel
	{
		public ServiceName serviceName;
	}

	[Test]
	public void MissingSemanticTypeField_ConstructsFromEmptyString()
	{
		// ServiceName (Beamable.Common.Semantics) only has ctor(string), not a parameterless ctor, so
		// before ordering the semantic-type check ahead of the null branch this silently defaulted to
		// default(ServiceName) - a struct whose Value field is null, not "". That reintroduces the exact
		// NullReferenceException risk this whole fix is meant to prevent.
		var result = Json.Deserialize<SemanticModel>("{}");
		Assert.That(result.serviceName.Value, Is.EqualTo(""));
	}

	[Test]
	public void ExplicitNullSemanticTypeField_ConstructsFromEmptyString()
	{
		var result = Json.Deserialize<SemanticModel>("{\"serviceName\":null}");
		Assert.That(result.serviceName.Value, Is.EqualTo(""));
	}

	public class InterfaceCollectionModel
	{
		public IList<string> items;
		public IDictionary<string, int> map;
	}

	[Test]
	public void MissingInterfaceTypedCollectionFields_DefaultToConcreteEmptyCollections()
	{
		// IList<T>/IDictionary<K,V> fields previously fell through every branch and stayed null since
		// CreateJsonUtilityDefault required IsClass (interfaces aren't classes). They now materialize a
		// concrete List<>/Dictionary<> instead.
		var result = Json.Deserialize<InterfaceCollectionModel>("{}");
		Assert.That(result.items, Is.Not.Null.And.Empty);
		Assert.That(result.items, Is.InstanceOf<List<string>>());
		Assert.That(result.map, Is.Not.Null.And.Empty);
		Assert.That(result.map, Is.InstanceOf<Dictionary<string, int>>());
	}

	public struct StructWithReferenceFields
	{
		public string label;
		public List<string> items;
	}

	public class StructModel
	{
		public StructWithReferenceFields s;
	}

	[Test]
	public void MissingStructField_RecursivelyDefaultsInnerReferenceFields()
	{
		// Struct-typed fields previously only got Activator.CreateInstance(type) (i.e. default(T)),
		// unlike the class branch which recurses into fields - so a struct's own string/list fields
		// stayed null instead of "" / empty. They now get the same field-level defaulting as classes.
		var result = Json.Deserialize<StructModel>("{}");
		Assert.That(result.s.label, Is.EqualTo(""));
		Assert.That(result.s.items, Is.Not.Null.And.Empty);
	}

	public class ObjectFieldModel
	{
		public object anything;
	}

	[Test]
	public void MissingObjectTypedField_StaysNull()
	{
		// A bare `object`-typed field has no safe non-null default; it must stay null rather than
		// become a useless `new object()` that breaks `== null` checks downstream.
		var result = Json.Deserialize<ObjectFieldModel>("{}");
		Assert.That(result.anything, Is.Null);
	}
}
