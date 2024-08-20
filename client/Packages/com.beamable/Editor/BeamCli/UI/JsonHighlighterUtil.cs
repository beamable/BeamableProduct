using Beamable.Serialization.SmallerJSON;
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
		private const string INDENT = "  ";
		private const string LINEBREAK = "\n";
		
		public static string HighlightJson(string json)
		{
			var dict = (ArrayDict)Json.Deserialize(json);
			var sb = new StringBuilder();
			Highlight(dict, sb, 0);
			var formatted = sb.ToString();
			return formatted;
		}
		
		public static string HighlightJson(ArrayDict dict)
		{
			var sb = new StringBuilder();
			Highlight(dict, sb, 0);
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

		static void Highlight(ArrayDict dict, StringBuilder sb, int indents, bool applyOpeningIndent=true)
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
			
			foreach (var kvp in dict)
			{
				AppendIndents(sb, indents);
				sb.Append(KEY_OPEN);
				sb.Append(kvp.Key);
				sb.Append(KEY_CLOSE);
				sb.Append(KEY_SEPARATOR);

				switch (kvp.Value)
				{
					case IList listable:
						sb.Append(ARRAY_OPEN);
				
						foreach (var element in listable)
						{
							// TODO: refactor to use some recursive definitions
							sb.Append(LINEBREAK);
							AppendIndents(sb, indents + 1);
							switch (element)
							{
								case ArrayDict subDict:
									break;
								case string:
									sb.Append(STR_OPEN);
									sb.Append("\"");
									sb.Append(VAL_CLOSE);

									sb.Append(BOOL_OPEN);
									sb.Append(element.ToString().ToLowerInvariant());
									sb.Append(VAL_CLOSE);

									sb.Append(STR_OPEN);
									sb.Append("\"");
									sb.Append(VAL_CLOSE);
									break;
								case int:
								case long:
								case bool:
									sb.Append(BOOL_OPEN);
									sb.Append(element.ToString().ToLowerInvariant());
									sb.Append(VAL_CLOSE);
									break;
								default:
									sb.Append(VAL_OPEN);
									sb.Append(element);
									sb.Append(VAL_CLOSE);
									break;
							}
						}
						sb.Append(LINEBREAK);
						AppendIndents(sb, indents);
						sb.Append(ARRAY_CLOSE);
						break;
					case ArrayDict subDict:
						Highlight(subDict, sb, indents, applyOpeningIndent: false);
						break;
					case string:
						sb.Append(STR_OPEN);
						sb.Append("\"");
						sb.Append(VAL_CLOSE);

						sb.Append(BOOL_OPEN);
						sb.Append(kvp.Value.ToString().ToLowerInvariant());
						sb.Append(VAL_CLOSE);

						sb.Append(STR_OPEN);
						sb.Append("\"");
						sb.Append(VAL_CLOSE);
						break;
					case int:
					case long:
					case bool:
						sb.Append(BOOL_OPEN);
						sb.Append(kvp.Value.ToString().ToLowerInvariant());
						sb.Append(VAL_CLOSE);
						break;
					default:
						sb.Append(VAL_OPEN);
						sb.Append(kvp.Value);
						sb.Append(VAL_CLOSE);
						break;
				}
				sb.Append(LINEBREAK);
			}

			indents--;
			AppendIndents(sb, indents);
			sb.Append(OBJECT_CLOSE);
			
		}
	}
}
