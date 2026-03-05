using Beamable.Common.Reflection;
using Beamable.Tooling.Common.OpenAPI;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Semantics;
using UnityEngine;

namespace microserviceTests.OpenAPITests;

public class TypeTests
{
	[Test]
	public void CheckRelatedTypes()
	{
		var types = SchemaGenerator.Traverse<int>().ToList();
		
		Assert.AreEqual(1, types.Count);
		Assert.AreEqual(typeof(int), types[0]);
	}

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
		var schema = SchemaGenerator.Convert(runtimeType);
		Assert.AreEqual(typeName, schema.Type);
		Assert.AreEqual(format, schema.Format);
	}

	[TestCase(typeof(float[]), "number", "float")]
	[TestCase(typeof(List<float>), "number", "float")]
	public void CheckPrimitiveArrays(Type runtimeType, string typeName, string format)
	{
		var schema = SchemaGenerator.Convert(runtimeType);
		Assert.AreEqual(typeName, schema.Items.Type);
		Assert.AreEqual(format, schema.Items.Format);
	}
	
	[TestCase(typeof(Dictionary<string, int>), "integer", "int32")]
	public void CheckMapTypes(Type runtimeType, string typeName, string format)
	{
		var schema = SchemaGenerator.Convert(runtimeType);
		Assert.AreEqual(true, schema.AdditionalPropertiesAllowed);
		Assert.AreEqual(typeName, schema.AdditionalProperties.Type);
		Assert.AreEqual(format, schema.AdditionalProperties.Format);
	}
	
	[TestCase(typeof(Sample[]))]
	[TestCase(typeof(List<Sample>))]
	public void CheckListOfObjects(Type runtimeType)
	{
		var schema = SchemaGenerator.Convert(runtimeType);
		Assert.AreEqual("microserviceTests.OpenAPITests.TypeTests.Sample", schema.Items.Reference.Id);
	}


	[Test]
	public void CheckObject()
	{
		var schema = SchemaGenerator.Convert(typeof(Vector2));
		
		Assert.AreEqual(2, schema.Properties.Count);
		
		Assert.AreEqual("number", schema.Properties["x"].Type);
		Assert.AreEqual("float", schema.Properties["x"].Format);
		
		Assert.AreEqual("number", schema.Properties["y"].Type);
		Assert.AreEqual("float", schema.Properties["y"].Format);
	}
	
	[Test]
	public void CheckObjectWithReference()
	{
		var schema = SchemaGenerator.Convert(typeof(Sample));
		
		Assert.AreEqual("this is a sample", schema.Description);
		Assert.AreEqual(1, schema.Properties.Count);
		
		Assert.AreEqual("microserviceTests.OpenAPITests.TypeTests.Tuna", schema.Properties[nameof(Sample.fish)].Reference.Id);
		Assert.AreEqual("a fish", schema.Properties[nameof(Sample.fish)].Description);
	}

	[Test]
	public void CheckEnums()
	{
		var schema = SchemaGenerator.Convert(typeof(Fish));
		Assert.AreEqual(2, schema.Enum.Count);
	}
	
	
	[Test]
	public void CheckEnumsOnObject()
	{
		var schema = SchemaGenerator.Convert(typeof(FishThing));
		Assert.AreEqual(1, schema.Properties.Count);

		var prop = schema.Properties[nameof(FishThing.type)];
		Assert.AreEqual("microserviceTests.OpenAPITests.TypeTests.Fish", prop.Reference.Id);
	}
	
	[TestCase(typeof(BeamAccountId), "integer", "int64")]
	[TestCase(typeof(BeamCid), "integer", "int64")]
	[TestCase(typeof(BeamContentId), "string", null)]
	[TestCase(typeof(BeamContentManifestId), "string", null)]
	[TestCase(typeof(BeamGamerTag), "integer", "int64")]
	[TestCase(typeof(BeamPid), "string", null)]
	[TestCase(typeof(BeamStats), "string", null)]
	[TestCase(typeof(ServiceName), "string", null)]
	public void CheckSemanticTypes(Type type, string typeName, string format)
	{
		var schema = SchemaGenerator.Convert(type);
		Assert.AreEqual(typeName, schema.Type);
		Assert.AreEqual(format, schema.Format);
	}


	/// <summary>
	/// this is a sample
	/// </summary>
	public class Sample
	{
		/// <summary>
		/// a fish
		/// </summary>
		public Tuna fish;
	}

	/// <summary>
	/// the fish
	/// </summary>
	public class Tuna
	{
		public int smelly;
	}
	
	public enum Fish { Tuna, Salmon }

	public class FishThing
	{
		public Fish type;
	}
}
