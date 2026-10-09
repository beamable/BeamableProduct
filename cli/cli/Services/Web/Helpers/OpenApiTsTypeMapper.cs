using cli.Services.Web.CodeGen;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace cli.Services.Web.Helpers;

/// <summary>
/// How a 64-bit integer schema (<c>integer</c>/<c>int64</c>, i.e. a C# <c>long</c>) is typed in TypeScript.
/// </summary>
public enum TsInt64Mapping
{
	/// <summary><c>bigint | string</c>. The historical default, kept for compatibility.</summary>
	BigIntUnion,

	/// <summary><c>number</c>. Values beyond 2^53 - 1 lose precision in JavaScript.</summary>
	Number,

	/// <summary><c>string</c>.</summary>
	String,
}

public static class OpenApiTsTypeMapper
{
	/// <summary>
	/// The accepted values for a <see cref="TsInt64Mapping"/> on the command line, in the order they are documented.
	/// </summary>
	public static readonly string[] Int64MappingNames = { "bigint-union", "number", "string" };

	/// <summary>
	/// Parses a command line value (<c>bigint-union</c>, <c>number</c> or <c>string</c>, case-insensitive) into a
	/// <see cref="TsInt64Mapping"/>.
	/// </summary>
	public static bool TryParseInt64Mapping(string value, out TsInt64Mapping mapping)
	{
		switch (value?.Trim().ToLowerInvariant())
		{
			case "bigint-union":
				mapping = TsInt64Mapping.BigIntUnion;
				return true;
			case "number":
				mapping = TsInt64Mapping.Number;
				return true;
			case "string":
				mapping = TsInt64Mapping.String;
				return true;
			default:
				mapping = TsInt64Mapping.BigIntUnion;
				return false;
		}
	}

	/// <summary>
	/// Maps an OpenAPI schema to the corresponding TypeScript type.
	/// </summary>
	/// <param name="schema">The schema to map.</param>
	/// <param name="modules">Receives the names of referenced (non-primitive) types.</param>
	/// <param name="int64Mapping">How <c>integer</c>/<c>int64</c> schemas are typed. Defaults to <c>bigint | string</c>.</param>
	public static TsType Map(OpenApiSchema schema, ref List<string> modules,
		TsInt64Mapping int64Mapping = TsInt64Mapping.BigIntUnion)
	{
		if (schema == null)
			return TsType.Unknown;

		string schemaType = schema.Type;
		string schemaFormat = schema.Format;
		string schemaRefId = schema.Reference?.Id;
		bool isNullable = schema.Nullable;
		List<TsType> oneOfTypes;

		switch (schemaType, schemaFormat, schemaRefId)
		{
			// Maps an array schema with polymorphic oneOf items to a TypeScript union of oneOf types within an array
			case ("array", _, _) when schema.Items?.OneOf?.Count > 0:
			{
				oneOfTypes = GetOneOfTypes(schema.Items.OneOf, ref modules, int64Mapping);
				var returnType = TsType.ArrayOf(TsType.Union(oneOfTypes.ToArray()));
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Maps a simple array schema to a TypeScript array of the mapped element type
			case ("array", _, _):
			{
				var elementType = Map(schema.Items, ref modules, int64Mapping);
				var returnType = TsType.ArrayOf(elementType);
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Maps the special OptionalJsonNodeWrapper object (with x-beamable-json-object) to a TypeScript string
			case ("object", _, "OptionalJsonNodeWrapper")
				when schema.Extensions.TryGetValue("x-beamable-json-object", out _):
			{
				var returnType = TsType.String;
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Maps any schema with a reference ID to the corresponding TypeScript type by name
			case var (_, _, referenceId) when !string.IsNullOrEmpty(referenceId):
			{
				if (referenceId.Contains('.'))
					referenceId = referenceId.Split('.').Last();

				if (referenceId == "DateTime")
				{
					var dateTimeReturnType = TsType.String;
					return isNullable ? TsType.Union(dateTimeReturnType, TsType.Null) : dateTimeReturnType;
				}

				modules.Add(referenceId);
				var returnType = TsType.Of(referenceId);
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Maps a root-level oneOf polymorphic schema to a TypeScript union of oneOf types
			case (_, _, _) when schema.OneOf?.Count > 0:
			{
				oneOfTypes = GetOneOfTypes(schema.OneOf, ref modules, int64Mapping);
				var returnType = TsType.Union(oneOfTypes.ToArray());
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Maps an object schema with additionalPropertiesAllowed to a TypeScript Record<string, <mapped type>>
			case ("object", _, _) when schema.Reference == null && schema.AdditionalPropertiesAllowed:
			{
				var additionalPropsSchema = schema.AdditionalProperties;
				var type = Map(additionalPropsSchema, ref modules, int64Mapping);
				var returnType = TsUtilityType.Record(TsType.String, type);
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Throws for object schemas without reference or additionalProperties since they cannot be mapped
			case ("object", _, _) when schema is { Reference: null, AdditionalPropertiesAllowed: false }:
			{
				var title = schema.Title;
				var returnType = TsType.Of(title);
				modules.Add(title);
				// Console.WriteLine("Cannot build a reference to a schema ({0}) that is just an object...", title);
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Maps boolean schema to TypeScript boolean
			case ("boolean", _, _):
			{
				var returnType = TsType.Boolean;
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Maps string schema with date-time format to TypeScript Date
			case ("string", "date-time", _):
			{
				var returnType = TsType.Date;
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Maps various string schemas (UUID, Base64, .NET System.String, generic string) to TypeScript string
			case ("string", "uuid", _): // i.e. a UUID
			case ("string", "byte", _): // i.e. a Base64‐encoded string
			case ("System.String", _, _):
			case ("string", _, _):
			{
				if (schema.Enum == null || schema.Enum.Count == 0)
					return isNullable ? TsType.Union(TsType.String, TsType.Null) : TsType.String;

				// Maps enum string schemas to a TypeScript union of string literals
				var enumStringTypes = schema.Enum
					.Select(e => e is OpenApiString eStr ? TsType.Of($"\"{eStr.Value}\"") : null)
					.Where(e => e is not null)
					.ToArray();
				var returnType = TsType.Union(enumStringTypes);
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Maps floating-point and small integer schemas (float, double, integer int16/int32) to TypeScript number
			case ("number", "float", _):
			case ("number", "double", _):
			case ("number", _, _):
			case ("integer", "int16", _):
			case ("integer", "int32", _):
			{
				var returnType = TsType.Number;
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Maps 64-bit integer schemas according to int64Mapping. The default is TypeScript bigint (BigInt for
			// values beyond 2^53 - 1) and string for safety.
			case ("integer", "int64", _):
			{
				var returnType = int64Mapping switch
				{
					TsInt64Mapping.Number => TsType.Number,
					TsInt64Mapping.String => TsType.String,
					_ => TsType.Union(TsType.BigInt, TsType.String),
				};
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Maps any other integer schema to TypeScript number
			case ("integer", _, _):
			{
				var returnType = TsType.Number;
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Fallback mapping for unrecognized schema combinations to TypeScript any
			default:
			{
				var returnType = TsType.Any;
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}
		}
	}

	private static List<TsType> GetOneOfTypes(IList<OpenApiSchema> oneOfList, ref List<string> modules,
		TsInt64Mapping int64Mapping)
	{
		// Route every oneOf member through Map rather than assuming each is a $ref. A oneOf can contain
		// inline objects, primitives, or nullable schemas (schema.Reference is null for those), which would
		// otherwise NRE on schema.Reference.Id. Map handles all of those cases (and adds modules) uniformly,
		// including the namespace-stripping and DateTime special-case a raw Reference.Id read would miss.
		var result = new List<TsType>(oneOfList.Count);
		foreach (var schema in oneOfList)
		{
			result.Add(Map(schema, ref modules, int64Mapping));
		}

		return result;
	}
}
