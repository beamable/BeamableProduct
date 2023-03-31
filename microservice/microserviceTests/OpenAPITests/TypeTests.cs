using Beamable.Common.Reflection;
using Beamable.Tooling.Common.OpenAPI;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace microserviceTests.OpenAPITests;

public class TypeTests
{
	private SchemaGenerator _generator;
	
	[SetUp]
	public void Setup()
	{
		_generator = new SchemaGenerator();
	}
	
	[Test]
	public void CheckRelatedTypes()
	{
		var g = new SchemaGenerator();
		var types = g.Traverse<int>().ToList();
		
		Assert.AreEqual(1, types.Count);
		Assert.AreEqual(typeof(int), types[0]);
	}
	
	// [Test]
	// public void CheckRelatedTypes_Vector()
	// {
	// 	var g = new SchemaGenerator();
	// 	var types = g.Traverse<Vector2>().ToList();
	// 	
	// 	Assert.AreEqual(2, types.Count);
	// 	Assert.AreEqual(typeof(Vector2), types[0]);
	// 	Assert.AreEqual(typeof(float), types[1]);
	// }

	[TestCase(typeof(float), "number", "float")]
	[TestCase(typeof(double), "number", "double")]
	[TestCase(typeof(short), "integer", "int16")]
	[TestCase(typeof(int), "integer", "int32")]
	[TestCase(typeof(long), "integer", "int64")]
	[TestCase(typeof(bool), "boolean", null)]
	[TestCase(typeof(string), "string", null)]
	[TestCase(typeof(byte), "string", "byte")]
	[TestCase(typeof(Guid), "string", "uuid")]
	public void CheckPrimitives(Type runtimeType, string typeName, string format)
	{
		var schema = _generator.Convert(runtimeType);
		Assert.AreEqual(typeName, schema.Type);
		Assert.AreEqual(format, schema.Format);
	}

	[TestCase(typeof(float[]), "number", "float")]
	[TestCase(typeof(List<float>), "number", "float")]
	public void CheckPrimitiveArrays(Type runtimeType, string typeName, string format)
	{
		var schema = _generator.Convert(runtimeType);
		Assert.AreEqual(typeName, schema.Items.Type);
		Assert.AreEqual(format, schema.Items.Format);
	}
	
	[TestCase(typeof(Dictionary<string, int>), "integer", "int32")]
	public void CheckMapTypes(Type runtimeType, string typeName, string format)
	{
		var schema = _generator.Convert(runtimeType);
		Assert.AreEqual(true, schema.AdditionalPropertiesAllowed);
		Assert.AreEqual(typeName, schema.AdditionalProperties.Type);
		Assert.AreEqual(format, schema.AdditionalProperties.Format);
	}
	
	[TestCase(typeof(Sample[]))]
	[TestCase(typeof(List<Sample>))]
	public void CheckListOfObjects(Type runtimeType)
	{
		var schema = _generator.Convert(runtimeType);
		Assert.AreEqual(nameof(Sample), schema.Items.Reference.Id);
	}


	[Test]
	public void CheckObject()
	{
		var schema = _generator.Convert(typeof(Vector2));
		
		Assert.AreEqual(2, schema.Properties.Count);
		
		Assert.AreEqual("number", schema.Properties["x"].Type);
		Assert.AreEqual("float", schema.Properties["x"].Format);
		
		Assert.AreEqual("number", schema.Properties["y"].Type);
		Assert.AreEqual("float", schema.Properties["y"].Format);
	}
	
	[Test]
	public void CheckObjectWithReference()
	{
		var schema = _generator.Convert(typeof(Sample));
		
		Assert.AreEqual(1, schema.Properties.Count);
		
		Assert.AreEqual(nameof(Tuna), schema.Properties[nameof(Sample.fish)].Reference.Id);
	}

	public class Sample
	{
		public Tuna fish;
	}

	public class Tuna
	{
		public int smelly;
	}
}
