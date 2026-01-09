using System;
using System.Collections.Generic;
using System.Linq;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using System.Reflection;

namespace beamable.otel.common
{

	public class OtlpExporterResourceInjector
	{

		public static bool TrySetResourceField<T>(T exporter, Dictionary<string, object> resourceAttributes,
			out string errorMessage)
		{
			var type = typeof(T);
			var baseType = type.BaseType;
			var fieldName = "resource";

			if (baseType != null &&
			    baseType.IsGenericType) // This is validating that the exporter type is a BaseExporter<T>, with T being either LogRecord, Activity or Metric
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

			return ConvertValue(value);
		}
		
		public static object ConvertValue(string value)
		{
			bool boolResult;
			if (bool.TryParse(value, out boolResult))
				return boolResult;

			int intResult;
			if (int.TryParse(value, out intResult))
				return intResult;

			long longResult;
			if (long.TryParse(value, out longResult))
				return longResult;

			double doubleResult;
			if (double.TryParse(value, out doubleResult))
				return doubleResult;

			DateTime dateResult;
			if (DateTime.TryParse(value, out dateResult))
				return dateResult;

			return value;
		}

	}
}