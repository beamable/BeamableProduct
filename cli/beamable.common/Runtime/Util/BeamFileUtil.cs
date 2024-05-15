using System;
using System.IO;

namespace Beamable.Common.Util
{
	public partial class BeamUtil
	{
		private static readonly char[] InvalidFileChars = Path.GetInvalidFileNameChars();

		/// <summary>
		/// replaces all invalid file name characters with a dash
		/// </summary>
		public static string SanitizeStringForPath(string str)
		{
			if (string.IsNullOrEmpty(str)) return str;

			foreach (var invalidChar in InvalidFileChars)
			{
				str = str.Replace(invalidChar, '-');
			}

			return str;
		}
	}
}
