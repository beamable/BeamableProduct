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
		List<TsType> oneOfTypes;

		switch (schemaType, schemaFormat, schemaRefId)
		{
			// Maps an array schema with polymorphic oneOf items to a TypeScript union of oneOf types within an array
			case ("array", _, _) when schema.Items?.OneOf?.Count > 0:
				oneOfTypes = GetOneOfTypes(schema.Items.OneOf, ref modules);
				return TsType.ArrayOf(TsType.Union(oneOfTypes.ToArray()));

			// Maps a simple array schema to a TypeScript array of the mapped element type
			case ("array", _, _):
				var elementType = Map(schema.Items, ref modules);
				return TsType.ArrayOf(elementType);

			// Maps the special OptionalJsonNodeWrapper object (with x-beamable-json-object) to a TypeScript string
			case ("object", _, "OptionalJsonNodeWrapper")
				when schema.Extensions.TryGetValue("x-beamable-json-object", out _):
				return TsType.String;

			// Maps any schema with a reference ID to the corresponding TypeScript type by name
			case var (_, _, referenceId) when !string.IsNullOrEmpty(referenceId):
				modules.Add(referenceId);
				return TsType.Of(referenceId);

			// Maps a root-level oneOf polymorphic schema to a TypeScript union of oneOf types
			case (_, _, _) when schema.OneOf?.Count > 0:
				oneOfTypes = GetOneOfTypes(schema.OneOf, ref modules);
				return TsType.Union(oneOfTypes.ToArray());

			// Maps an object schema with additionalPropertiesAllowed to a TypeScript Record<string, <mapped type>>
			case ("object", _, _) when schema.Reference == null && schema.AdditionalPropertiesAllowed:
				var additionalPropsSchema = schema.AdditionalProperties;
				var type = Map(additionalPropsSchema, ref modules);
				return TsUtilityType.Record(TsType.String, type);

			// Throws for object schemas without reference or additionalProperties since they cannot be mapped
			case ("object", _, _) when schema is { Reference: null, AdditionalPropertiesAllowed: false }:
				throw new Exception("Cannot build a reference to a schema that is just an object...");

			// Maps boolean schema to TypeScript boolean
			case ("boolean", _, _):
				return TsType.Boolean;

			// Maps string schema with date-time format to TypeScript Date
			case ("string", "date-time", _):
				return TsType.Date;

			// Maps various string schemas (UUID, Base64, .NET System.String, generic string) to TypeScript string
			case ("string", "uuid", _): // i.e. a UUID
			case ("string", "byte", _): // i.e. a Base64‐encoded string
			case ("System.String", _, _):
			case ("string", _, _):
				if (schema.Enum == null || schema.Enum.Count == 0)
					return TsType.String;
				// Maps enum string schemas to a TypeScript union of string literals
				var enumStringTypes = schema.Enum
					.Select(e => e is OpenApiString eStr ? TsType.Of($"\"{eStr.Value}\"") : null)
					.Where(e => e is not null)
					.ToArray();
				return TsType.Union(enumStringTypes);

			// Maps floating-point and small integer schemas (float, double, integer int16/int32) to TypeScript number
			case ("number", "float", _):
			case ("number", "double", _):
			case ("number", _, _):
			case ("integer", "int16", _):
			case ("integer", "int32", _):
				return TsType.Number;

			// Maps 64-bit integer schemas to TypeScript bigint (BigInt for values beyond 2^53 - 1) and string for safety
			case ("integer", "int64", _):
				return TsType.Union(TsType.BigInt, TsType.String); // use bigint for large numbers and string for safety

			// Maps any other integer schema to TypeScript number
			case ("integer", _, _):
				return TsType.Number;

			// Fallback mapping for unrecognized schema combinations to TypeScript any
			default:
				return TsType.Any;
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
