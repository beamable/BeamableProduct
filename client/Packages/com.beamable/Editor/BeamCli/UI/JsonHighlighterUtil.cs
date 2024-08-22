using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections;
using System.Text;

namespace Beamable.Editor.BeamCli.UI
{
	public static class JsonHighlighterUtil
	{
		private const string OBJECT_OPEN = "{";
		private const string OBJECT_CLOSE = "}";
		private const string ARRAY_OPEN = "[";
		private const string ARRAY_CLOSE = "]";
		private const string KEY_OPEN = "<color=#fbb863>";
		private const string KEY_CLOSE = "</color>";
		private const string VAL_OPEN = "<color=#ffeedb>";
		private const string BOOL_OPEN = "<color=#ffd9ae>";
		private const string STR_OPEN = "<color=#bfb8b5>";
		private const string VAL_CLOSE = KEY_CLOSE;
		private const string KEY_SEPARATOR = ": ";
		private const string VALUE_SEPARATOR = ", ";
		private const string INDENT = "  ";
		private const string LINEBREAK = "\n";
		
		public static string HighlightJson(string json)
		{
			var dict = (ArrayDict)Json.Deserialize(json);
			var sb = new StringBuilder();
			Highlight2(dict, sb, 0);
			var formatted = sb.ToString();
			return formatted;
		}
		
		public static string HighlightJson(ArrayDict dict)
		{
			var sb = new StringBuilder();
			Highlight2(dict, sb, 0);
			var formatted = sb.ToString();
			return formatted;
		}
		

		static void AppendIndents(StringBuilder sb, int indents)
		{
			for (var i = 0; i < indents; i++)
			{
				sb.Append(INDENT);
			}
			

		}

		static void Highlight2(ArrayDict dict, StringBuilder sb, int indents, bool applyOpeningIndent = true)
		{
			if (applyOpeningIndent)
			{
				AppendIndents(sb, indents);
			}
			indents++;
			sb.Append(OBJECT_OPEN);
			
			if (dict.Count > 0)
			{
				sb.Append(LINEBREAK);
			}

			var count = dict.Count;
			foreach (var kvp in dict)
			{
				count--;
				AppendIndents(sb, indents);
				sb.Append(KEY_OPEN);
				sb.Append(kvp.Key);
				sb.Append(KEY_CLOSE);
				sb.Append(KEY_SEPARATOR);
				
				HighlightValue(kvp.Value, sb, indents);

				if (count > 0)
				{
					sb.Append(VALUE_SEPARATOR);
				}
				sb.Append(LINEBREAK);
			}
			
			indents--;
			AppendIndents(sb, indents);
			sb.Append(OBJECT_CLOSE);
		}

		static void HighlightValue(object value, StringBuilder sb, int indents)
		{
			if (value is int || value is float || value is long || value is double || value is bool)
			{
				sb.Append(BOOL_OPEN);
				sb.Append(value.ToString().ToLowerInvariant());
				sb.Append(VAL_CLOSE);
			} else if (value is string)
			{
				sb.Append(STR_OPEN);
				sb.Append("\"");
				sb.Append(VAL_CLOSE);

				sb.Append(BOOL_OPEN);
				sb.Append(value.ToString());
				sb.Append(VAL_CLOSE);

				sb.Append(STR_OPEN);
				sb.Append("\"");
				sb.Append(VAL_CLOSE);
			} else if (value is ArrayDict subDict)
			{
				Highlight2(subDict, sb, indents, false);
			} else if (value is IList listable)
			{
				sb.Append(ARRAY_OPEN);
				var count = listable.Count;
				foreach (var element in listable)
				{
					count--;
					sb.Append(LINEBREAK);
					AppendIndents(sb, indents + 1);
					
					// serialize value.
					HighlightValue(element, sb, indents + 1);

					if (count > 0)
					{
						sb.Append(VALUE_SEPARATOR);
					}
				}
				sb.Append(LINEBREAK);
				AppendIndents(sb, indents);
				sb.Append(ARRAY_CLOSE);
				
			}
			else
			{
				// ? 
			}
		}
	}
}
