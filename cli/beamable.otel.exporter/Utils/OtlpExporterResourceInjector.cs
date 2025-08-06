using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using System.Reflection;

namespace beamable.otel.exporter.Utils;

public class OtlpExporterResourceInjector
{

	public static bool TrySetResourceField<T>(T exporter, Dictionary<string, object> resourceAttributes, out string errorMessage)
	{
		var type = typeof(T);
		var baseType = type.BaseType;
		var fieldName = "resource"; //TODO make this a constant prob

		if (baseType != null && baseType.IsGenericType) // This is validating that the exporter type is a BaseExporter<T>, with T being either LogRecord, Activity or Metric
		{
			var genericTypeDef = baseType.GetGenericTypeDefinition();

			if (IsBaseExporterType(genericTypeDef))
			{
				var genericArgs = baseType.GetGenericArguments();
				if (genericArgs.Length == 1)
				{
					var argName = genericArgs[0].Name;
					if (argName != "LogRecord" && argName != "Activity" && argName != "Metric")
					{
						errorMessage =
							"The exporter passed needs to be implementing BaseExporter<T> with T being either LogRecord, Activity or Metric";
						return false;
					}
				}
			}
		}

		var customResource = ResourceBuilder.CreateEmpty()
			.AddAttributes(resourceAttributes.Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value)))
			.Build();

		var field = type.GetField(fieldName,
				BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

		if (field == null)
		{
			errorMessage = "Couldn't find the field 'resource' in the passed exporter object";
			return false;
		}

		field.SetValue(exporter, customResource);
		errorMessage = string.Empty;
		return true;
	}

	private static bool IsBaseExporterType(Type genericTypeDef)
	{
		return genericTypeDef.Name == "BaseExporter`1" ||
		       genericTypeDef.FullName?.StartsWith("OpenTelemetry.BaseExporter`1") == true;
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
