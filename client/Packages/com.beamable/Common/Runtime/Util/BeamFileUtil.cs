using System;
using System.IO;

namespace Beamable.Common.Util
{
	public partial class BeamUtil
	{
		/// <summary>
		/// replaces all invalid file name characters with a dash
		/// </summary>
		public static string SanitizeStringForPath(string str)
		{
			if (string.IsNullOrWhitespace(str)) return str;
			
			var invalidChars = Path.GetInvalidFileNameChars();
			foreach (var invalidChar in invalidChars)
			{
				str = str.Replace(invalidChar, '-');
			}

			return str;
		}
	}
}
