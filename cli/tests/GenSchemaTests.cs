using cli;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using System;
using UnityEngine;

namespace tests;

public class GenSchemaTests
{


	[TestCase("number", "double", typeof(double), "double", TestName = "primitive double")]
	[TestCase("number", "float", typeof(float), "float", TestName = "primitive float")]
	[TestCase("number", null, typeof(double), "double", TestName = "primitive double by default")]
	[TestCase("boolean", null, typeof(bool), "bool", TestName = "primitive boolean by default")]
	[TestCase("string", null, typeof(string), "string", TestName = "primitive string by default")]
	[TestCase("string", "byte", typeof(byte), "byte", TestName = "primitive byte")]
	[TestCase("string", "uuid", typeof(Guid), "Guid", TestName = "primitive guid")]
	[TestCase("integer", "int64", typeof(long), "long", TestName = "primitive long")]
	[TestCase("integer", "int16", typeof(short), "short", TestName = "primitive short")]
	[TestCase("integer", "int32", typeof(int), "int", TestName = "primitive int")]
	[TestCase("integer", null, typeof(int), "int", TestName = "primitive int by default")]
	[Test]
	public void GeneratePrimitives(string type, string format, Type expected, string expectedTypeName)
	{
		var oapi = new OpenApiSchema { Type = type, Format = format };
		var gen = new GenSchema(oapi);

		var tRef = gen.GetTypeReference();

		Assert.AreEqual(0, tRef.ArrayRank);
		Assert.AreEqual(expected.FullName, tRef.BaseType);
		Assert.AreEqual(0, tRef.TypeArguments.Count);
		Assert.AreEqual(default, tRef.ArrayElementType);
		Assert.AreEqual(expectedTypeName, tRef.DisplayName);


		// check for optional version...
		var optionalRef = gen.GetOptionalTypeReference();
		Assert.AreEqual(0, optionalRef.ArrayRank);
		Assert.AreEqual(0, optionalRef.TypeArguments.Count);
		Assert.AreEqual($"Optional{tRef.UpperDisplayName}", optionalRef.DisplayName);
	}

	[TestCase("Tuna", TestName = "object ref")]
	[Test]
	public void GenerateObjectRef(string refId)
	{
		var oapi = new OpenApiSchema { Type = "object", Reference = new OpenApiReference() { Id = refId } };
		var gen = new GenSchema(oapi);

		var tRef = gen.GetTypeReference();

		Assert.AreEqual(0, tRef.ArrayRank);
		Assert.AreEqual(refId, tRef.BaseType);
		Assert.AreEqual(refId, tRef.DisplayName);
		Assert.AreEqual(refId, tRef.UpperDisplayName);
	}

	[TestCase("integer", null, typeof(int), TestName = "primitive array int")]
	[TestCase("string", null, typeof(string), TestName = "primitive array string")]
	[TestCase("integer", "int64", typeof(long), TestName = "primitive array long")]
	[Test]
	public void GeneratePrimitiveArrays(string type, string format, Type elementExpected)
	{
		var oapi = new OpenApiSchema { Type = "array", Items = new OpenApiSchema { Type = type, Format = format } };
		var gen = new GenSchema(oapi);

		var tRef = gen.GetTypeReference();

		Assert.AreEqual(1, tRef.ArrayRank);
		Assert.AreEqual(elementExpected.FullName, tRef.BaseType);
		Assert.AreEqual(0, tRef.TypeArguments.Count);
		Assert.AreEqual(elementExpected.FullName, tRef.ArrayElementType!.BaseType);

		var optionalTRef = gen.GetOptionalTypeReference();

	}

	[TestCase("Tuna", TestName = "complex array")]
	[Test]
	public void GenerateComplexArrays(string refId)
	{
		var oapi = new OpenApiSchema
		{
			Type = "array",
			Items = new OpenApiSchema
			{
				Reference = new OpenApiReference
				{
					Id = refId
				}
			}
		};
		var gen = new GenSchema(oapi);

		var tRef = gen.GetTypeReference();

		Assert.AreEqual(1, tRef.ArrayRank);
		Assert.AreEqual(refId, tRef.BaseType);
		Assert.AreEqual(0, tRef.TypeArguments.Count);
		Assert.AreEqual(refId, tRef.ArrayElementType!.BaseType);
	}

	[TestCase("integer", null, typeof(int), TestName = "primitive map int")]
	[TestCase("integer", "int64", typeof(long), TestName = "primitive map long")]
	[TestCase("string", "", typeof(string), TestName = "primitive map string")]
	[Test]
	public void GenerateMapToPrimitives(string title, string format, Type expectedType)
	{
		var oapi = new OpenApiSchema
		{
			Type = "object",
			AdditionalPropertiesAllowed = true,
			AdditionalProperties = new OpenApiSchema
			{
				Type = title,
				Format = format
			}
		};
		var gen = new GenSchema(oapi);

		var expectedGenType = new GenCodeTypeReference(expectedType);

		var tRef = gen.GetTypeReference();
		Assert.AreEqual(0, tRef.ArrayRank);
		Assert.AreEqual(tRef.BaseType, $"MapOf{expectedGenType.UpperDisplayName}");
		Assert.AreEqual(0, tRef.TypeArguments.Count);
		// Assert.AreEqual(typeof(string).FullName, tRef.TypeArguments[0].BaseType); // nope, this is just the type reference, so we don't want the type to have generics...
		// Assert.AreEqual(expectedType.FullName, tRef.TypeArguments[1].BaseType);
	}

	[TestCase("integer", null, typeof(int), TestName = "primitive array map int")]
	[TestCase("integer", "int64", typeof(long), TestName = "primitive array map long")]
	[TestCase("string", "", typeof(string), TestName = "primitive array map string")]
	[Test]
	public void GenerateMapToArrayPrimitives(string title, string format, Type expectedType)
	{
		var oapi = new OpenApiSchema
		{
			Type = "object",
			AdditionalPropertiesAllowed = true,
			AdditionalProperties = new OpenApiSchema
			{
				Type = "array",
				Items = new OpenApiSchema
				{
					Type = title,
					Format = format
				}
			}
		};
		var gen = new GenSchema(oapi);

		var expectedGenType = new GenCodeTypeReference(new GenCodeTypeReference(expectedType), 1);

		var tRef = gen.GetTypeReference();
		Assert.AreEqual(0, tRef.ArrayRank);
		Assert.AreEqual(tRef.BaseType, $"MapOf{expectedGenType.UpperDisplayName}");
		Assert.AreEqual(0, tRef.TypeArguments.Count);
		// Assert.AreEqual(typeof(string).FullName, tRef.TypeArguments[0].BaseType); // nope, this is just the type reference, so we don't want the type to have generics...
		// Assert.AreEqual(expectedType.FullName, tRef.TypeArguments[1].BaseType);
	}

	[TestCase("Tuna", TestName = "complex map")]
	[Test]
	public void GenerateMapToComplex(string refId)
	{
		var oapi = new OpenApiSchema
		{
			Type = "object",
			AdditionalPropertiesAllowed = true,
			AdditionalProperties = new OpenApiSchema
			{
				Reference = new OpenApiReference() { Id = refId }
			}
		};
		var gen = new GenSchema(oapi);

		// var expectedGenType = new GenCodeTypeReference(expectedType);

		var tRef = gen.GetTypeReference();
		Assert.AreEqual(0, tRef.ArrayRank);
		Assert.IsTrue(tRef.BaseType.Contains($"MapOf{refId}"));
		// Assert.AreEqual(2, tRef.TypeArguments.Count);
		// Assert.AreEqual(typeof(string).FullName, tRef.TypeArguments[0].BaseType);
		// Assert.AreEqual(expectedType.FullName, tRef.TypeArguments[1].BaseType);
	}
}
