using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Beamable.Editor.UI.Buss
{
	public static class BussNameUtility
	{
		private static Regex _cleanRegex = new Regex("\\w+");

		public static string CleanString(string input)
		{
			return _cleanRegex?.Match(input)?.Value ?? "";
		}

		public static string AsIdSelector(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return "";
			return "#" + CleanString(input);
		}

		public static string AsClassSelector(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return "";
			return "." + CleanString(input);
		}

		public static string AsVariableName(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return "";
			return "--" + CleanString(input);
		}

		public static List<string> AsClassesList(List<string> classesList)
		{
			List<string> finalList = new List<string>();
			finalList.AddRange(classesList.Select(AsClassSelector));
			return finalList;
		}

		public static List<string> AsCleanList(List<string> list)
		{
			List<string> finalList = new List<string>();
			finalList.AddRange(list.Select(CleanString));
			return finalList;
		}
		
		public static string FormatLabel(BussElement component)
		{
			if (!component) return String.Empty;

			string label = string.IsNullOrWhiteSpace(component.Id)
				? component.name
				: AsIdSelector(component.Id);

			foreach (string className in component.Classes)
			{
				label += " " + AsClassSelector(className);
			}

			return label.Replace(" ", "");
		}
	}
}
