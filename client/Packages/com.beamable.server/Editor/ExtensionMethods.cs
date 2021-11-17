namespace Beamable.Editor
{
	public static class ExtensionMethods
	{
		/// <summary>
		/// Tries to ellipse text if given length is lower than input text length
		/// </summary>
		/// <param name="inputText">Input text</param>
		/// <param name="length">Distance at which the text will be trimmed</param>
		/// <param name="outputText">Output text</param>
		/// <returns>The result of text operation. Returns true if text was trimmed</returns>
		public static bool TryEllipseText(this string inputText, int length, out string outputText)
		{
			outputText = inputText;
			if (length >= inputText.Length)
				return false;
			outputText = $"{inputText.Substring(0, length)}...";
			return true;
		}
	}
}
