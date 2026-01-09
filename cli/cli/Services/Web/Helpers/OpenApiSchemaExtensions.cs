using Microsoft.OpenApi.Models;

namespace cli.Services.Web.Helpers;

public static class OpenApiSchemaExtensions
{
	private static readonly HashSet<string> _primitives = new()
	{
		"null",
		"boolean",
		"integer",
		"number",
		"string",
	};

	/// <summary>
	/// Returns true if the schema.Type is exactly one of the JSON primitive types.
	/// </summary>
	public static bool IsPrimitive(this OpenApiSchema schema)
	{
		if (schema == null || schema.Type == null)
			return false;

		return _primitives.Contains(schema.Type);
	}
}
