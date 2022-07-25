using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NewTestingTool.Helpers
{
	public static class TestHelper
	{
		public static bool IsAsyncMethod(Type classType, string methodName)
		{
			MethodInfo methodInfo = classType.GetMethod(methodName);
			return IsAsyncMethod(methodInfo);
		}
		public static bool IsAsyncMethod(MethodInfo methodInfo)
		{
			var attType = typeof(AsyncStateMachineAttribute);
			var attrib = (AsyncStateMachineAttribute)methodInfo.GetCustomAttribute(attType);
			return attrib != null;
		}
		public static string[] ConvertObjectToString(object[] objectValues)
		{
			if (objectValues == null)
				return null;
			
			var convertedValues = new List<string>();
			foreach (var value in objectValues)
			{
				var type = value.GetType();
				convertedValues.Add($"{type.Name}_{value}");
			}
			return convertedValues.ToArray();
		}
		public static object[] ConvertStringToObject(string[] stringValues)
		{
			if (stringValues == null)
				return null;
			
			var originalValues = new List<object>();
			foreach (var value in stringValues)
			{
				var splitted = value.Split('_');
				var type = splitted[0];
				var val = splitted[1];

				object convertedValue = type switch
				{
					"byte" => byte.Parse(val),
					"char" => char.Parse(val),
					"short" => short.Parse(val),
					"int" => int.Parse(val),
					"long" => long.Parse(val),
					"float" => float.Parse(val),
					"double" => double.Parse(val),
					"bool" => bool.Parse(val),
					"string" => val,
					_ => null
				};
				
				originalValues.Add(convertedValue);
			}
			return originalValues.ToArray();
		}
	}
}
