using cli;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace tests;

public class OpenApiEqualityTests
{

	[TestCase(true, new[] { "a", "b" }, new[] { "b", "a" })]
	[TestCase(false, new[] { "a" }, new[] { "b" })]
	public void Equality_Required(bool expected, string[] a, string[] b)
	{
		var schemaA = new OpenApiSchema { Required = new HashSet<string>(a) };
		var schemaB = new OpenApiSchema { Required = new HashSet<string>(b) };

		var isEqual = NamedOpenApiSchema.AreEqual(schemaA, schemaB, out var differences);
		foreach (var diff in differences)
		{
			Console.WriteLine(diff);
		}
		Assert.AreEqual(expected, isEqual);
	}

	[TestCase(true, true, true)]
	[TestCase(false, false, true)]
	public void Equality_AdditionalPropertiesAllowed(bool expected, bool a, bool b)
	{
		var schemaA = new OpenApiSchema { AdditionalPropertiesAllowed = a };
		var schemaB = new OpenApiSchema { AdditionalPropertiesAllowed = b };

		var isEqual = NamedOpenApiSchema.AreEqual(schemaA, schemaB, out var differences);
		foreach (var diff in differences)
		{
			Console.WriteLine(diff);
		}
		Assert.AreEqual(expected, isEqual);
	}

	[TestCase(true, "a", "a")]
	[TestCase(false, "b", "a")]
	public void Equality_AdditionalProperties(bool expected, string a, string b)
	{
		var schemaA = new OpenApiSchema
		{
			AdditionalProperties = new OpenApiSchema
			{
				Type = a
			}
		};
		var schemaB = new OpenApiSchema
		{
			AdditionalProperties = new OpenApiSchema
			{
				Type = b
			}
		};

		var isEqual = NamedOpenApiSchema.AreEqual(schemaA, schemaB, out var differences);
		foreach (var diff in differences)
		{
			Console.WriteLine(diff);
		}
		Assert.AreEqual(expected, isEqual);
	}


	[TestCase(true, "a", "a")]
	[TestCase(false, "b", "a")]
	public void Equality_Items(bool expected, string a, string b)
	{
		var schemaA = new OpenApiSchema
		{
			Items = new OpenApiSchema
			{
				Type = a
			}
		};
		var schemaB = new OpenApiSchema
		{
			Items = new OpenApiSchema
			{
				Type = b
			}
		};

		var isEqual = NamedOpenApiSchema.AreEqual(schemaA, schemaB, out var differences);
		foreach (var diff in differences)
		{
			Console.WriteLine(diff);
		}
		Assert.AreEqual(expected, isEqual);
	}


	[TestCase(true, "x", "y", "a", "b", "x", "y", "a", "b")]
	[TestCase(false, "x", "z", "a", "b", "x", "y", "a", "b")]
	[TestCase(false, "x", "y", "a", "b", "x", "y", "a", "c")]
	public void Equality_Properties(bool expected, string aKey1, string aKey2, string aTitle1, string aTitle2, string bKey1, string bKey2, string bTitle1, string bTitle2)
	{
		var schemaA = new OpenApiSchema
		{
			Properties = new Dictionary<string, OpenApiSchema>
			{
				[aKey1] = new OpenApiSchema { Type = aTitle1 },
				[aKey2] = new OpenApiSchema { Type = aTitle2 },
			}
		};
		var schemaB = new OpenApiSchema
		{
			Properties = new Dictionary<string, OpenApiSchema>
			{
				[bKey1] = new OpenApiSchema { Type = bTitle1 },
				[bKey2] = new OpenApiSchema { Type = bTitle2 },
			}
		};


		var isEqual = NamedOpenApiSchema.AreEqual(schemaA, schemaB, out var differences);
		foreach (var diff in differences)
		{
			Console.WriteLine(diff);
		}
		Assert.AreEqual(expected, isEqual);
	}

	[TestCase(true, "a", "aTitle", "a", "aTitle")]
	[TestCase(false, "a", "aTitle", "b", "aTitle")]
	public void Equality_Reference(bool expected, string aId, string aTitle, string bId, string bTitle)
	{
		var schemaA = new OpenApiSchema
		{
			Reference = new OpenApiReference
			{
				Id = aId,
				HostDocument = new OpenApiDocument { Info = new OpenApiInfo { Title = aTitle } }
			}
		};
		var schemaB = new OpenApiSchema
		{
			Reference = new OpenApiReference
			{
				Id = bId,
				HostDocument = new OpenApiDocument { Info = new OpenApiInfo { Title = bTitle } }
			}
		};

		var isEqual = NamedOpenApiSchema.AreEqual(schemaA, schemaB, out var differences);
		foreach (var diff in differences)
		{
			Console.WriteLine(diff);
		}
		Assert.AreEqual(expected, isEqual);
	}


	[TestCase(true, "a", "a")]
	[TestCase(true, null, null)]
	[TestCase(false, "a", "b")]
	[TestCase(false, "a", null)]
	public void Equality_Types(bool expected, string a, string b)
	{
		var schemaA = new OpenApiSchema { Type = a };
		var schemaB = new OpenApiSchema { Type = b };

		var isEqual = NamedOpenApiSchema.AreEqual(schemaA, schemaB, out var differences);
		foreach (var diff in differences)
		{
			Console.WriteLine(diff);
		}
		Assert.AreEqual(expected, isEqual);
	}

	[TestCase(true, "a", "a")]
	[TestCase(true, null, null)]
	[TestCase(false, "a", "b")]
	[TestCase(false, "a", null)]
	public void Equality_Formats(bool expected, string a, string b)
	{
		var schemaA = new OpenApiSchema { Format = a };
		var schemaB = new OpenApiSchema { Format = b };

		var isEqual = NamedOpenApiSchema.AreEqual(schemaA, schemaB, out var differences);
		foreach (var diff in differences)
		{
			Console.WriteLine(diff);
		}
		Assert.AreEqual(expected, isEqual);
	}


}
