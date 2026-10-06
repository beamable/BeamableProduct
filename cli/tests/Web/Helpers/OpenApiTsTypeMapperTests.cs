using cli.Services.Web.Helpers;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using System.Collections.Generic;

namespace tests.Web.Helpers;

[TestFixture]
public class OpenApiTsTypeMapperTests
{
	private static OpenApiSchema Int64(bool nullable = false) =>
		new() { Type = "integer", Format = "int64", Nullable = nullable };

	[TestCase(TsInt64Mapping.BigIntUnion, false, "bigint | string")]
	[TestCase(TsInt64Mapping.Number, false, "number")]
	[TestCase(TsInt64Mapping.String, false, "string")]
	[TestCase(TsInt64Mapping.BigIntUnion, true, "bigint | string | null")]
	[TestCase(TsInt64Mapping.Number, true, "number | null")]
	[TestCase(TsInt64Mapping.String, true, "string | null")]
	public void Map_Int64_UsesMapping(TsInt64Mapping mapping, bool nullable, string expected)
	{
		var modules = new List<string>();

		var result = OpenApiTsTypeMapper.Map(Int64(nullable), ref modules, mapping).Render();

		Assert.AreEqual(expected, result);
		Assert.IsEmpty(modules);
	}

	[Test]
	public void Map_Int64_DefaultsToBigIntUnion()
	{
		var modules = new List<string>();

		Assert.AreEqual("bigint | string", OpenApiTsTypeMapper.Map(Int64(), ref modules).Render());
		Assert.AreEqual("bigint | string | null", OpenApiTsTypeMapper.Map(Int64(true), ref modules).Render());
	}

	[TestCase(TsInt64Mapping.BigIntUnion, "(bigint | string)[]")]
	[TestCase(TsInt64Mapping.Number, "number[]")]
	[TestCase(TsInt64Mapping.String, "string[]")]
	public void Map_Int64Array_UsesMappingForItems(TsInt64Mapping mapping, string expected)
	{
		var modules = new List<string>();
		var schema = new OpenApiSchema { Type = "array", Items = Int64() };

		Assert.AreEqual(expected, OpenApiTsTypeMapper.Map(schema, ref modules, mapping).Render());
	}

	[TestCase(TsInt64Mapping.Number, "Record<string, number>")]
	[TestCase(TsInt64Mapping.String, "Record<string, string>")]
	public void Map_Int64Dictionary_UsesMappingForValues(TsInt64Mapping mapping, string expected)
	{
		var modules = new List<string>();
		var schema = new OpenApiSchema { Type = "object", AdditionalPropertiesAllowed = true, AdditionalProperties = Int64() };

		Assert.AreEqual(expected, OpenApiTsTypeMapper.Map(schema, ref modules, mapping).Render());
	}

	[TestCase(TsInt64Mapping.Number)]
	[TestCase(TsInt64Mapping.String)]
	public void Map_OtherIntegers_IgnoreInt64Mapping(TsInt64Mapping mapping)
	{
		var modules = new List<string>();

		Assert.AreEqual("number",
			OpenApiTsTypeMapper.Map(new OpenApiSchema { Type = "integer", Format = "int32" }, ref modules, mapping)
				.Render());
		Assert.AreEqual("number",
			OpenApiTsTypeMapper.Map(new OpenApiSchema { Type = "number", Format = "double" }, ref modules, mapping)
				.Render());
	}

	[TestCase("bigint-union", TsInt64Mapping.BigIntUnion)]
	[TestCase("number", TsInt64Mapping.Number)]
	[TestCase("string", TsInt64Mapping.String)]
	[TestCase(" Number ", TsInt64Mapping.Number)]
	public void TryParseInt64Mapping_AcceptsDocumentedValues(string value, TsInt64Mapping expected)
	{
		Assert.IsTrue(OpenApiTsTypeMapper.TryParseInt64Mapping(value, out var mapping));
		Assert.AreEqual(expected, mapping);
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("bigint")]
	[TestCase("long")]
	public void TryParseInt64Mapping_RejectsOtherValues(string value)
	{
		Assert.IsFalse(OpenApiTsTypeMapper.TryParseInt64Mapping(value, out _));
	}
}
