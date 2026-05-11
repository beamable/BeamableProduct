using System.Text.Json;
using System.Text.Json.Serialization;

namespace cli.Services.Analytics;

/// <summary>
/// One entry in the input file for the `beam analytics generate-validators` command.
/// Mirrors the body shape of `PUT /analytics/event/schemas/:batch` from the analytics
/// service spec — name + description + enabled + the JSON Schema body inline.
/// </summary>
public class AnalyticsEventSchema
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("description")]
	public string Description { get; set; } = string.Empty;

	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("schema")]
	public JsonSchemaBody Schema { get; set; } = new();

	public static List<AnalyticsEventSchema> ParseList(string json)
	{
		var options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = true,
		};
		return JsonSerializer.Deserialize<List<AnalyticsEventSchema>>(json, options) ?? new List<AnalyticsEventSchema>();
	}
}

public class JsonSchemaBody
{
	[JsonPropertyName("type")]
	public string Type { get; set; } = "object";

	[JsonPropertyName("properties")]
	public Dictionary<string, JsonSchemaProperty> Properties { get; set; } = new();

	[JsonPropertyName("required")]
	public List<string> Required { get; set; } = new();

	[JsonPropertyName("x-beamOpCode")]
	public string? BeamOpCode { get; set; }

	[JsonPropertyName("x-beamCategory")]
	public string? BeamCategory { get; set; }

	[JsonPropertyName("x-schemaVersion")]
	public string? SchemaVersion { get; set; }
}

public class JsonSchemaProperty
{
	[JsonPropertyName("type")]
	public string Type { get; set; } = "string";

	[JsonPropertyName("minimum")]
	public double? Minimum { get; set; }

	[JsonPropertyName("maximum")]
	public double? Maximum { get; set; }

	[JsonPropertyName("exclusiveMinimum")]
	public double? ExclusiveMinimum { get; set; }

	[JsonPropertyName("exclusiveMaximum")]
	public double? ExclusiveMaximum { get; set; }

	[JsonPropertyName("multipleOf")]
	public double? MultipleOf { get; set; }

	[JsonPropertyName("minLength")]
	public int? MinLength { get; set; }

	[JsonPropertyName("maxLength")]
	public int? MaxLength { get; set; }

	[JsonPropertyName("pattern")]
	public string? Pattern { get; set; }

	// Populated when Type == "object" — describes the nested custom type's shape.
	[JsonPropertyName("properties")]
	public Dictionary<string, JsonSchemaProperty>? Properties { get; set; }

	[JsonPropertyName("required")]
	public List<string>? Required { get; set; }
}
