using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using System.Reflection;

namespace beamable.otel.exporter.Utils;

public class OtlpExporterResourceInjector
{
	public static bool TryInjectResource(OtlpLogExporter exporter, Dictionary<string, object> resourceAttributes)
	{
		var customResource = ResourceBuilder.CreateEmpty()
			.AddAttributes(resourceAttributes.Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value)))
			.Build();

		var resourceField = typeof(OtlpLogExporter)
			.GetField("resource", BindingFlags.NonPublic | BindingFlags.Instance);

		if (resourceField != null && resourceField.FieldType == typeof(Resource))
		{
			resourceField.SetValue(exporter, customResource);
			return true;
		}

		return false;
	}

	public static object ParseStringToObject(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return null;
		}

		return value switch
		{
			_ when bool.TryParse(value, out bool boolResult) => boolResult,
			_ when int.TryParse(value, out int intResult) => intResult,
			_ when long.TryParse(value, out long longResult) => longResult,
			_ when double.TryParse(value, out double doubleResult) => doubleResult,
			_ when DateTime.TryParse(value, out DateTime dateResult) => dateResult,
			_ => value
		};
	}
}
