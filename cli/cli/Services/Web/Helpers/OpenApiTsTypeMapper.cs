using cli.Services.Web.CodeGen;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace cli.Services.Web.Helpers;

public static class OpenApiTsTypeMapper
{
	/// <summary>
	/// Maps an OpenAPI schema to the corresponding TypeScript type.
	/// </summary>
	public static TsType Map(OpenApiSchema schema, ref List<string> modules)
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
				oneOfTypes = GetOneOfTypes(schema.Items.OneOf, ref modules);
				var returnType = TsType.ArrayOf(TsType.Union(oneOfTypes.ToArray()));
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Maps a simple array schema to a TypeScript array of the mapped element type
			case ("array", _, _):
			{
				var elementType = Map(schema.Items, ref modules);
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
				oneOfTypes = GetOneOfTypes(schema.OneOf, ref modules);
				var returnType = TsType.Union(oneOfTypes.ToArray());
				return isNullable ? TsType.Union(returnType, TsType.Null) : returnType;
			}

			// Maps an object schema with additionalPropertiesAllowed to a TypeScript Record<string, <mapped type>>
			case ("object", _, _) when schema.Reference == null && schema.AdditionalPropertiesAllowed:
			{
				var additionalPropsSchema = schema.AdditionalProperties;
				var type = Map(additionalPropsSchema, ref modules);
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

			// Maps 64-bit integer schemas to TypeScript bigint (BigInt for values beyond 2^53 - 1) and string for safety
			case ("integer", "int64", _):
			{
				// use bigint for large numbers and string for safety
				var returnType = TsType.Union(TsType.BigInt, TsType.String);
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

	private static List<TsType> GetOneOfTypes(IList<OpenApiSchema> oneOfList, ref List<string> modules)
	{
		var moduleList = modules;
		return oneOfList.Select(schema =>
		{
			var type = schema.Reference.Id;
			moduleList.Add(type);
			return TsType.Of(type);
		}).ToList();
	}
}
