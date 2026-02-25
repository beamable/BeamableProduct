using beamable.server;
using Beamable.Server.Common;
using Beamable.Tooling.Common.OpenAPI;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Semantics;
using System.Text.Json;using UnityEngine;

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
		var requiredField = new HashSet<Type>();
		var schema = SchemaGenerator.Convert(runtimeType, ref requiredField);
		Assert.AreEqual(typeName, schema.Type);
		Assert.AreEqual(format, schema.Format);
	}

	[TestCase(typeof(float[]), "number", "float")]
	[TestCase(typeof(List<float>), "number", "float")]
	public void CheckPrimitiveArrays(Type runtimeType, string typeName, string format)
	{
		var requiredField = new HashSet<Type>();
		var schema = SchemaGenerator.Convert(runtimeType, ref requiredField);
		Assert.AreEqual(typeName, schema.Items.Type);
		Assert.AreEqual(format, schema.Items.Format);
	}
	
	[TestCase(typeof(Dictionary<string, int>), "integer", "int32")]
	public void CheckMapTypes(Type runtimeType, string typeName, string format)
	{
		var requiredField = new HashSet<Type>();
		var schema = SchemaGenerator.Convert(runtimeType, ref requiredField);
		Assert.AreEqual(true, schema.AdditionalPropertiesAllowed);
		Assert.AreEqual(typeName, schema.AdditionalProperties.Type);
		Assert.AreEqual(format, schema.AdditionalProperties.Format);
	}
	
	[TestCase(typeof(Sample[]))]
	[TestCase(typeof(List<Sample>))]
	public void CheckListOfObjects(Type runtimeType)
	{
		var requiredField = new HashSet<Type>();
		var schema = SchemaGenerator.Convert(runtimeType, ref requiredField);
		Assert.AreEqual("microserviceTests.OpenAPITests.TypeTests.Sample", Uri.UnescapeDataString(schema.Items.Reference.Id));
	}

	[Test]
	public void CheckMicroserviceRuntimeMetadata()
	{
		var requiredFields = new HashSet<Type>();
		var schema = SchemaGenerator.Convert(typeof(MicroserviceRuntimeMetadata),ref requiredFields);
		Assert.AreEqual("beamable.server.FederationComponentMetadata", schema.Properties["federatedComponents"].Items.Reference.Id);
		var doc = new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = "Test", Version = "0.0.0" },
			Paths = new OpenApiPaths(),
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, OpenApiSchema>()
			}
		};
		doc.Components.Schemas.Add(typeof(MicroserviceRuntimeMetadata).GetSanitizedFullName(), schema);
		SchemaGenerator.TryAddMissingSchemaTypes(ref doc, requiredFields);
		Assert.AreEqual(2, doc.Components.Schemas[typeof(FederationComponentMetadata).GetSanitizedFullName()].Properties.Count);
	}

	[Test]
	public void CheckObject()
	{
		var requiredField = new HashSet<Type>();
		var schema = SchemaGenerator.Convert(typeof(Vector2), ref requiredField);
		
		Assert.AreEqual(2, schema.Properties.Count);
		
		Assert.AreEqual("number", schema.Properties["x"].Type);
		Assert.AreEqual("float", schema.Properties["x"].Format);
		
		Assert.AreEqual("number", schema.Properties["y"].Type);
		Assert.AreEqual("float", schema.Properties["y"].Format);
	}
	
	[Test]
	public void CheckObjectWithReference()
	{
		var requiredField = new HashSet<Type>();
		var schema = SchemaGenerator.Convert(typeof(Sample), ref requiredField);
		
		Assert.AreEqual("this is a sample", schema.Description);
		Assert.AreEqual(1, schema.Properties.Count);
		
		Assert.AreEqual("microserviceTests.OpenAPITests.TypeTests.Tuna", Uri.UnescapeDataString(schema.Properties[nameof(Sample.fish)].Reference.Id));
		Assert.AreEqual("a fish", schema.Properties[nameof(Sample.fish)].Description);
		Assert.AreEqual(requiredField.Count, 1, "It should be missing Sample type definition");
	}
	[Test]
	public void CheckObjectWithGeneric()
	{
		var requiredFields = new HashSet<Type>();
		var schema = SchemaGenerator.Convert(typeof(SampleGenericField), ref requiredFields, 1, true);
		var doc = new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = "Test", Version = "0.0.0" },
			
			Paths = new OpenApiPaths(),
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, OpenApiSchema>()
			}
		};
		doc.Components.Schemas.Add(SchemaGenerator.GetQualifiedReferenceName(typeof(SampleGenericField)), schema);
		SchemaGenerator.TryAddMissingSchemaTypes(ref doc, requiredFields);
		
		Assert.AreEqual("this is a sample", schema.Description);
		Assert.AreEqual(1, schema.Properties.Count);
		Assert.AreEqual(doc.Components.Schemas[typeof(Result<string>).GetSanitizedFullName()].Properties[nameof(Result<string>.Field)].Type, "string");
	}

	[Test]
	public void CheckEnums()
	{
		var requiredField = new HashSet<Type>();
		var schema = SchemaGenerator.Convert(typeof(Fish), ref requiredField);
		Assert.AreEqual(2, schema.Enum.Count);
	}
	
	
	[Test]
	public void CheckEnumsOnObject()
	{
		var requiredField = new HashSet<Type>();
		var schema = SchemaGenerator.Convert(typeof(FishThing), ref requiredField);
		Assert.AreEqual(1, schema.Properties.Count);

		var prop = schema.Properties[nameof(FishThing.type)];
		Assert.AreEqual("microserviceTests.OpenAPITests.TypeTests.Fish", Uri.UnescapeDataString(prop.Reference.Id));
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
	/// this is a sample
	/// </summary>
	public class SampleGenericField
	{
		/// <summary>
		/// This is a field description
		/// </summary>
		public Result<string> theOnlyField;
	}

	/// <summary>
	/// A generic result class
	/// </summary>
	/// <typeparam name="T">Type of the field</typeparam>
	public class Result<T>
	{
		/// <summary>
		/// Description of the generic field
		/// </summary>
		public T Field;
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
