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
	}
}
