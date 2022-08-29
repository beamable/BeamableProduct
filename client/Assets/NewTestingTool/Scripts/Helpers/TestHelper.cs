using Beamable.Common;
using Beamable.NewTestingTool.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.NewTestingTool.Helpers
{
	public static class TestHelper
	{
		private static readonly Dictionary<TestResult, string> TestResultsDict = new Dictionary<TestResult, string>
		{
			{ TestResult.NotSet, "resultNotSet" },
			{ TestResult.Passed, "resultPassed" },
			{ TestResult.Failed, "resultFailed" },
		};
		private static readonly Dictionary<TestResult, Color> TestResultToColorDict = new Dictionary<TestResult, Color>()
		{
			{ TestResult.NotSet, Color.yellow },
			{ TestResult.Passed, Color.green },
			{ TestResult.Failed, Color.red },
		};
		
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

		public static bool IsPromiseMethod(MethodInfo methodInfo)
			=> methodInfo.ReturnType.ToString().ToLower().Contains("promise");

		public static string[] ConvertObjectToString(object[] objectValues)
		{
			if (objectValues == null)
				return null;
			
			var convertedValues = new List<string>();
			foreach (var value in objectValues)
			{
				var type = value.GetType();
				convertedValues.Add($"{type.Name.ToLower()}_{value}");
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

				object convertedValue;
				switch (type)
				{
					case "byte":
						convertedValue = byte.Parse(val);
						break;
					case "char":
						convertedValue = char.Parse(val);
						break;
					case "short":
					case "int16":
						convertedValue = short.Parse(val);
						break;
					case "int":
					case "int32":
						convertedValue = int.Parse(val);
						break;
					case "long":
					case "int64":
						convertedValue = long.Parse(val);
						break;
					case "single":
					case "float":
						convertedValue = float.Parse(val);
						break;
					case "double":
						convertedValue = double.Parse(val);
						break;
					case "boolean":
					case "bool":
						convertedValue = bool.Parse(val);
						break;
					case "string":
						convertedValue = val;
						break;
					default:
						convertedValue = null;
						break;
				}

				originalValues.Add(convertedValue);
			}
			return originalValues.ToArray();
		}
		
		public static void SetTestResult(VisualElement ve, TestResult result)
		{
			foreach (var testResult in TestResultsDict)
				ve.EnableInClassList(testResult.Value, false);
			ve.EnableInClassList(TestResultsDict[result], true);
		}
		public static Color ConvertTestResultToColor(TestResult testResult)
		{
			if (!TestResultToColorDict.ContainsKey(testResult))
				throw new InvalidEnumArgumentException($"TestResult=[{testResult}] is not defined in color dict!");
			return TestResultToColorDict[testResult];
		}
	}
}
